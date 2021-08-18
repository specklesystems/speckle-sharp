using Speckle.ConnectorGSA.Proxy.Results;
using Speckle.GSA.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Results
{
  public class Results1dProcessor : ResultsProcessorBase  
  {
    public override ResultGroup Group => ResultGroup.Element1d;

    private List<ResultType> possibleResultTypes = new List<ResultType>()
    {
      ResultType.Element1dDisplacement,
      ResultType.Element1dForce
    };

    public Results1dProcessor(string filePath, Dictionary<ResultUnitType, double> unitData, List<ResultType> resultTypes = null, 
      List<string> cases = null, List<int> elemIds = null) : base(filePath, unitData, cases, elemIds)
    {
      if (resultTypes == null)
      {
        this.resultTypes = possibleResultTypes;
      }
      else
      {
        this.resultTypes = resultTypes.Where(rt => possibleResultTypes.Contains(rt)).ToList();
      }

      ColumnValuesFns = new Dictionary<ResultType, Func<List<int>, Dictionary<string, object>>>()
      {
        { ResultType.Element1dDisplacement, ResultTypeColumnValues_Element1dDisplacement },
        { ResultType.Element1dForce, ResultTypeColumnValues_Element1dForce }
      };
    }

    public override bool LoadFromFile(out int numErrorRows, bool parallel = true) 
      => base.LoadFromFile<CsvElem1d>(out numErrorRows, parallel);


    #region column_values_fns
    protected Dictionary<string, object> ResultTypeColumnValues_Element1dDisplacement(List<int> indices)
    {
      var factors = GetFactors(ResultUnitType.Length);
      var retDict = new Dictionary<string, object>
      {
        { "ux", indices.Select(i => ApplyFactors(((CsvElem1d)Records[i]).Ux, factors)).Cast<object>().ToList() },
        { "uy", indices.Select(i => ApplyFactors(((CsvElem1d)Records[i]).Uy, factors)).Cast<object>().ToList() },
        { "uz", indices.Select(i => ApplyFactors(((CsvElem1d)Records[i]).Uz, factors)).Cast<object>().ToList() },
        { "|u|", indices.Select(i => ApplyFactors(((CsvElem1d)Records[i]).U.Value, factors)).Cast<object>().ToList() }
      };
      return retDict;
    }


    protected Dictionary<string, object> ResultTypeColumnValues_Element1dForce(List<int> indices)
    {
      var factorsForce = GetFactors(ResultUnitType.Force);
      var factorsMoment = GetFactors(ResultUnitType.Force, ResultUnitType.Length);
      var retDict = new Dictionary<string, object>
      {
        { "fx", indices.Select(i => ApplyFactors(((CsvElem1d)Records[i]).Fx, factorsForce)).Cast<object>().ToList() },
        { "fy", indices.Select(i => ApplyFactors(((CsvElem1d)Records[i]).Fy, factorsForce)).Cast<object>().ToList() },
        { "fz", indices.Select(i => ApplyFactors(((CsvElem1d)Records[i]).Fz, factorsForce)).Cast<object>().ToList() },
        { "|f|", indices.Select(i => ApplyFactors(((CsvElem1d)Records[i]).F.Value, factorsForce)).Cast<object>().ToList() },
        { "mxx", indices.Select(i => ApplyFactors(((CsvElem1d)Records[i]).Mxx, factorsMoment)).Cast<object>().ToList() },
        { "myy", indices.Select(i => ApplyFactors(((CsvElem1d)Records[i]).Myy, factorsMoment)).Cast<object>().ToList() },
        { "mzz", indices.Select(i => ApplyFactors(((CsvElem1d)Records[i]).Mzz, factorsMoment)).Cast<object>().ToList() },
        { "|m|", indices.Select(i => ApplyFactors(((CsvElem1d)Records[i]).M.Value, factorsMoment)).Cast<object>().ToList() },
        { "fyz", indices.Select(i => ApplyFactors(((CsvElem1d)Records[i]).Fyz.Value, factorsForce)).Cast<object>().ToList() },
        { "myz", indices.Select(i => ApplyFactors(((CsvElem1d)Records[i]).Myz.Value, factorsMoment)).Cast<object>().ToList() }
      };
      return retDict;
    }
    #endregion
  }
}
