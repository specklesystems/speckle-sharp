using System;
using System.Collections.Generic;
using System.Linq;


namespace Speckle.GSA.API.GwaSchema
{
  [GsaType(GwaKeyword.EL, GwaSetCommandType.Set, true, false, true, GwaKeyword.NODE)]
  public class GsaEl : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public Colour Colour = Colour.NO_RGB;
    public ElementType Type;
    public int? PropertyIndex;
    //The taper offsets aren't mentioned in the documentation but they are there encapsulated in square brackets in he property field
    public double? TaperOffsetPercentageEnd1;
    public double? TaperOffsetPercentageEnd2;
    public int? Group;
    public List<int> NodeIndices;           //Perimeter/edge topology, the number of which depends on int value for the ElementType value
    public int? OrientationNodeIndex;
    public double? Angle;  //Degrees - GWA also stores this in degrees
    public ReleaseInclusion ReleaseInclusion;
    public Dictionary<AxisDirection6, ReleaseCode> Releases1;
    public List<double> Stiffnesses1;
    public Dictionary<AxisDirection6, ReleaseCode> Releases2;
    public List<double> Stiffnesses2;
    public double? End1OffsetX;
    public double? End2OffsetX;
    public double? OffsetY;
    public double? OffsetZ;
    public bool Dummy;
    public int? ParentIndex;

    public GsaEl() : base()
    {
      Version = 4;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;

      // EL.4 | num | name | colour | type | prop | group | topo() | orient_node | orient_angle | is_rls { | rls { | k } } off_x1 | off_x2 | off_y | off_z | dummy | parent
      if (!FromGwaByFuncs(items, out remainingItems, (v) => AddName(v, out name), (v) => AddColour(v, out Colour), (v) => v.TryParseStringValue(out Type), AddProp, (v) => AddNullableIntValue(v, out Group)))
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

      if (!FromGwaByFuncs(items, out remainingItems, (v) => AddNullableIndex(v, out OrientationNodeIndex), (v) => AddNullableDoubleValue(v, out Angle), (v) => v.TryParseStringValue(out ReleaseInclusion)))
      {
        return false;
      }
      items = remainingItems;

      if (ReleaseInclusion == ReleaseInclusion.Included)
      {
        //This assumes that rls_1 { | k_1 } rls_2 { | k_2 } is at the start of the items list
        if (!ProcessReleases(items, out remainingItems, ref Releases1, ref Stiffnesses1, ref Releases2, ref Stiffnesses2))
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
        if (!AddNullableIndex(items[1], out ParentIndex))
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
      AddItems(ref items, Name, Colour.NO_RGB.ToString(), Type.GetStringValue(), AddProp(), (Group ?? 0));

      //Again uses the integer value of the Type - this time to determine how many node index items to add
      var numNodes = NumNodes();
      for (var i = 0; i < numNodes; i++)
      {
        items.Add(((i >= NodeIndices.Count()) ? 0 : NodeIndices[i]).ToString());
      }

      AddItems(ref items, OrientationNodeIndex ?? 0, Angle ?? 0, ReleaseInclusion.GetStringValue());

      if (ReleaseInclusion == ReleaseInclusion.Included)
      {
        var axisDirs = Enum.GetValues(typeof(AxisDirection6)).Cast<AxisDirection6>().Where(v => v != AxisDirection6.NotSet).ToList();
        AddEndReleaseItems(ref items, Releases1, Stiffnesses1, axisDirs);
        AddEndReleaseItems(ref items, Releases2, Stiffnesses2, axisDirs);
      }
      AddItems(ref items, (End1OffsetX ?? 0), (End2OffsetX ?? 0), (OffsetY ?? 0), (OffsetZ ?? 0));

      if (ParentIndex.HasValue && ParentIndex.Value > 0)
      {
        AddItems(ref items, Dummy ? "DUMMY" : "", (ParentIndex ?? 0));
      }
      else if (Dummy)
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
      if (!TaperOffsetPercentageEnd1.HasValue || !TaperOffsetPercentageEnd2.HasValue 
        || (TaperOffsetPercentageEnd1.Value == 0 && TaperOffsetPercentageEnd2.Value == 100))
      {
        return (PropertyIndex ?? 0).ToString();
      }

      return ((PropertyIndex ?? 0).ToString() + "[" + Math.Round((TaperOffsetPercentageEnd1 ?? 0) / 100, 6) + ":" + Math.Round((TaperOffsetPercentageEnd2 ?? 0) / 100, 6) + "]");
    }

    #endregion

    #region from_gwa_fns

    private bool AddProp(string v)
    {
      var pieces = v.Split('[', ']');
      if (pieces.Count() == 0 || !AddNullableIndex(v, out PropertyIndex))
      {
        return false;
      }

      if (!AddNullableIndex(pieces[0], out PropertyIndex))
      {
        return false;
      }

      if (pieces.Count() > 1)
      {
        var tapers = pieces[1].Split(':');
        if (tapers.Count() == 1)
        {
          TaperOffsetPercentageEnd1 = 0;
          TaperOffsetPercentageEnd2 = 100;
        }
        else if (tapers.Count() == 2 && double.TryParse(tapers[0], out double end1Taper) && double.TryParse(tapers[1], out double end2Taper))
        {
          TaperOffsetPercentageEnd1 = end1Taper * 100;
          TaperOffsetPercentageEnd2 = end2Taper * 100;
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

      End1OffsetX = offsets[0];
      End2OffsetX = offsets[1];
      OffsetY = offsets[2];
      OffsetZ = offsets[3];

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
        if (NodeIndices ==  null)
        {
          NodeIndices = new List<int>();
        }
        if (foundIndex.Value > 0)
        {
          NodeIndices.Add(foundIndex.Value);
        }
      }
      remainingItems = items.Skip(numIndices).ToList();
      return true;
    }

    private bool AddDummy(string v)
    {
      Dummy = v.Equals("DUMMY", StringComparison.InvariantCultureIgnoreCase);
      return true;
    }
    #endregion

    #region other_fns
    private int NumNodes()
    {
      switch (Type)
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
