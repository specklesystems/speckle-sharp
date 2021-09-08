using CsvHelper;
using Speckle.ConnectorGSA.Proxy.Results;
using Speckle.GSA.API;
using Speckle.GSA.API.CsvSchema;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Speckle.ConnectorGSA.Results
{
  public abstract class ResultsProcessorBase<T> : IResultsProcessor where T : CsvRecord
  {
    protected string filePath;
    protected HashSet<string> cases;
    protected HashSet<int> elemIds;
    protected Dictionary<int, CsvRecord> Records = new Dictionary<int, CsvRecord>();

    protected Dictionary<ResultUnitType, double> unitData;
    protected List<string> orderedCases = null; // will be updated in the first call to GetResultHierarchy
    protected HashSet<ResultType> resultTypes;
    protected const int significantDigits = 6;

    protected Dictionary<int, Dictionary<string, List<int>>> RecordIndices = new Dictionary<int, Dictionary<string, List<int>>>();

    public string ResultTypeName(ResultType rt) => Proxy.GsaProxy.ResultTypeStrings[rt];
    public List<int> ElementIds => elemIds.OrderBy(i => i).ToList();
    public List<string> CaseIds => cases.OrderBy(c => c).ToList();
    public abstract ResultGroup Group { get; }

    public ResultsProcessorBase(string filePath, Dictionary<ResultUnitType, double> unitData, List<string> cases = null, List<int> elemIds = null)
    {
      this.filePath = filePath;
      if (cases != null)
      {
        this.cases = new HashSet<string>(cases);
      }
      if (elemIds != null)
      {
        this.elemIds = new HashSet<int>(elemIds);
      }
      this.unitData = unitData;
    }

    //public abstract bool LoadFromFile(out int numErrorRows, bool parallel = true);

    protected abstract bool Scale(T record);

    //protected bool LoadFromFile(out int numErrorRows, bool parallel = true) where T: CsvRecord
    public bool LoadFromFile(out int numErrorRows, bool parallel = true)
    {
      if (!File.Exists(filePath))
      {
        numErrorRows = 0;
        return false;
      }
      var reader = new StreamReader(filePath);

      var tasks = new List<Task>();

      int rowIndex = 0;

      var foundCases = new HashSet<string>();
      var foundElems = new HashSet<int>();

      numErrorRows = 0;

      // [ result_type, [ [ headers ], [ row, column ] ] ]

      using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
      {
        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
          bool successfulRead = false;
          T record = null;
          try
          {
            record = csv.GetRecord<T>();
            successfulRead = true;
          }
          catch
          {
            numErrorRows++;
          }

          if (successfulRead)
          {
            Scale(record);
            if (elemIds == null && !foundElems.Contains(record.ElemId))
            {
              foundElems.Add(record.ElemId);
            }
            if (cases == null && !foundCases.Contains(record.CaseId))
            {
              foundCases.Add(record.CaseId);
            }

            if ((elemIds == null || elemIds.Contains(record.ElemId)) && ((cases == null) || (cases.Contains(record.CaseId))))
            {
              Records.Add(rowIndex, record);
              if (!RecordIndices.ContainsKey(record.ElemId))
              {
                RecordIndices.Add(record.ElemId, new Dictionary<string, List<int>>());
              }
              if (!RecordIndices[record.ElemId].ContainsKey(record.CaseId))
              {
                RecordIndices[record.ElemId].Add(record.CaseId, new List<int>());
              }
              RecordIndices[record.ElemId][record.CaseId].Add(rowIndex);
            }
          }

          rowIndex++;
        }
      }

      if (elemIds == null)
      {
        this.elemIds = foundElems;
      }
      if (cases == null)
      {
        this.cases = foundCases;
      }

      this.orderedCases = this.cases.OrderBy(c => c).ToList();

      reader.Close();
      return true;
    }

    public bool GetResultRecords(int index, string loadCase, out List<CsvRecord> records)
    {
      if (GetResultRecords(index, loadCase, out List<T> localRecords))
      {
        records = localRecords.Cast<CsvRecord>().ToList();
        return true;
      }
      records = null;
      return false;
    }

    public bool GetResultRecords(int index, out List<CsvRecord> records)
    {
      if (GetResultRecords(index, out List<T> localRecords))
      {
        records = localRecords.Cast<CsvRecord>().ToList();
        return true;
      }
      records = null;
      return false;
    }

    public virtual bool GetResultRecords(int index, string loadCase, out List<T> records)
    {
      if (RecordIndices.ContainsKey(index) && RecordIndices[index] != null && RecordIndices[index].ContainsKey(loadCase) 
        && RecordIndices[index][loadCase] != null && RecordIndices[index][loadCase].Count > 0)
      {
        records = RecordIndices[index][loadCase].Select(i => (T)Records[i]).ToList();
        return true;
      }
      records = null;
      return false;
    }

    public virtual bool GetResultRecords(int index, out List<T> records)
    {
      if (RecordIndices.ContainsKey(index) && RecordIndices[index]  != null && RecordIndices[index].Count > 0)
      {
        records = RecordIndices[index].Keys.SelectMany(k => RecordIndices[index][k].Select(i => (T)Records[i])).ToList();
        return true;
      }
      records = null;
      return false;
    }


    protected float? ApplyFactors(float? val, List<double> factors)
    {
      if (!val.HasValue)
      {
        return null;
      }
      return ApplyFactors(val.Value, factors);
    }

    protected float ApplyFactors(float val, List<double> factors)
    {
      if (factors == null || factors.Count() == 0)
      {
        return RoundToSignificantDigits(val);
      }
      if ((float)val == 0)
      {
        return 0;
      }
      foreach (var f in factors)
      {
        val = (float)((float)val * f);
      }
      return RoundToSignificantDigits(val);
    }

    protected double? ApplyFactors(double? val, List<double> factors)
    {
      if (!val.HasValue)
      {
        return null;
      }
      return ApplyFactors(val.Value, factors);
    }

    protected double ApplyFactors(double val, List<double> factors)
    {
      if (factors == null || factors.Count() == 0)
      {
        return RoundToSignificantDigits(val);
      }
      if ((double)val == 0)
      {
        return 0;
      }
      foreach (var f in factors)
      {
        val = (double)((double)val * f);
      }
      return RoundToSignificantDigits(val);
    }

    protected List<double> GetFactors(params ResultUnitType[] ruts)
    {
      return ruts.Where(r => unitData.ContainsKey(r)).Select(r => unitData[r]).ToList();
    }

    protected double RoundToSignificantDigits(double d, int digits = significantDigits)
    {
      if (d == 0)
      {
        return 0;
      }

      var scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);
      return Math.Round(scale * Math.Round(d / scale, digits), digits);
    }

    protected float RoundToSignificantDigits(float d, int digits = significantDigits)
    {
      if (d == 0)
      {
        return 0;
      }

      var scale = (float)Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);
      return (float)Math.Round((scale * Math.Round(d / scale, digits)),digits);
    }

    protected bool SendableValue(object v)
    {
      if (v == null)
      {
        return false;
      }
      if (v is int)
      {
        return ((int)v != 0);
      }
      else if (v is float)
      {
        return ((float)v != 0);
      }
      else if (v is double)
      {
        return ((double)v != 0);
      }
      else if (v is string)
      {
        return (!string.IsNullOrEmpty((string)v) && !((string)v).Equals("null", StringComparison.InvariantCultureIgnoreCase));
      }
      return true;
    }
  }
}
