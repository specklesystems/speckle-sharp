using Speckle.GSA.API;
using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.GSA.API.GwaSchema;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.ASSEMBLY, GwaSetCommandType.Set, true, GwaKeyword.NODE, GwaKeyword.MEMB, GwaKeyword.EL, GwaKeyword.GRID_PLANE)]
  public class GsaAssemblyParser : GwaParser<GsaAssembly>
  {
    public GsaAssemblyParser(GsaAssembly gsaAssembly) : base(gsaAssembly) { }

    public GsaAssemblyParser() : base(new GsaAssembly()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;

      //ASSEMBLY.3 | num | name | type | entity | topo_1 | topo_2 |
      return FromGwaByFuncs(items, out _, AddName, AddType, (v) => AddEntities(v, out record.MemberIndices, out record.ElementIndices), 
        (v) => AddNullableIndex(v, out record.Topo1), (v) => AddNullableIndex(v, out record.Topo2),
        // orient_node | int_topo | size_y | size_z | curve_type | 
        (v) => AddNullableIndex(v, out record.OrientNode), AddIntTopo, (v) => double.TryParse(v, out record.SizeY), (v) => double.TryParse(v, out record.SizeZ), AddCurveType,
        // curve_order | point_defn | points
        (v) => true, (v) => Enum.TryParse(v, true, out record.PointDefn), AddPoints);
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //ASSEMBLY.3 | num | name | type | entity | topo_1 | topo_2 | orient_node | int_topo | size_y | size_z | curve_type | curve_order | point_defn | points
      AddItems(ref items, record.Name, AddType(), AddEntities(record.MemberIndices, record.ElementIndices), record.Topo1, record.Topo2, record.OrientNode, AddIntTopo(), 
        record.SizeY, record.SizeZ, AddCurveType(), record.CurveOrder ?? 0, AddPointDefn(), AddPoints());

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region to_gwa_fns
    private string AddType()
    {
      if (record.Type == GSAEntity.MEMBER)
      {
        return "MEMBER";
      }
      else if (record.Type == GSAEntity.ELEMENT)
      {
        return "ELEMENT";
      }
      else if (Instance.GsaModel.StreamLayer == GSALayer.Design)
      {
        return "MEMBER";
      }
      else
      {
        return "ELEMENT";
      }
    }

    private string AddIntTopo()
    {
      return string.Join(" ", record.IntTopo);
    }

    private string AddCurveType()
    {
      return (record.CurveType == CurveType.Circular) ? "CIRCULAR" : "LAGRANGE";
    }

    private string AddPointDefn()
    {
      var pd = (record.PointDefn == PointDefinition.NotSet) ? PointDefinition.Points : record.PointDefn;   //Default to points
      return pd.ToString().ToUpper();
    }

    private string AddPoints()
    {
      if (record.PointDefn == PointDefinition.Spacing)
      {
        return record.Spacing.ToString();
      }
      else if (record.PointDefn == PointDefinition.Storey)
      {
        if (record.StoreyIndices != null && record.StoreyIndices.Count() > 0)
        {
          var allOrderedStoreyIndices = GetStoreyIndices();
          var orderedStoreyIndices = record.StoreyIndices.OrderBy(i => i).ToList();
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
      else if (record.PointDefn == PointDefinition.Explicit)
      {
        return string.Join(" ", record.ExplicitPositions);
      }
      else  //Default to points again, and just the endpoints
      {
        return 2.ToString();
      }
    }

    private List<int> GetStoreyIndices()
    {
      //Since there is no way in the GSA COM API to resolve list specification ("1 2 to 8" etc) of grid surfaces, the cache needs to be used
      var allGridPlaneIndices = Instance.GsaModel.Cache.LookupIndices<GsaGridPlane>().ToList();
      var storeyIndices = new List<int>();

      return storeyIndices.Distinct().OrderBy(i => i).ToList();
    }
    #endregion

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    private bool AddType(string v)
    {
      if (v.Equals("member", StringComparison.InvariantCultureIgnoreCase))
      {
        record.Type = GSAEntity.MEMBER;
      }
      else if (v.Equals("element", StringComparison.InvariantCultureIgnoreCase))
      {
        record.Type = GSAEntity.ELEMENT;
      }
      else
      {
        record.Type = GSAEntity.NotSet;
      }
      return true;
    }

    private bool AddIntTopo(string v)
    {
      var nodeIndices = Instance.GsaModel.Proxy.ConvertGSAList(v, GSAEntity.NODE);
      record.IntTopo = nodeIndices.ToList();
      return true;
    }

    private bool AddCurveType(string v)
    {
      record.CurveType = (v.Equals("circular", StringComparison.InvariantCultureIgnoreCase))
        ? record.CurveType = CurveType.Circular
        : (v.Equals("lagrange", StringComparison.InvariantCultureIgnoreCase))
          ? record.CurveType = CurveType.Lagrange
          : CurveType.NotSet;
      return true;
    }

    private bool AddPoints(string v)
    {
      if (record.PointDefn == PointDefinition.Spacing)
      {
        return AddNullableDoubleValue(v, out record.Spacing);
      }
      else if (record.PointDefn == PointDefinition.Storey)
      {
        //So far only specific numbers are recognised
        record.StoreyIndices = StringToIntList(v);
        return record.StoreyIndices.Count() > 0;
      }
      else if (record.PointDefn == PointDefinition.Explicit)
      {
        //So far only specific numbers are recognised
        record.ExplicitPositions = StringToDoubleList(v);
        return record.ExplicitPositions.Count() > 0;
      }
      else
      {
        return AddNullableIntValue(v, out record.NumberOfPoints);
      }
    }
    #endregion
  }
}
