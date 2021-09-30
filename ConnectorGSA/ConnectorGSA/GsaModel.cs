using Speckle.GSA.API;
using Speckle.ConnectorGSA.Proxy.Cache;
using Speckle.Core.Credentials;
using Serilog;

namespace ConnectorGSA
{
  public class GsaModel : GsaModelBase
  {
    public static IGSAModel Instance = new GsaModel();

    private static IGSACache cache = new GsaCache();
    private static IGSAProxy proxy = new Speckle.ConnectorGSA.Proxy.GsaProxy();
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

    public GsaModel()
    {
      if (Speckle.GSA.API.Instance.GsaModel == null)
      {
        Speckle.GSA.API.Instance.GsaModel = this;
      }
    }
  }
}
