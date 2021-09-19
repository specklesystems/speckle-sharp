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
    public abstract IGSAMessenger Messenger { get; set; }

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
    public int LoggingMinimumLevel { get; set; }
    public bool SendOnlyMeaningfulNodes { get; set; }
    

    public virtual GsaRecord GetNative<T>(int value) => Cache.GetNative<T>(value);
    public virtual List<int> LookupIndices<T>() => Cache.LookupIndices<T>();
    public virtual List<int> ConvertGSAList(string v, GSAEntity e) => Proxy.ConvertGSAList(v, e);
    public virtual string GetApplicationId<T>(int value) => Cache.GetApplicationId<T>(value);

    /*
    public abstract bool ClearResults(ResultGroup group);

    public abstract List <int> ConvertGSAList(string list, GSAEntity entityType);

    public abstract GsaRecord GetNative<T>(int index);
    public abstract bool GetNative(Type t, out List<GsaRecord> gsaRecords);
    public abstract bool GetNative(Type t, int index, out GsaRecord gsaRecords);
    

    public abstract string GetApplicationId<T>(int index);

    public abstract bool GetResultHierarchy(ResultGroup group, int index, out Dictionary<string, Dictionary<string, object>> valueHierarchy, int dimension = 1);

    public abstract List<int> LookupIndices<T>();

    public abstract int NodeAt(double x, double y, double z, double coincidenceTol);

    public abstract bool LoadResults(ResultGroup group, out int numErrorRows, List<string> cases = null, List<int> elemIds = null);


    public abstract bool SetSpeckleObjects(GsaRecord gsaRecords, IEnumerable<object> speckleObjects);

    public abstract bool GetGwaData(bool nodeApplicationIdFilter, out List<GsaRecord> records, IProgress<int> incrementProgress = null);
    */
  }
}
