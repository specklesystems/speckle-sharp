using Speckle.GSA.API;
using Speckle.ConnectorGSA.Proxy.Cache;
using Speckle.Core.Credentials;
using Serilog;
using Speckle.ConnectorGSA.Proxy;
using Speckle.Core.Kits;
using System;
using Speckle.Core.Models;
using System.Linq;
using System.Collections.Generic;

namespace ConnectorGSA
{
  public class GsaModel : GsaModelBase
  {
    public static IGSAModel Instance = new GsaModel();

    private static IGSACache cache = new GsaCache();
    private static IGSAProxy proxy = new GsaProxy();
    //private static IGSAMessenger messenger = new GsaMessenger();

    public override IGSACache Cache { get => cache; set => cache = value; }
    public override IGSAProxy Proxy { get => proxy; set => proxy = value; }
    //public override IGSAMessenger Messenger { get => messenger; set => messenger = value; }

		//Using an integer scale at the moment from 0 to 5, which can be mapped to individual loggers
		private int loggingthreshold = 3;
		public override int LoggingMinimumLevel
		{
			get
			{
				return loggingthreshold;
			}
			set
			{
				this.loggingthreshold = value;
				var loggerConfigMinimum = new LoggerConfiguration().ReadFrom.AppSettings().MinimumLevel;
				LoggerConfiguration loggerConfig;
				switch (this.loggingthreshold)
				{
					case 1:
						loggerConfig = loggerConfigMinimum.Debug();
						break;

					case 4:
						loggerConfig = loggerConfigMinimum.Error();
						break;

					default:
						loggerConfig = loggerConfigMinimum.Information();
						break;
				}
				Log.Logger = loggerConfig.CreateLogger();
			}
		}

		public Account Account;
		public string LastCommitId;

    public GsaModel()
    {
      if (Speckle.GSA.API.Instance.GsaModel == null)
      {
        Speckle.GSA.API.Instance.GsaModel = this;
      }
    }

    //TEMP: Not sure where to put this yet 
    public List<List<Type>> SpeckleDependencyTree()
    {
      var kit = KitManager.GetDefaultKit();

      var structuralTypes = kit.Types.Where(t => t.Namespace.ToLower().Contains("structural"));
      var tree = new TypeTreeCollection<Type>(structuralTypes);

      var typeChildren = new Dictionary<Type, List<Type>>();
      var baseType = typeof(Base);
      foreach (var t in structuralTypes)
      {
        var baseClasses = t.GetBaseClasses().Where(bc => structuralTypes.Any(st => st == bc) && bc.InheritsOrImplements(baseType) && bc != baseType);
        foreach (var p in baseClasses)
        {
          typeChildren.UpsertDictionary(p, t);
        }
      }

      foreach (var t in structuralTypes)
      {
        var referencedStructuralTypes = new List<Type>();
        var propertyInfos = t.GetProperties();

        foreach (var pi in propertyInfos)
        {
          Type typeToAdd = null;
          if (pi.IsList(out Type listType))
          {
            if (structuralTypes.Any(st => st == listType))
            {
              typeToAdd = listType;
            }
          }
          else if (structuralTypes.Any(st => st == pi.PropertyType))
          {
            typeToAdd = pi.PropertyType;
          }
          if (typeToAdd != null)
          {
            if (typeChildren.ContainsKey(typeToAdd))
            {
              foreach (var c in typeChildren[typeToAdd])
              {
                if (!referencedStructuralTypes.Contains(c))
                {
                  referencedStructuralTypes.Add(c);
                }
              }
            }
            if (!referencedStructuralTypes.Contains(typeToAdd))
            {
              referencedStructuralTypes.Add(typeToAdd);
            }
          }
        }
        tree.Integrate(t, referencedStructuralTypes.ToArray());
      }
      return tree.Generations();
    }
  }
}
