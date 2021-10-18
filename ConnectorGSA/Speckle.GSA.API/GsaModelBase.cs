using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.GSA.API
{
  public abstract class GsaModelBase : IGSAModel
  {
    public GSALayer StreamLayer { get; set; } = GSALayer.Design;

    public abstract IGSACache Cache { get; set; }
    public abstract IGSAProxy Proxy { get; set; }
    //public abstract IGSAMessenger Messenger { get; set; }

    public string Units { get; set; } = "mm";
    public double CoincidentNodeAllowance { get; set; }
    public List<ResultType> ResultTypes { get; set; }
    public List<ResultGroup> ResultGroups
    {
      get
      {
        var retList = new List<ResultGroup>();
        if (ResultTypes != null && ResultTypes.Count > 0)
        {
          foreach (var rts in ResultTypes.Select(rt => rt.ToString().ToLower()))
          {
            if (rts.Contains("1d") && !retList.Contains(ResultGroup.Element1d) && !retList.Contains(ResultGroup.Element1d))
            {
              retList.Add(ResultGroup.Element1d);
            }
            else if (rts.Contains("2d") && !retList.Contains(ResultGroup.Element2d) && !retList.Contains(ResultGroup.Element2d))
            {
              retList.Add(ResultGroup.Element2d);
            }
            else if (rts.Contains("assembly") && !retList.Contains(ResultGroup.Assembly) && !retList.Contains(ResultGroup.Assembly))
            {
              retList.Add(ResultGroup.Assembly);
            }
            else if (!retList.Contains(ResultGroup.Node) && !retList.Contains(ResultGroup.Node))
            {
              retList.Add(ResultGroup.Node);
            }
          }
        }
        return retList;
      }
    }
    public bool SendResults { get => (ResultTypes != null && ResultTypes.Count > 0 && ResultCases != null && ResultCases.Count > 0); }
    public StreamContentConfig StreamSendConfig { get; set; }
    public List<string> ResultCases { get; set; }
    public bool ResultInLocalAxis { get; set; }
    public int Result1DNumPosition { get; set; } = 3;

    public char GwaDelimiter { get; set; } = '\t';
    public virtual int LoggingMinimumLevel { get; set; }
    public bool SendOnlyMeaningfulNodes { get; set; }
    public IProgress<bool> ConversionProgress { get; set; }
    //public object KitManager { get; private set; }

    public virtual GsaRecord GetNative<T>(int value) => Cache.GetNative<T>(value);
    public virtual List<int> LookupIndices<T>() => Cache.LookupIndices<T>();
    public virtual List<int> ConvertGSAList(string v, GSAEntity e) => Proxy.ConvertGSAList(v, e);
    public virtual string GetApplicationId<T>(int value) => Cache.GetApplicationId<T>(value);

    //public abstract List<List<Type>> SpeckleDependencyTree();
  }
}
