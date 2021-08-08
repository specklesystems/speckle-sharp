using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.GSA.API.GwaSchema
{
  [GsaType(GwaKeyword.ASSEMBLY, GwaSetCommandType.Set, true, GwaKeyword.NODE, GwaKeyword.MEMB, GwaKeyword.EL, GwaKeyword.GRID_PLANE)]
  public partial class GsaAssembly : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public GSAEntity Type;
    public List<int> Entities = new List<int>();
    public int? Topo1;
    public int? Topo2;
    public int? OrientNode;
    public List<int> IntTopo = new List<int>();
    public double SizeY;
    public double SizeZ;
    public CurveType CurveType;
    public int? CurveOrder;
    public PointDefinition PointDefn;
    //Only one of these will be set, according to the PointDefn value
    public int? NumberOfPoints;
    public double? Spacing;
    public List<int> StoreyIndices = new List<int>();
    public List<double> ExplicitPositions = new List<double>();

    public GsaAssembly() : base()
    {
      Version = 3;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;

      //ASSEMBLY.3 | num | name | type | entity | topo_1 | topo_2 |
      return FromGwaByFuncs(items, out _, AddName, AddType, AddEntities, (v) => AddNullableIndex(v, out Topo1), (v) => AddNullableIndex(v, out Topo2),
        // orient_node | int_topo | size_y | size_z | curve_type | 
        (v) => AddNullableIndex(v, out OrientNode), AddIntTopo, (v) => double.TryParse(v, out SizeY), (v) => double.TryParse(v, out SizeZ), AddCurveType,
        // curve_order | point_defn | points
        (v) => true, (v) => Enum.TryParse(v, true, out PointDefn), AddPoints);
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //ASSEMBLY.3 | num | name | type | entity | topo_1 | topo_2 | orient_node | int_topo | size_y | size_z | curve_type | curve_order | point_defn | points
      AddItems(ref items, Name, AddType(), AddEntities(), Topo1, Topo2, OrientNode, AddIntTopo(), SizeY, SizeZ, AddCurveType(), CurveOrder ?? 0, AddPointDefn(), AddPoints());

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region to_gwa_fns
    private string AddType()
    {
      if (Type == GSAEntity.MEMBER)
      {
        return "MEMBER";
      }
      else if (Type == GSAEntity.ELEMENT)
      {
        return "ELEMENT";
      }
      else if (Instance.GsaModel.Layer == GSALayer.Design)
      {
        return "MEMBER";
      }
      else
      {
        return "ELEMENT";
      }
    }

    public string AddEntities()
    {
      //The old mechanism of using "G_" in the entities field to signify members is understood to be superseded by the inclusion of the entity type
      //parameter as of version 3.

      var allIndices = Instance.GsaModel.LookupIndices(Type == GSAEntity.MEMBER ? GetKeyword<GsaMemb>() : GetKeyword<GsaEl>())
        .Distinct().OrderBy(i => i).ToList();

      if (Entities.Distinct().OrderBy(i => i).SequenceEqual(allIndices))
      {
        return "all";
      }
      return string.Join(" ", Entities);
    }

    private string AddIntTopo()
    {
      return string.Join(" ", IntTopo);
    }

    private string AddCurveType()
    {
      return (CurveType == CurveType.Circular) ? "CIRCULAR" : "LAGRANGE";
    }

    private string AddPointDefn()
    {
      var pd = (PointDefn == PointDefinition.NotSet) ? PointDefinition.Points : PointDefn;   //Default to points
      return pd.ToString().ToUpper();
    }

    private string AddPoints()
    {
      if (PointDefn == PointDefinition.Spacing)
      {
        return Spacing.ToString();
      }
      else if (PointDefn == PointDefinition.Storey)
      {
        if (StoreyIndices != null && StoreyIndices.Count() > 0)
        {
          var allOrderedStoreyIndices = GetStoreyIndices();
          var orderedStoreyIndices = StoreyIndices.OrderBy(i => i).ToList();
          if (orderedStoreyIndices.SequenceEqual(allOrderedStoreyIndices))
          {
            return "all";
          }
          else
          {
            return string.Join(" ", orderedStoreyIndices.Intersect(allOrderedStoreyIndices));
          }
        }
        return "all";
      }
      else if (PointDefn == PointDefinition.Explicit)
      {
        return string.Join(" ", ExplicitPositions);
      }
      else  //Default to points again, and just the endpoints
      {
        return 2.ToString();
      }
    }

    private List<int> GetStoreyIndices()
    {
      //Since there is no way in the GSA COM API to resolve list specification ("1 2 to 8" etc) of grid surfaces, the cache needs to be used
      var gridPlaneKw = GetKeyword<GsaGridPlane>();
      var allGridPlaneIndices = Instance.GsaModel.LookupIndices(gridPlaneKw).ToList();
      var storeyIndices = new List<int>();
      /* TO DO - solve the issue of getting the GWA in this new repo
      foreach (var i in allGridPlaneIndices)
      {
        var planeGwas = Initialiser.AppResources.Cache.GetGwa(gridPlaneKw, i);
        if (planeGwas != null && planeGwas.Count() > 0)
        {
          var gsaGridPlane = new GsaGridPlane();
          gsaGridPlane.FromGwa(planeGwas.First());
          if (gsaGridPlane.Type == GridPlaneType.Storey && gsaGridPlane.Index.HasValue)
          {
            storeyIndices.Add(gsaGridPlane.Index.Value);
          }
        }
      }
      */
      return storeyIndices.Distinct().OrderBy(i => i).ToList();
    }
    #endregion

    #region from_gwa_fns
    private bool AddType(string v)
    {
      if (v.Equals("member", StringComparison.InvariantCultureIgnoreCase))
      {
        Type = GSAEntity.MEMBER;
      }
      else if (v.Equals("element", StringComparison.InvariantCultureIgnoreCase))
      {
        Type = GSAEntity.ELEMENT;
      }
      else
      {
        Type = GSAEntity.NotSet;
      }
      return true;
    }

    private bool AddEntities(string v)
    {
      Entities = Instance.GsaModel.ConvertGSAList(v.Replace("G", ""), Type).ToList();
      return Entities != null;
    }

    private bool AddIntTopo(string v)
    {
      var nodeIndices = Instance.GsaModel.ConvertGSAList(v, GSAEntity.NODE);
      IntTopo = nodeIndices.ToList();
      return true;
    }

    private bool AddCurveType(string v)
    {
      CurveType = (v.Equals("circular", StringComparison.InvariantCultureIgnoreCase))
        ? CurveType = CurveType.Circular
        : (v.Equals("lagrange", StringComparison.InvariantCultureIgnoreCase))
          ? CurveType = CurveType.Lagrange
          : CurveType.NotSet;
      return true;
    }

    private bool AddPoints(string v)
    {
      if (PointDefn == PointDefinition.Spacing)
      {
        return AddNullableDoubleValue(v, out Spacing);
      }
      else if (PointDefn == PointDefinition.Storey)
      {
        //So far only specific numbers are recognised
        StoreyIndices = StringToIntList(v);
        return StoreyIndices.Count() > 0;
      }
      else if (PointDefn == PointDefinition.Explicit)
      {
        //So far only specific numbers are recognised
        ExplicitPositions = StringToDoubleList(v);
        return ExplicitPositions.Count() > 0;
      }
      else
      {
        return AddNullableIntValue(v, out NumberOfPoints);
      }
    }

    #endregion
  }
}
