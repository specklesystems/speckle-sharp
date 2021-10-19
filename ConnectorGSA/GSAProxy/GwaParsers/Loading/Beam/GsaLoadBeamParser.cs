using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  //This one keyword is recognised by the COM API as the key to all the child classes in this file - importantly, the index range is shared across all these types.
  //So there is one table 
  [GsaType(GwaKeyword.LOAD_BEAM, GwaSetCommandType.SetAt, true, GwaKeyword.MEMB, GwaKeyword.EL)]
  public abstract class GsaLoadBeamParser : GwaParser<GsaLoadBeam>
  {  
    protected GwaKeyword childKeyword;

    public GsaLoadBeamParser(GsaLoadBeam gsaLoadBeam) : base(gsaLoadBeam) { }

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
        (v) => AddEntities(v, out record.MemberIndices, out record.ElementIndices), 
        (v) => (AddNullableIndex(v, out record.LoadCaseIndex)),
        AddAxis,
        AddProj, 
        (v) => Enum.TryParse(v, true, out record.LoadDirection))
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
      AddItems(ref items, record.Name, AddEntities(record.MemberIndices, record.ElementIndices),
        record.LoadCaseIndex ?? 0,
        AddAxis(),
        record.Projected ? "YES" : "NO", 
        (record.LoadDirection == AxisDirection6.NotSet) ? "X" : record.LoadDirection.ToString());
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
      return (record.AxisRefType == LoadBeamAxisRefType.Global || record.AxisRefType == LoadBeamAxisRefType.NotSet)
        ?  "GLOBAL"
        : (record.AxisRefType == LoadBeamAxisRefType.Reference)
         ? (record.AxisIndex ?? 0).ToString()
         : record.AxisRefType.ToString().ToUpper();
    }
    #endregion

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    public bool AddAxis(string v)
    {
      if (v.Equals("global", StringComparison.InvariantCultureIgnoreCase))
      {
        record.AxisRefType = LoadBeamAxisRefType.Global;
      }
      else if (v.Equals("local", StringComparison.InvariantCultureIgnoreCase))
      {
        record.AxisRefType = LoadBeamAxisRefType.Local;
      }
      else if (v.Equals("natural", StringComparison.InvariantCultureIgnoreCase))
      {
        record.AxisRefType = LoadBeamAxisRefType.Natural;
      }
      else if (v.IsDigits() && int.TryParse(v, out var foundIndex) && foundIndex > 0)
      {
        record.AxisRefType = LoadBeamAxisRefType.Reference;
        record.AxisIndex = foundIndex;
      }
      else
      {
        record.AxisRefType = LoadBeamAxisRefType.NotSet;
      }
      return true;
    }

    private bool AddProj(string v)
    {
      record.Projected = v.Equals("yes", StringComparison.InvariantCultureIgnoreCase);
      return true;
    }
    #endregion
  }

  [GsaChildType(GwaKeyword.LOAD_BEAM_POINT, typeof(GsaLoadBeamPoint))]
  public class GsaLoadBeamPointParser : GsaLoadBeamParser
  {
    public double Position;
    public double? Load;


    public GsaLoadBeamPointParser(GsaLoadBeamPoint gsaLoadBeamPoint) : base(gsaLoadBeamPoint) 
    {
      childKeyword = GwaKeyword.LOAD_BEAM_POINT;
    }

    public GsaLoadBeamPointParser() : base(new GsaLoadBeamPoint())
    {
      childKeyword = GwaKeyword.LOAD_BEAM_POINT;
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

  [GsaChildType(GwaKeyword.LOAD_BEAM_UDL, typeof(GsaLoadBeamUdl))]
  public class GsaLoadBeamUdlParser : GsaLoadBeamParser
  {
    public GsaLoadBeamUdlParser() : base(new GsaLoadBeamUdl())
    {
      childKeyword = GwaKeyword.LOAD_BEAM_UDL;
    }

    public GsaLoadBeamUdlParser(GsaLoadBeamUdl gsaLoadBeamUdl) : base(gsaLoadBeamUdl)
    {
      childKeyword = GwaKeyword.LOAD_BEAM_UDL;
    }

    public override bool FromGwa(string gwa)
    {
      //Already parsed by FromGwaCommon: name | list | case | axis | proj | dir
      //Remaining: value
      return FromGwaCommon(gwa, out var remainingItems, (v) => AddNullableDoubleValue(v, out ((GsaLoadBeamUdl)record).Load));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      // Already processed by GwaCommon: name | list | case | axis | proj | dir
      // Remaining: value
      return GwaCommon(includeSet, out gwa, ((GsaLoadBeamUdl)record).Load ?? 0);
    }
  }

  [GsaChildType(GwaKeyword.LOAD_BEAM_LINE, typeof(GsaLoadBeamLine))]
  public class GsaLoadBeamLineParser : GsaLoadBeamParser
  {
    public GsaLoadBeamLineParser() : base(new GsaLoadBeamLine())
    {
      childKeyword = GwaKeyword.LOAD_BEAM_LINE;
    }

    public GsaLoadBeamLineParser(GsaLoadBeamLine gsaLoadBeamLine) : base(gsaLoadBeamLine)
    {
      childKeyword = GwaKeyword.LOAD_BEAM_LINE;
    }

    public override bool FromGwa(string gwa)
    {
      // Already parsed by FromGwaCommon: name | list | case | axis | proj | dir
      // Remaining: value_1 | value_2
      return FromGwaCommon(gwa, out var remainingItems, (v) => AddNullableDoubleValue(v, out ((GsaLoadBeamLine)record).Load1), 
        (v) => AddNullableDoubleValue(v, out ((GsaLoadBeamLine)record).Load2));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      // Already processed by GwaCommon: name | list | case | axis | proj | dir
      // Remaining: value_1 | value_2
      return GwaCommon(includeSet, out gwa, ((GsaLoadBeamLine)record).Load1 ?? 0, ((GsaLoadBeamLine)record).Load2 ?? 0);
    }
  }

  //This class is here simply to save on code as the code is the same except for the keyword.  If the syntax/schema for a future ever changes then
  //this will need to be refactored into separate classes again
  public abstract class GsaLoadBeamPatchTrilinParser : GsaLoadBeamParser
  {
    public GsaLoadBeamPatchTrilinParser(GsaLoadBeamPatchTrilin gsaLoadBeamPatchTrilin) : base(gsaLoadBeamPatchTrilin) { }

    public override bool FromGwa(string gwa)
    {
      var localRecord = ((GsaLoadBeamPatchTrilin)record);

      // Already parsed by FromGwaCommon: name | list | case | axis | proj | dir

      return FromGwaCommon(gwa, out var remainingItems,
        // pos_1 | value_1 | pos_2 | value_2
        (v) => double.TryParse(v, out localRecord.Position1),
        (v) => AddNullableDoubleValue(v, out localRecord.Load1),
        (v) => double.TryParse(v.Replace("%", ""), out localRecord.Position2Percent),
        (v) => AddNullableDoubleValue(v, out localRecord.Load2));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      var localRecord = ((GsaLoadBeamPatchTrilin)record);
      // Already processed by GwaCommon: name | list | case | axis | proj | dir
      // Remaining: pos_1 | value_1 | pos_2 | value_2
      return GwaCommon(includeSet, out gwa, localRecord.Position1, localRecord.Load1 ?? 0, localRecord.Position2Percent + "%", localRecord.Load2 ?? 0);
    }
  }

  [GsaChildType(GwaKeyword.LOAD_BEAM_PATCH, typeof(GsaLoadBeamPatch))]
  public class GsaLoadBeamPatchParser : GsaLoadBeamPatchTrilinParser
  {
    public GsaLoadBeamPatchParser() : base(new GsaLoadBeamPatch())
    {
      childKeyword = GwaKeyword.LOAD_BEAM_PATCH;
    }

    public GsaLoadBeamPatchParser(GsaLoadBeamPatch gsaLoadBeamPatch) : base(gsaLoadBeamPatch)
    {
      childKeyword = GwaKeyword.LOAD_BEAM_PATCH;
    }
  }

  [GsaChildType(GwaKeyword.LOAD_BEAM_TRILIN, typeof(GsaLoadBeamTrilin))]
  public class GsaLoadBeamTrilinParser : GsaLoadBeamPatchTrilinParser
  {
    public GsaLoadBeamTrilinParser() : base(new GsaLoadBeamTrilin())
    {
      childKeyword = GwaKeyword.LOAD_BEAM_TRILIN;
    }
    public GsaLoadBeamTrilinParser(GsaLoadBeamTrilin gsaLoadBeamTrilin) : base(gsaLoadBeamTrilin)
    {
      childKeyword = GwaKeyword.LOAD_BEAM_TRILIN;
    }
  }
}
