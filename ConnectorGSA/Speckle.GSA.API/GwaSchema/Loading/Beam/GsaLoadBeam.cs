using System;
using System.Collections.Generic;
using System.Linq;


namespace Speckle.GSA.API.GwaSchema
{
  [GsaType(GwaKeyword.LOAD_BEAM, GwaSetCommandType.SetAt, true, GwaKeyword.MEMB, GwaKeyword.EL)]
  public abstract class GsaLoadBeam : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public List<int> Entities = new List<int>();
    public int? LoadCaseIndex;
    public LoadBeamAxisRefType AxisRefType;
    public int? AxisIndex;
    public bool Projected;
    public AxisDirection6 LoadDirection;

    protected GwaKeyword childKeyword;

    protected bool FromGwaCommon(string gwa, out List<string> remainingItems, params Func<string, bool>[] extraFns)
    {
      if (!BasicFromGwa(gwa, out remainingItems, childKeyword.ToString()))
      {
        return false;
      }
      var items = remainingItems;

      //LOAD_BEAM_POINT.2 | name | list | case | axis | proj | dir | pos | value
      //LOAD_BEAM_UDL.2 | name | list | case | axis | proj | dir | value
      //LOAD_BEAM_LINE.2 | name | list | case | axis | proj | dir | value_1 | value_2
      //LOAD_BEAM_PATCH.2 | name | list | case | axis | proj | dir | pos_1 | value_1 | pos_2 | value_2
      //LOAD_BEAM_TRILIN.2 | name | list | case | axis | proj | dir | pos_1 | value_1 | pos_2 | value_2

      //Common fields across all of them: name | list | case | axis | proj | dir
      return (FromGwaByFuncs(items, out remainingItems, 
        AddName, 
        (v) => AddEntities(v, out Entities), 
        (v) => (AddNullableIndex(v, out LoadCaseIndex)),
        AddAxis,
        AddProj, 
        (v) => Enum.TryParse(v, true, out LoadDirection))
        && (((extraFns.Count() > 0) && FromGwaByFuncs(remainingItems, out _, extraFns)) || true));
    }

    protected bool GwaCommon(bool includeSet, out List<string> gwa, params object[] extra)
    {
      if (!InitialiseGwa(includeSet, out var items, childKeyword.ToString()))
      {
        gwa = new List<string>();
        return false;
      }

      //LOAD_BEAM_POINT.2 | name | list | case | axis | proj | dir | pos | value
      //LOAD_BEAM_UDL.2 | name | list | case | axis | proj | dir | value
      //LOAD_BEAM_LINE.2 | name | list | case | axis | proj | dir | value_1 | value_2
      //LOAD_BEAM_PATCH.2 | name | list | case | axis | proj | dir | pos_1 | value_1 | pos_2 | value_2
      //LOAD_BEAM_TRILIN.2 | name | list | case | axis | proj | dir | pos_1 | value_1 | pos_2 | value_2

      //Common fields across all of them: name | list | case | axis | proj | dir
      AddItems(ref items, Name, AddEntities(Entities), 
        LoadCaseIndex ?? 0,
        AddAxis(), 
        Projected ? "YES" : "NO", 
        (LoadDirection == AxisDirection6.NotSet) ? "X" : LoadDirection.ToString());
      if (extra.Count() > 0)
      {
        AddItems(ref items, extra);
      }

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region to_gwa_fns
    public string AddAxis()
    {
      return (AxisRefType == LoadBeamAxisRefType.Global || AxisRefType == LoadBeamAxisRefType.NotSet)
        ?  "GLOBAL"
        : (AxisRefType == LoadBeamAxisRefType.Reference)
         ? (AxisIndex ?? 0).ToString()
         : AxisRefType.ToString().ToUpper();
    }
    #endregion

    #region from_gwa_fns
    public bool AddAxis(string v)
    {
      if (v.Equals("global", StringComparison.InvariantCultureIgnoreCase))
      {
        AxisRefType = LoadBeamAxisRefType.Global;
      }
      else if (v.Equals("local", StringComparison.InvariantCultureIgnoreCase))
      {
        AxisRefType = LoadBeamAxisRefType.Local;
      }
      else if (v.Equals("natural", StringComparison.InvariantCultureIgnoreCase))
      {
        AxisRefType = LoadBeamAxisRefType.Natural;
      }
      else if (v.IsDigits() && int.TryParse(v, out var foundIndex) && foundIndex > 0)
      {
        AxisRefType = LoadBeamAxisRefType.Reference;
        AxisIndex = foundIndex;
      }
      else
      {
        AxisRefType = LoadBeamAxisRefType.NotSet;
      }
      return true;
    }

    private bool AddProj(string v)
    {
      Projected = v.Equals("yes", StringComparison.InvariantCultureIgnoreCase);
      return true;
    }
    #endregion
  }

  public class GsaLoadBeamPoint : GsaLoadBeam
  {
    public double Position;
    public double? Load;

    public GsaLoadBeamPoint() : base()
    {
      childKeyword = GwaKeyword.LOAD_BEAM_POINT;
      Version = 2;
    }

    public override bool FromGwa(string gwa)
    {
      //Already parsed by FromGwaCommon: name | list | case | axis | proj | dir
      //Remaining: pos | value
      return FromGwaCommon(gwa, out var remainingItems, (v) => double.TryParse(v, out Position), (v) => AddNullableDoubleValue(v, out Load));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      // Already processed by GwaCommon: name | list | case | axis | proj | dir
      // Remaining: pos | value
      return GwaCommon(includeSet, out gwa, Position.ToString(), Load ?? 0);
    }
  }

  public class GsaLoadBeamUdl : GsaLoadBeam
  {
    public double? Load;

    public GsaLoadBeamUdl() : base()
    {
      childKeyword = GwaKeyword.LOAD_BEAM_UDL;
      Version = 2;
    }

    public override bool FromGwa(string gwa)
    {
      //Already parsed by FromGwaCommon: name | list | case | axis | proj | dir
      //Remaining: value
      return FromGwaCommon(gwa, out var remainingItems, (v) => AddNullableDoubleValue(v, out Load));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      // Already processed by GwaCommon: name | list | case | axis | proj | dir
      // Remaining: value
      return GwaCommon(includeSet, out gwa, Load ?? 0);
    }
  }

  public class GsaLoadBeamLine : GsaLoadBeam
  {
    public double? Load1;
    public double? Load2;

    public GsaLoadBeamLine() : base()
    {
      childKeyword = GwaKeyword.LOAD_BEAM_LINE;
      Version = 2;
    }

    public override bool FromGwa(string gwa)
    {
      // Already parsed by FromGwaCommon: name | list | case | axis | proj | dir
      // Remaining: value_1 | value_2
      return FromGwaCommon(gwa, out var remainingItems, (v) => AddNullableDoubleValue(v, out Load1), (v) => AddNullableDoubleValue(v, out Load2));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      // Already processed by GwaCommon: name | list | case | axis | proj | dir
      // Remaining: value_1 | value_2
      return GwaCommon(includeSet, out gwa, Load1 ?? 0, Load2 ?? 0);
    }
  }

  //This class is here simply to save on code as the code is the same except for the keyword.  If the syntax/schema for a future ever changes then
  //this will need to be refactored into separate classes again
  public abstract class GsaLoadBeamPatchTrilin : GsaLoadBeam
  {
    public double Position1;
    public double? Load1;
    public double Position2Percent;
    public double? Load2;

    public override bool FromGwa(string gwa)
    {
      // Already parsed by FromGwaCommon: name | list | case | axis | proj | dir

      return FromGwaCommon(gwa, out var remainingItems,
        // pos_1 | value_1 | pos_2 | value_2
        (v) => double.TryParse(v, out Position1),
        (v) => AddNullableDoubleValue(v, out Load1),
        (v) => double.TryParse(v.Replace("%", ""), out Position2Percent),
        (v) => AddNullableDoubleValue(v, out Load2));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      // Already processed by GwaCommon: name | list | case | axis | proj | dir
      // Remaining: pos_1 | value_1 | pos_2 | value_2
      return GwaCommon(includeSet, out gwa, Position1, Load1 ?? 0, Position2Percent + "%", Load2 ?? 0);
    }
  }

  public class GsaLoadBeamPatch : GsaLoadBeamPatchTrilin
  {
    public GsaLoadBeamPatch() : base()
    {
      childKeyword = GwaKeyword.LOAD_BEAM_PATCH;
      Version = 2;
    }
  }

  public class GsaLoadBeamTrilin : GsaLoadBeamPatchTrilin
  {
    public GsaLoadBeamTrilin() : base()
    {
      childKeyword = GwaKeyword.LOAD_BEAM_TRILIN;
      Version = 2;
    }
  }
}
