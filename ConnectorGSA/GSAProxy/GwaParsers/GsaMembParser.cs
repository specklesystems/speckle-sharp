using Speckle.GSA.API;
using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.GSA.API.GwaSchema;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.MEMB, GwaSetCommandType.Set, true, true, false, GwaKeyword.NODE, GwaKeyword.PROP_SPR, GwaKeyword.SECTION, GwaKeyword.PROP_2D)]
  public class GsaMembParser : GwaParser<GsaMemb>
  {
    //Not supporting: 3D members, or 2D reinforcement
    public GsaMembParser(GsaMemb gsaMemb) : base(gsaMemb) { }

    public GsaMembParser() : base(new GsaMemb()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;

      //MEMB.8 | num | name | colour | type (1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 } rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | AUTOMATIC | load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }
      //MEMB.8 | num | name | colour | type (1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 } rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | EFF_LEN | lyy | lzz | llt | load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }
      //MEMB.8 | num | name | colour | type (1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 } rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | EXPLICIT | num_pt | { pt | rest | } | num_span | { span | rest | } load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }
      //MEMB.8 | num | name | colour | type (2D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | off_z | off_auto_internal | reinforcement2d |
      //MEMB.8 | num | name | colour | type (3D) | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | time[4] | dummy |

      //Process enough to determine the type of member it is (i.e 1D, 2D or 3D)
      if (!FromGwaByFuncs(items, out remainingItems, AddName, (v) => AddColour(v, out record.Colour), 
        (v) => v.TryParseStringValue(out record.Type)) || record.Type == MemberType.Generic3d)
      {
        return false;
      }
      items = remainingItems;

      //Now all the common items for 1D and 2D members
      if (!FromGwaByFuncs(items, out remainingItems, (v) => Enum.TryParse(v, true, out record.Exposure), 
        (v) => AddNullableIndex(v, out record.PropertyIndex), (v) => AddNullableIntValue(v, out record.Group), AddTopology, 
        (v) => AddNullableIndex(v, out record.OrientationNodeIndex), (v) => AddNullableDoubleValue(v, out record.Angle), 
        (v) => AddNullableDoubleValue(v, out record.MeshSize), (v) => AddYesNoBoolean(v, out record.IsIntersector), 
        (v) => Enum.TryParse(v, true, out record.AnalysisType), AddFire, (v) => AddNullableDoubleValue(v, out record.LimitingTemperature), 
        (v) => int.TryParse(v, out record.CreationFromStartDays), (v) => int.TryParse(v, out record.StartOfDryingDays),
        (v) => int.TryParse(v, out record.AgeAtLoadingDays), (v) => int.TryParse(v, out record.RemovedAtDays), AddDummy))
      {
        return false;
      }
      items = remainingItems;

      if (record.Type == MemberType.Beam || record.Type == MemberType.Generic1d || record.Type == MemberType.Column || record.Type == MemberType.Void1d)
      {
        //This assumes that rls_1 { | k_1 } rls_2 { | k_2 } is at the start of the items list
        if (!ProcessReleases(items, out remainingItems, ref record.Releases1, ref record.Stiffnesses1, ref record.Releases2, 
          ref record.Stiffnesses2))
        {
          return false;
        }
        items = remainingItems;

        if (!FromGwaByFuncs(items, out remainingItems, (v) => v.TryParseStringValue(out record.RestraintEnd1), 
          (v) => v.TryParseStringValue(out record.RestraintEnd2), (v) => v.TryParseStringValue(out record.EffectiveLengthType)))
        {
          return false;
        }
        items = remainingItems;

        if (record.EffectiveLengthType == EffectiveLengthType.EffectiveLength)
        {
          AddEffectiveLength(items[0], ref record.EffectiveLengthYY, ref record.PercentageYY);
          AddEffectiveLength(items[1], ref record.EffectiveLengthZZ, ref record.PercentageZZ);
          AddEffectiveLength(items[2], ref record.EffectiveLengthLateralTorsional, ref record.FractionLateralTorsional);
          items = items.Skip(3).ToList();
        }
        else if (record.EffectiveLengthType == EffectiveLengthType.Explicit)
        {
          if (!ProcessExplicit(items, out remainingItems))
          {
            return false;
          }
          items = remainingItems;
        }

        if (!FromGwaByFuncs(items, out remainingItems, (v) => AddNullableDoubleValue(v, out record.LoadHeight), 
          (v) => v.TryParseStringValue(out record.LoadHeightReferencePoint), AddIsOffset))
        {
          return false;
        }
        items = remainingItems;

        return record.MemberHasOffsets ? ProcessOffsets(items) : true;
      }
      else if (record.Type == MemberType.Generic2d || record.Type == MemberType.Slab || record.Type == MemberType.Wall || record.Type == MemberType.Void2d)
      {
        AddItems(ref items, record.Offset2dZ ?? 0, record.OffsetAutomaticInternal ? "YES" : "NO", "REBAR_2D.1", 0, 0, 0);
        return FromGwaByFuncs(items, out _, (v) => AddNullableDoubleValue(v, out record.Offset2dZ), 
          (v) => AddYesNoBoolean(v, out record.OffsetAutomaticInternal));
      }
      else
      {
        return false;
      }
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      gwa = new List<string>();
      //Just supporting non-void 1D types at this stage
      if (record.Type == MemberType.Generic3d || !InitialiseGwa(includeSet, out var items))
      {
        return false;
      }

      //MEMB.8 | num | name | colour | type(1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 } rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | AUTOMATIC | load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }
      //MEMB.8 | num | name | colour | type(1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 } rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | EFF_LEN | lyy | lzz | llt | load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }
      //MEMB.8 | num | name | colour | type(1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 } rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | EXPLICIT | num_pt | { pt | rest | } | num_span | { span | rest | } load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }
      AddItems(ref items, record.Name, Colour.NO_RGB.ToString(), record.Type.GetStringValue(), record.Exposure.ToString(), record.PropertyIndex ?? 0, 
        record.Group ?? 0, AddTopology(), record.OrientationNodeIndex ?? 0, record.Angle, record.MeshSize ?? 0, 
        record.IsIntersector ? "YES" : "NO", AddAnalysisType(), (int)record.Fire, record.LimitingTemperature ?? 0, record.CreationFromStartDays, 
        record.StartOfDryingDays, record.AgeAtLoadingDays, record.RemovedAtDays, record.Dummy ? "DUMMY" : "ACTIVE");

      if (record.Type == MemberType.Beam || record.Type == MemberType.Generic1d || record.Type == MemberType.Column || record.Type == MemberType.Void1d)
      {
        var axisDirs = Enum.GetValues(typeof(AxisDirection6)).Cast<AxisDirection6>().Where(v => v != AxisDirection6.NotSet).ToList();
        AddEndReleaseItems(ref items, record.Releases1, record.Stiffnesses1, axisDirs);
        AddEndReleaseItems(ref items, record.Releases2, record.Stiffnesses2, axisDirs);

        AddItems(ref items, record.RestraintEnd1.GetStringValue(), record.RestraintEnd2.GetStringValue(), record.EffectiveLengthType.GetStringValue());

        if (record.EffectiveLengthType == EffectiveLengthType.EffectiveLength)
        {
          AddItems(ref items,
            AddEffectiveLength(record.EffectiveLengthYY, record.PercentageYY),
            AddEffectiveLength(record.EffectiveLengthZZ, record.PercentageZZ),
            AddEffectiveLength(record.EffectiveLengthLateralTorsional, 
            record.FractionLateralTorsional));
        }
        else if (record.EffectiveLengthType == EffectiveLengthType.Explicit)
        {
          AddExplicitItems(ref items, record.PointRestraints);
          AddExplicitItems(ref items, record.SpanRestraints);
        }

        AddItems(ref items, record.LoadHeight ?? 0, record.LoadHeightReferencePoint.GetStringValue(), record.MemberHasOffsets ? "OFF" : "NO_OFF");

        if (record.MemberHasOffsets)
        {
          AddItems(ref items, AddAutoOrMan(record.End1AutomaticOffset), AddAutoOrMan(record.End2AutomaticOffset), record.End1OffsetX ?? 0, 
            record.End2OffsetX ?? 0, record.OffsetY ?? 0, record.OffsetZ ?? 0);
        }
      }
      else if (record.Type == MemberType.Generic2d || record.Type == MemberType.Slab || record.Type == MemberType.Wall || record.Type == MemberType.Void2d)
      {
        AddItems(ref items, record.Offset2dZ ?? 0, record.OffsetAutomaticInternal ? "YES" : "NO", "REBAR_2D.1", 0, 0, 0);
      }
      else
      {
        return false;
      }

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region to_gwa_fns

    private string AddTopology()
    {
      var topoPortions = new List<string>
      {
        string.Join(" ", record.NodeIndices)
      };

      if (record.Voids != null && record.Voids.Count() > 0 && record.Voids.Any(v => v != null && v.Count() > 0))
      {
        var topoVoids = new List<string>();
        foreach (var vList in record.Voids.Where(v => v != null))
        {
          topoVoids.Add("V(" + string.Join(" ", vList) + ")");
        }
        topoPortions.Add(string.Join(" ", topoVoids));
      }

      if (record.PointNodeIndices != null && record.PointNodeIndices.Count() > 0)
      {
        topoPortions.Add("P(" + string.Join(" ", record.PointNodeIndices) + ")");
      }

      if (record.Polylines != null && record.Polylines.Count() > 0)
      {
        topoPortions.Add("L(" + string.Join(" ", record.PointNodeIndices) + ")");
      }

      if (record.AdditionalAreas != null && record.AdditionalAreas.Count() > 0 && record.AdditionalAreas.Any(v => v != null && v.Count() > 0))
      {
        var topoAdditional = new List<string>();
        foreach (var vList in record.AdditionalAreas.Where(v => v != null))
        {
          topoAdditional.Add("V(" + string.Join(" ", vList) + ")");
        }
        topoPortions.Add(string.Join(" ", topoAdditional));
      }
      return string.Join(" ", topoPortions);
    }

    private string AddTime()
    {
      return string.Join(" ", new[] { record.CreationFromStartDays, record.StartOfDryingDays, record.AgeAtLoadingDays, record.RemovedAtDays });
    }

    private string AddAnalysisType()
    {
      //TO DO: some validation here to ensure a valid combination of MemberType and AnalysisType
      return record.AnalysisType.ToString();
    }

    private string AddAutoOrMan(bool val)
    {
      return (val ? "AUTO" : "MAN");
    }

    private string AddEffectiveLength(double? el, double? fraction)
    {
      return ((el == null || el.Value == 0) && fraction.HasValue && fraction.Value > 0)
        ? (fraction.Value + "%")
        : (el.HasValue)
            ? el.Value.ToString()
            : 0.ToString();
    }

    #region other_to_gwa_add_x_Items_fns

    private void AddExplicitItems(ref List<string> items, List<RestraintDefinition> restraintDefinitions)
    {
      items.Add(restraintDefinitions.Count().ToString());
      //Let 0 mean "all" too in light of the fact that all is written as 0 in the GWA
      var allDef = restraintDefinitions.Where(rd => rd.All || rd.Index == 0);

      if (allDef.Count() > 0)
      {
        items.AddRange(new[] { 0.ToString(), allDef.First().Restraint.GetStringValue() });
        return;
      }
      var orderedRestrDef = restraintDefinitions.OrderBy(rd => rd.Index).ToList();
      foreach (var rd in orderedRestrDef)
      {
        items.AddRange(new[] { rd.Index.ToString(), rd.Restraint.GetStringValue() });
      }
      return;
    }
    #endregion
    #endregion

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }
    private bool AddTopology(string v)
    {
      var bracketPieces = v.Split(new[] { '(', ')' }).Select(s => s.Trim()).ToList();
      if (bracketPieces.Count() > 1)
      {
        var listTypes = bracketPieces.Take(bracketPieces.Count() - 1).Select(bp => bp.Last()).ToList();
        for (var i = 0; i < listTypes.Count(); i++)
        {
          switch (char.ToUpper(listTypes[i]))
          {
            case 'V':
              if (record.Voids == null)
              {
                record.Voids = new List<List<int>>();
              }
              record.Voids.Add(StringToIntList(bracketPieces[i + 1]));
              break;
            case 'P':
              record.PointNodeIndices = StringToIntList(bracketPieces[i + 1]);
              break;
            case 'L':
              if (record.Polylines == null)
              {
                record.Polylines = new List<List<int>>();
              }
              record.Polylines.Add(StringToIntList(bracketPieces[i + 1]));
              break;
            case 'A':
              if (record.AdditionalAreas == null)
              {
                record.AdditionalAreas = new List<List<int>>();
              }
              record.AdditionalAreas.Add(StringToIntList(bracketPieces[i + 1]));
              break;
          }
        }
      }
      record.NodeIndices = StringToIntList(bracketPieces[0]);
      return true;
    }

    private bool AddFire(string v)
    {
      if (int.TryParse(v, out int fireMinutes))
      {
        record.Fire = (FireResistance)fireMinutes;
        return true;
      }
      return false;
    }

    private bool AddDummy(string v)
    {
      record.Dummy = v.Equals("DUMMY", StringComparison.InvariantCultureIgnoreCase);
      return true;
    }

    private bool AddEffectiveLength(string v, ref double? el, ref double? perc)
    {
      var val = v.Trim();
      double doubleVal;
      if (val.Contains("%"))
      {
        if (double.TryParse(val.Substring(0, val.Length - 1), out doubleVal))
        {
          perc = doubleVal;
          return true;
        }
        else
        {
          return false;
        }
      }
      else if (double.TryParse(val, out doubleVal))
      {
        el = doubleVal;
        return true;
      }
      else
      {
        return false;
      }
    }

    private bool ProcessExplicit(List<string> items, out List<string> remainingItems)
    {
      remainingItems = items; //default in case of early exit of this method
      //Assume the first item in the items list is the num points value in the GWA format for MEMB
      var itemIndex = 0;
      if (!int.TryParse(items[itemIndex++], out var numPts))
      {
        return false;
      }
      for (var i = 0; i < numPts; i++)
      {
        if (!int.TryParse(items[itemIndex++], out var ptIndex) || !items[itemIndex++].TryParseStringValue(out Restraint restraint))
        {
          return false;
        }
        if (record.PointRestraints == null)
        {
          record.PointRestraints = new List<RestraintDefinition>();
        }
        record.PointRestraints.Add(new RestraintDefinition() { All = (ptIndex == 0), Index = (ptIndex == 0) ? null : (int?)ptIndex, Restraint = restraint });
      }

      if (!int.TryParse(items[itemIndex++], out var numSpans))
      {
        return false;
      }
      for (var i = 0; i < numSpans; i++)
      {
        if (!int.TryParse(items[itemIndex++], out var spanIndex) || !items[itemIndex++].TryParseStringValue(out Restraint restraint))
        {
          return false;
        }
        if (record.SpanRestraints == null)
        {
          record.SpanRestraints = new List<RestraintDefinition>();
        }
        record.SpanRestraints.Add(new RestraintDefinition() { All = (spanIndex == 0), Index = (spanIndex == 0) ? null : (int?)spanIndex, Restraint = restraint });
      }
      remainingItems = items.Skip(itemIndex).ToList();
      return true;
    }

    private bool AddIsOffset(string v)
    {
      record.MemberHasOffsets = (v.Equals("off", StringComparison.InvariantCultureIgnoreCase));
      return true;
    }

    private bool ProcessOffsets(List<string> items)
    {
      if (items.Count() < 6)
      {
        return false;
      }
      record.End1AutomaticOffset = items[0].Equals("AUTO", StringComparison.InvariantCultureIgnoreCase);
      record.End2AutomaticOffset = items[1].Equals("AUTO", StringComparison.InvariantCultureIgnoreCase);

      items = items.Skip(2).ToList();

      var offsets = new double?[4];
      for (int i = 0; i < 4; i++)
      {
        double? val = null;
        if (!AddNullableDoubleValue(items[i], out val))
        {
          return false;
        }
        if (val.HasValue && val > 0)
        {
          offsets[i] = val;
        }
      }

      record.End1OffsetX = offsets[0];
      record.End2OffsetX = offsets[1];
      record.OffsetY = offsets[2];
      record.OffsetZ = offsets[3];

      return true;
    }

    #endregion

  }

  /*
  public struct RestraintDefinition
  {
    public bool All;
    public int? Index;
    public Restraint Restraint;
  }
  */

}
