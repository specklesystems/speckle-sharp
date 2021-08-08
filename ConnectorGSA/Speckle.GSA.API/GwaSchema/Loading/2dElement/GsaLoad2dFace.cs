using System;
using System.Collections.Generic;
using System.Linq;


namespace Speckle.GSA.API.GwaSchema
{
  [GsaType(GwaKeyword.LOAD_2D_FACE, GwaSetCommandType.SetAt, true, GwaKeyword.LOAD_TITLE, GwaKeyword.AXIS, GwaKeyword.EL, GwaKeyword.MEMB)]
  public class GsaLoad2dFace : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public List<int> Entities;
    public int? LoadCaseIndex;
    public AxisRefType AxisRefType;
    public int? AxisIndex;
    public Load2dFaceType Type;
    public bool Projected;
    public AxisDirection3 LoadDirection;
    public List<double> Values;
    public double? R;
    public double? S;

    public GsaLoad2dFace() : base()
    {
      //Defaults
      Version = 2;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;

      //LOAD_2D_FACE.2 | name | list | case | axis | type | proj | dir | value(n) | r | s
      if (!FromGwaByFuncs(items, out remainingItems, AddName, (v) => AddEntities(v, out Entities), AddCase, AddAxis, AddType, AddProj, AddDir))
      { 
        return false;
      }
      items = remainingItems;

      if (items.Count() > 0)
      {
        Values = new List<double>();
        var total = items.Count();
        if (Type == Load2dFaceType.Point)
        {
          total = items.Count() - 2;
        }
        for (int i = 0; i < total; i++)
        {
          if (double.TryParse(items[i], out double v))
          {
            Values.Add(v);
          }
          else
          {
            return false;
          }
        }
        if (Type == Load2dFaceType.Point)
        {
          R = (double.TryParse(items[total], out var r)) ? (double?)r : null;
          S = (double.TryParse(items[total + 1], out var s)) ? (double?)s : null;
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
      AddItems(ref items, Name, AddEntities(Entities), LoadCaseIndex ?? 0, AddAxis(), AddType(), AddProj(), LoadDirection, AddValues());
      if (Type == Load2dFaceType.Point)
      {
        AddItems(ref items, R, S);
      }

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region to_gwa_fns
    private string AddAxis()
    {
      if (AxisRefType == AxisRefType.Reference && AxisIndex.HasValue)
      {
        return AxisIndex.Value.ToString();
      }
      if (AxisRefType == AxisRefType.Local)
      {
        return "LOCAL";
      }
      //Assume global to be the default
      return "GLOBAL";
    }

    private string AddType()
    {
      switch (Type)
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
      return (Projected) ? "YES" : "NO";
    }
    private string AddValues()
    {
      if (Values != null && Values.Count() > 0)
      {
        return string.Join(" ", Values);
      }
      else
      {
        return "";
      }
    }
    #endregion

    #region from_gwa_fns
    private bool AddCase(string v)
    {
      LoadCaseIndex = (int.TryParse(v, out var loadCaseIndex) && loadCaseIndex > 0) ? (int?)loadCaseIndex : null;
      return true;
    }

    private bool AddAxis(string v)
    {
      AxisRefType = Enum.TryParse<AxisRefType>(v, true, out var refType) ? refType : AxisRefType.NotSet;
      if (AxisRefType == AxisRefType.NotSet && int.TryParse(v, out var axisIndex) && axisIndex > 0)
      {
        AxisRefType = AxisRefType.Reference;
        AxisIndex = axisIndex;
      }
      return true;
    }

    private bool AddType(string v)
    {
      switch (v)
      {
        case "CONS":
          Type = Load2dFaceType.Uniform;
          break;
        case "GEN":
          Type = Load2dFaceType.General;
          break;
        case "POINT":
          Type = Load2dFaceType.Point;
          break;
        default:
          return false;
      }
      return true;
    }

    private bool AddProj(string v)
    {
      Projected = (v.Equals("yes", StringComparison.InvariantCultureIgnoreCase));
      return true;
    }

    private bool AddDir(string v)
    {
      LoadDirection = Enum.TryParse<AxisDirection3>(v, true, out var loadDir) ? loadDir : AxisDirection3.NotSet;
      return true;
    }
    #endregion
  }
}
