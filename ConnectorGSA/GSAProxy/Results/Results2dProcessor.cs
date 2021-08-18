using CsvHelper;
using Speckle.ConnectorGSA.Proxy.Results;
using Speckle.GSA.API;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Speckle.ConnectorGSA.Results
{
  public class Results2dProcessor : ResultsProcessorBase
  {
    //Not using the base class's records or indices as 2D elements are a special case
    protected Dictionary<int, CsvElem2d> Records2d = new Dictionary<int, CsvElem2d>();
    protected Dictionary<int, Dictionary<string, List<int>>> FaceRecordIndices = new Dictionary<int, Dictionary<string, List<int>>>();
    protected Dictionary<int, Dictionary<string, List<int>>> VertexRecordIndices = new Dictionary<int, Dictionary<string, List<int>>>();

    public override ResultGroup Group => ResultGroup.Element2d;

    private List<ResultType> possibleResultTypes = new List<ResultType>()
    {
      ResultType.Element2dDisplacement,
      ResultType.Element2dProjectedForce,
      ResultType.Element2dProjectedMoment,
      ResultType.Element2dProjectedStressBottom,
      ResultType.Element2dProjectedStressMiddle,
      ResultType.Element2dProjectedStressTop
    };

    public Results2dProcessor(string filePath, Dictionary<ResultUnitType, double> unitData, List<ResultType> resultTypes = null, 
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
        { ResultType.Element2dDisplacement, ResultTypeColumnValues_Element2dDisplacement },
        { ResultType.Element2dProjectedForce, ResultTypeColumnValues_Element2dProjectedForce },
        { ResultType.Element2dProjectedMoment, ResultTypeColumnValues_Element2dProjectedMoment },
        { ResultType.Element2dProjectedStressBottom, ResultTypeColumnValues_Element2dProjectedStressBottom },
        { ResultType.Element2dProjectedStressMiddle, ResultTypeColumnValues_Element2dProjectedStressMiddle },
        { ResultType.Element2dProjectedStressTop, ResultTypeColumnValues_Element2dProjectedStressTop }
      };
    }

    //Assume order is always correct
    //the hierarchy to compile, to be converted, is
    public override bool LoadFromFile(out int numErrorRows, bool parallel = true)
    {
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
          CsvElem2d record = null;
          try
          {
            record = csv.GetRecord<CsvElem2d>();
            successfulRead = true;
          }
          catch
          {
            numErrorRows++;
          }

          if (successfulRead)
          {
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
              Records2d.Add(rowIndex, record);
              if (record.IsVertex)
              {
                if (!VertexRecordIndices.ContainsKey(record.ElemId))
                {
                  VertexRecordIndices.Add(record.ElemId, new Dictionary<string, List<int>>());
                }
                if (!VertexRecordIndices[record.ElemId].ContainsKey(record.CaseId))
                {
                  VertexRecordIndices[record.ElemId].Add(record.CaseId, new List<int>());
                }
                VertexRecordIndices[record.ElemId][record.CaseId].Add(rowIndex);
              }
              else
              {
                if (!FaceRecordIndices.ContainsKey(record.ElemId))
                {
                  FaceRecordIndices.Add(record.ElemId, new Dictionary<string, List<int>>());
                }
                if (!FaceRecordIndices[record.ElemId].ContainsKey(record.CaseId))
                {
                  FaceRecordIndices[record.ElemId].Add(record.CaseId, new List<int>());
                }
                FaceRecordIndices[record.ElemId][record.CaseId].Add(rowIndex);
              }
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

    // For both embedded and separate results, the format needs to be, per element:
    // [ load_case [ result_type [ column [ values ] ] ] ]
    public override Dictionary<string, Dictionary<string, object>> GetResultHierarchy(int elemId)
    {
      var retDict = new Dictionary<string, Dictionary<string, object>>();

      if (!VertexRecordIndices.ContainsKey(elemId) && !FaceRecordIndices.ContainsKey(elemId))
      {
        return null;
      }

      foreach (var caseId in orderedCases)
      {
        var indicesVertex = (VertexRecordIndices[elemId].ContainsKey(caseId)) ? VertexRecordIndices[elemId][caseId] : null;
        var indicesFace = (FaceRecordIndices[elemId].ContainsKey(caseId)) ? FaceRecordIndices[elemId][caseId] : null;

        if (indicesVertex != null && indicesVertex.Count > 0 && indicesFace != null && indicesFace.Count > 0)
        {
          var rtDict = new Dictionary<string, object>(resultTypes.Count * 2);
          foreach (var rt in resultTypes)
          {
            var name = ResultTypeName(rt);
            if (!string.IsNullOrEmpty(name))
            {
              rtDict.Add(name + "_face", ColumnValuesFns[rt](indicesFace));
              if (rt == ResultType.Element2dDisplacement)
              {
                rtDict.Add(name + "_vertex", ColumnValuesFns[rt](indicesVertex));
              }
            }
          }
          retDict.Add(caseId, rtDict);
        }
      }

      return retDict;
    }

    #region column_values_fns
    protected Dictionary<string, object> ResultTypeColumnValues_Element2dDisplacement(List<int> indices)
    {
      var factors = GetFactors(ResultUnitType.Length);
      var retDict = new Dictionary<string, object>
      {
        { "ux", indices.Select(i => ApplyFactors(Records2d[i].Ux, factors)).Cast<object>().ToList() },
        { "uy", indices.Select(i => ApplyFactors(Records2d[i].Uy, factors)).Cast<object>().ToList() },
        { "uz", indices.Select(i => ApplyFactors(Records2d[i].Uz, factors)).Cast<object>().ToList() },
        { "|u|", indices.Select(i => ApplyFactors(Records2d[i].U.Value, factors)).Cast<object>().ToList() }
      };
      return retDict;
    }

    protected Dictionary<string, object> ResultTypeColumnValues_Element2dProjectedMoment(List<int> indices)
    {
      var factors = GetFactors(ResultUnitType.Force, ResultUnitType.Length);
      var retDict = new Dictionary<string, object>
      {
        { "mx", indices.Select(i => ApplyFactors(Records2d[i].Mx, factors)).Cast<object>().ToList() },
        { "my", indices.Select(i => ApplyFactors(Records2d[i].My, factors)).Cast<object>().ToList() },
        { "mxy", indices.Select(i => ApplyFactors(Records2d[i].Mxy, factors)).Cast<object>().ToList() },
        { "mx+mxy", indices.Select(i => ApplyFactors(Records2d[i].Mx_Mxy.Value, factors)).Cast<object>().ToList() },
        { "my+myx", indices.Select(i => ApplyFactors(Records2d[i].My_Myx.Value, factors)).Cast<object>().ToList() }
      };
      return retDict;
    }

    protected Dictionary<string, object> ResultTypeColumnValues_Element2dProjectedForce(List<int> indices)
    {
      var factors = GetFactors(ResultUnitType.Force, ResultUnitType.Length);
      var retDict = new Dictionary<string, object>
      {
        { "nx", indices.Select(i => ApplyFactors(Records2d[i].Nx, factors)).Cast<object>().ToList() },
        { "ny", indices.Select(i => ApplyFactors(Records2d[i].Ny, factors)).Cast<object>().ToList() },
        { "nxy", indices.Select(i => ApplyFactors(Records2d[i].Nxy, factors)).Cast<object>().ToList() },
        { "qx", indices.Select(i => ApplyFactors(Records2d[i].Qx, factors)).Cast<object>().ToList() },
        { "qy", indices.Select(i => ApplyFactors(Records2d[i].Qy, factors)).Cast<object>().ToList() }
      };
      return retDict;
    }

    protected Dictionary<string, object> ResultTypeColumnValues_Element2dProjectedStressBottom(List<int> indices)
    {
      var factors = GetFactors(ResultUnitType.Stress);
      var retDict = new Dictionary<string, object>
      {
        { "xx", indices.Select(i => ApplyFactors(Records2d[i].Xx_b, factors)).Cast<object>().ToList() },
        { "yy", indices.Select(i => ApplyFactors(Records2d[i].Yy_b, factors)).Cast<object>().ToList() },
        { "zz", indices.Select(i => ApplyFactors(Records2d[i].Zz_b, factors)).Cast<object>().ToList() },
        { "xy", indices.Select(i => ApplyFactors(Records2d[i].Xy_b, factors)).Cast<object>().ToList() },
        { "yz", indices.Select(i => ApplyFactors(Records2d[i].Yz_b, factors)).Cast<object>().ToList() },
        { "zx", indices.Select(i => ApplyFactors(Records2d[i].Zx_b, factors)).Cast<object>().ToList() }
      };
      return retDict;
    }

    protected Dictionary<string, object> ResultTypeColumnValues_Element2dProjectedStressMiddle(List<int> indices)
    {
      var factors = GetFactors(ResultUnitType.Stress);
      var retDict = new Dictionary<string, object>
      {
        { "xx", indices.Select(i => ApplyFactors(Records2d[i].Xx_m, factors)).Cast<object>().ToList() },
        { "yy", indices.Select(i => ApplyFactors(Records2d[i].Yy_m, factors)).Cast<object>().ToList() },
        { "zz", indices.Select(i => ApplyFactors(Records2d[i].Zz_m, factors)).Cast<object>().ToList() },
        { "xy", indices.Select(i => ApplyFactors(Records2d[i].Xy_m, factors)).Cast<object>().ToList() },
        { "yz", indices.Select(i => ApplyFactors(Records2d[i].Yz_m, factors)).Cast<object>().ToList() },
        { "zx", indices.Select(i => ApplyFactors(Records2d[i].Zx_m, factors)).Cast<object>().ToList() }
      };
      return retDict;
    }

    protected Dictionary<string, object> ResultTypeColumnValues_Element2dProjectedStressTop(List<int> indices)
    {
      var factors = GetFactors(ResultUnitType.Stress);
      var retDict = new Dictionary<string, object>
      {
        { "xx", indices.Select(i => ApplyFactors(Records2d[i].Xx_t, factors)).Cast<object>().ToList() },
        { "yy", indices.Select(i => ApplyFactors(Records2d[i].Yy_t, factors)).Cast<object>().ToList() },
        { "zz", indices.Select(i => ApplyFactors(Records2d[i].Zz_t, factors)).Cast<object>().ToList() },
        { "xy", indices.Select(i => ApplyFactors(Records2d[i].Xy_t, factors)).Cast<object>().ToList() },
        { "yz", indices.Select(i => ApplyFactors(Records2d[i].Yz_t, factors)).Cast<object>().ToList() },
        { "zx", indices.Select(i => ApplyFactors(Records2d[i].Zx_t, factors)).Cast<object>().ToList() }
      };
      return retDict;
    }
    #endregion
  }
}
