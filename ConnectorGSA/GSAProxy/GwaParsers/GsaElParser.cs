using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.EL, GwaSetCommandType.Set, true, false, true, GwaKeyword.NODE)]
  public class GsaElParser : GwaParser<GsaEl>
  {
    public GsaElParser(GsaEl gsaEl) : base(gsaEl) { }

    public GsaElParser() : base(new GsaEl()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;

      // EL.4 | num | name | colour | type | prop | group | topo() | orient_node | orient_angle | is_rls { | rls { | k } } off_x1 | off_x2 | off_y | off_z | dummy | parent
      if (!FromGwaByFuncs(items, out remainingItems, AddName, (v) => AddColour(v, out record.Colour), 
        (v) => v.TryParseStringValue(out record.Type), AddProp, (v) => AddNullableIntValue(v, out record.Group)))
      {
        return false;
      }
      items = remainingItems;

      //Using the int value of the enumerated Type value to determine how many indices to look for
      var numNodes = NumNodes();
      if (!ProcessTopology(items, out remainingItems, numNodes))
      {
        return false;
      }
      items = remainingItems;

      if (!FromGwaByFuncs(items, out remainingItems, (v) => AddNullableIndex(v, out record.OrientationNodeIndex), 
        (v) => AddNullableDoubleValue(v, out record.Angle), (v) => v.TryParseStringValue(out record.ReleaseInclusion)))
      {
        return false;
      }
      items = remainingItems;

      if (record.ReleaseInclusion == ReleaseInclusion.Included)
      {
        //This assumes that rls_1 { | k_1 } rls_2 { | k_2 } is at the start of the items list
        if (!ProcessReleases(items, out remainingItems, ref record.Releases1, ref record.Stiffnesses1, ref record.Releases2, ref record.Stiffnesses2))
        {
          return false;
        }
        items = remainingItems;
      }

      if (!ProcessOffsets(items, out remainingItems))
      {
        return false;
      }
      items = remainingItems;

      //The only two fields that might be remaining are DUMMY and PARENT, but these are optional so only process them if present
      if (items.Count() == 0)
      {
        return true;
      }
      else if (items.Count() > 0)
      {
        if (!AddDummy(items.First()))
        {
          return false;
        }
      }
      if (items.Count() == 2)
      {
        if (!AddNullableIndex(items[1], out record.ParentIndex))
        {
          return false;
        }
      }
      return true;
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      gwa = new List<string>();
      //Just supporting non-void 1D types at this stage
      if (!InitialiseGwa(includeSet, out var items))
      {
        return false;
      }

      // EL.4 | num | name | colour | type | prop | group | topo() | orient_node | orient_angle | is_rls { | rls { | k } } off_x1 | off_x2 | off_y | off_z | dummy | parent
      AddItems(ref items, record.Name, Colour.NO_RGB.ToString(), record.Type.GetStringValue(), AddProp(), (record.Group ?? 0));

      //Again uses the integer value of the Type - this time to determine how many node index items to add
      var numNodes = NumNodes();
      if (record.NodeIndices == null)
      {
        items.Add("");
      }
      else
      {
        for (var i = 0; i < Math.Min(numNodes, record.NodeIndices.Count()) ; i++)
        {
          items.Add(((i >= record.NodeIndices.Count()) ? 0 : record.NodeIndices[i]).ToString());
        }
      }

      AddItems(ref items, record.OrientationNodeIndex ?? 0, record.Angle ?? 0, record.ReleaseInclusion.GetStringValue());

      if (record.ReleaseInclusion == ReleaseInclusion.Included)
      {
        var axisDirs = Enum.GetValues(typeof(AxisDirection6)).Cast<AxisDirection6>().Where(v => v != AxisDirection6.NotSet).ToList();
        AddEndReleaseItems(ref items, record.Releases1, record.Stiffnesses1, axisDirs);
        AddEndReleaseItems(ref items, record.Releases2, record.Stiffnesses2, axisDirs);
      }
      AddItems(ref items, (record.End1OffsetX ?? 0), (record.End2OffsetX ?? 0), (record.OffsetY ?? 0), (record.OffsetZ ?? 0));

      if (record.ParentIndex.HasValue && record.ParentIndex.Value > 0)
      {
        AddItems(ref items, record.Dummy ? "DUMMY" : "", (record.ParentIndex ?? 0));
      }
      else if (record.Dummy)
      {
        AddItems(ref items, "DUMMY");
      }

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region to_gwa_fns

    private string AddProp()
    {
      //If there is any issue with the percentage values (i.e. if they're null somehow) then just ignore them and return the default (which is just the prop value)
      //which infers a 0-100% range for the tapers
      if (!record.TaperOffsetPercentageEnd1.HasValue || !record.TaperOffsetPercentageEnd2.HasValue
        || (record.TaperOffsetPercentageEnd1.Value == 0 && record.TaperOffsetPercentageEnd2.Value == 100))
      {
        return (record.PropertyIndex ?? 0).ToString();
      }

      return ((record.PropertyIndex ?? 0).ToString() + "[" + Math.Round((record.TaperOffsetPercentageEnd1 ?? 0) / 100, 6) 
        + ":" + Math.Round((record.TaperOffsetPercentageEnd2 ?? 0) / 100, 6) + "]");
    }

    #endregion

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    private bool AddProp(string v)
    {
      var pieces = v.Split('[', ']');
      if (pieces.Count() == 0 || !AddNullableIndex(v, out record.PropertyIndex))
      {
        return false;
      }

      if (!AddNullableIndex(pieces[0], out record.PropertyIndex))
      {
        return false;
      }

      if (pieces.Count() > 1)
      {
        var tapers = pieces[1].Split(':');
        if (tapers.Count() == 1)
        {
          record.TaperOffsetPercentageEnd1 = 0;
          record.TaperOffsetPercentageEnd2 = 100;
        }
        else if (tapers.Count() == 2 && double.TryParse(tapers[0], out double end1Taper) && double.TryParse(tapers[1], out double end2Taper))
        {
          record.TaperOffsetPercentageEnd1 = end1Taper * 100;
          record.TaperOffsetPercentageEnd2 = end2Taper * 100;
        }
        else
        {
          return false;
        }
      }
      return true;
    }

    private bool ProcessOffsets(List<string> items, out List<string> remainingItems)
    {
      remainingItems = items; //in case of early exit from this method
      var offsets = new double?[4];
      for (var i = 0; i < 4; i++)
      {
        if (!AddNullableDoubleValue(items[i], out var val))
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

      remainingItems = items.Skip(4).ToList();

      return true;
    }
    private bool ProcessTopology(List<string> items, out List<string> remainingItems, int numIndices)
    {
      remainingItems = items;
      if (items.Count() < numIndices)
      {
        return false;
      }
      for (var i = 0; i < numIndices; i++)
      {
        if (!AddNullableIntValue(items[i], out var foundIndex) && foundIndex.HasValue)
        {
          return false;
        }
        if (record.NodeIndices == null)
        {
          record.NodeIndices = new List<int>();
        }
        if (foundIndex.Value > 0)
        {
          record.NodeIndices.Add(foundIndex.Value);
        }
      }
      remainingItems = items.Skip(numIndices).ToList();
      return true;
    }

    private bool AddDummy(string v)
    {
      record.Dummy = v.Equals("DUMMY", StringComparison.InvariantCultureIgnoreCase);
      return true;
    }
    #endregion

    #region other_fns
    private int NumNodes()
    {
      switch (record.Type)
      {
        case ElementType.Brick8:
        case ElementType.Quad8:
          return 8;

        case ElementType.Triangle6:
        case ElementType.Wedge6:
          return 6;

        case ElementType.Pyramid5: return 5;

        case ElementType.Quad4:
        case ElementType.Tetra4:
          return 4;

        case ElementType.Triangle3: return 3;

        default: return 2;
      }
    }
    #endregion
  }
}
