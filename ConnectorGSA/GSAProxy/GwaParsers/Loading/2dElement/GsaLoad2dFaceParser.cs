using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.LOAD_2D_FACE, GwaSetCommandType.SetAt, true, GwaKeyword.LOAD_TITLE, GwaKeyword.AXIS, GwaKeyword.EL, GwaKeyword.MEMB)]
  public class GsaLoad2dFaceParser : GwaParser<GsaLoad2dFace>
  {
    public GsaLoad2dFaceParser(GsaLoad2dFace gsaLoad2DFace) : base(gsaLoad2DFace) { }

    public GsaLoad2dFaceParser() : base(new GsaLoad2dFace()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;

      //LOAD_2D_FACE.2 | name | list | case | axis | type | proj | dir | value(n) | r | s
      if (!FromGwaByFuncs(items, out remainingItems, AddName, (v) => AddEntities(v, out record.MemberIndices, out record.ElementIndices), AddCase, AddAxis, AddType, AddProj, AddDir))
      { 
        return false;
      }
      items = remainingItems;

      if (items.Count() > 0)
      {
        record.Values = new List<double>();
        var total = items.Count();
        if (record.Type == Load2dFaceType.Point)
        {
          total = items.Count() - 2;
        }
        for (int i = 0; i < total; i++)
        {
          if (double.TryParse(items[i], out double v))
          {
            record.Values.Add(v);
          }
          else
          {
            return false;
          }
        }
        if (record.Type == Load2dFaceType.Point)
        {
          record.R = (double.TryParse(items[total], out var r)) ? (double?)r : null;
          record.S = (double.TryParse(items[total + 1], out var s)) ? (double?)s : null;
        }
      }
      return true;
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //LOAD_2D_FACE.2 | name | list | case | axis | type | proj | dir | value(n) | r | s
      AddItems(ref items, record.Name, AddEntities(record.MemberIndices, record.ElementIndices), record.LoadCaseIndex ?? 0, AddAxis(), AddType(), AddProj(), record.LoadDirection, AddValues());
      if (record.Type == Load2dFaceType.Point)
      {
        AddItems(ref items, record.R, record.S);
      }

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region to_gwa_fns
    private string AddAxis()
    {
      if (record.AxisRefType == AxisRefType.Reference && record.AxisIndex.HasValue)
      {
        return record.AxisIndex.Value.ToString();
      }
      if (record.AxisRefType == AxisRefType.Local)
      {
        return "LOCAL";
      }
      //Assume global to be the default
      return "GLOBAL";
    }

    private string AddType()
    {
      switch (record.Type)
      {
        case Load2dFaceType.Uniform:
          return "CONS";
        case Load2dFaceType.General:
          return "GEN";
        case Load2dFaceType.Point:
          return "POINT";
        default:
          return "";
      }
    }

    private string AddProj()
    {
      return (record.Projected) ? "YES" : "NO";
    }
    private string AddValues()
    {
      if (record.Values != null && record.Values.Count() > 0)
      {
        return string.Join(" ", record.Values);
      }
      else
      {
        return "";
      }
    }
    #endregion

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    private bool AddCase(string v)
    {
      record.LoadCaseIndex = (int.TryParse(v, out var loadCaseIndex) && loadCaseIndex > 0) ? (int?)loadCaseIndex : null;
      return true;
    }

    private bool AddAxis(string v)
    {
      record.AxisRefType = Enum.TryParse<AxisRefType>(v, true, out var refType) ? refType : AxisRefType.NotSet;
      if (record.AxisRefType == AxisRefType.NotSet && int.TryParse(v, out var axisIndex) && axisIndex > 0)
      {
        record.AxisRefType = AxisRefType.Reference;
        record.AxisIndex = axisIndex;
      }
      return true;
    }

    private bool AddType(string v)
    {
      switch (v)
      {
        case "CONS":
          record.Type = Load2dFaceType.Uniform;
          break;
        case "GEN":
          record.Type = Load2dFaceType.General;
          break;
        case "POINT":
          record.Type = Load2dFaceType.Point;
          break;
        default:
          return false;
      }
      return true;
    }

    private bool AddProj(string v)
    {
      record.Projected = (v.Equals("yes", StringComparison.InvariantCultureIgnoreCase));
      return true;
    }

    private bool AddDir(string v)
    {
      record.LoadDirection = Enum.TryParse<AxisDirection3>(v, true, out var loadDir) ? loadDir : AxisDirection3.NotSet;
      return true;
    }
    #endregion
  }
}
