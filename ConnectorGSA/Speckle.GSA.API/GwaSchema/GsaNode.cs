using System;
using System.Collections.Generic;
using System.Linq;


namespace Speckle.GSA.API.GwaSchema
{
  //The load case keyword dependency is there for the creation of results
  [GsaType(GwaKeyword.NODE, GwaSetCommandType.Set, true, true, true, GwaKeyword.AXIS, GwaKeyword.PROP_MASS, GwaKeyword.PROP_SPR, GwaKeyword.LOAD_TITLE)]
  public class GsaNode : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public Colour Colour = Colour.NO_RGB;
    public double X;
    public double Y;
    public double Z;
    public NodeRestraint NodeRestraint;
    public List<AxisDirection6> Restraints;
    public NodeAxisRefType AxisRefType;
    public int? AxisIndex;
    public double? MeshSize;
    public int? SpringPropertyIndex;
    public int? MassPropertyIndex;
   

    private static readonly List<AxisDirection6> axisDirs = Enum.GetValues(typeof(AxisDirection6)).Cast<AxisDirection6>().Where(e => e != AxisDirection6.NotSet).ToList();
    //Damper property is left out at this point

    public GsaNode() : base()
    {
      //Defaults
      Version = 3;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;
      //NODE.3 | num | name | colour | x | y | z | restraint | axis | mesh_size | springProperty | massProperty | damperProperty
      //Zero values are valid for origin, but not for vectors below
      if (!FromGwaByFuncs(items, out remainingItems, AddName, null, (v) => double.TryParse(v, out X), (v) => double.TryParse(v, out Y), (v) => double.TryParse(v, out Z)))
      {
        return false;
      }
      items = remainingItems;
      if (items.Count() > 0)
      {
        FromGwaByFuncs(items, out remainingItems, AddRestraints);
      }

      items = remainingItems;
      if (items.Count() > 0)
      {
        FromGwaByFuncs(items, out remainingItems, AddAxis);
      }
      else
      {
        AxisRefType = NodeAxisRefType.Global;
      }

      items = remainingItems;
      if (items.Count() > 0)
      {
        FromGwaByFuncs(items, out remainingItems, (v) => AddNullableDoubleValue(v, out MeshSize));
      }

      items = remainingItems;
      if (items.Count() > 0)
      {
        FromGwaByFuncs(items, out remainingItems, (v) => AddNullableIndex(v, out SpringPropertyIndex));
      }

      items = remainingItems;
      if (items.Count() > 0)
      {
        FromGwaByFuncs(items, out remainingItems, (v) => AddNullableIndex(v, out MassPropertyIndex));
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

      //NODE.3 | num | name | colour | x | y | z | restraint | axis | mesh_size | springProperty | massProperty | damperProperty
      AddItems(ref items, Name, "NO_RGB", X, Y, Z);

      int numRemainingParameters = 0;
      if (MassPropertyIndex.HasValue && MassPropertyIndex.Value > 0)
      {
        numRemainingParameters = 5;
      }
      else if (SpringPropertyIndex.HasValue && SpringPropertyIndex.Value > 0)
      {
        numRemainingParameters = 4;
      }
      else if (MeshSize.HasValue && MeshSize.Value > 0)
      {
        numRemainingParameters = 3;
      }
      else if (AxisRefType == NodeAxisRefType.XElevation || AxisRefType == NodeAxisRefType.YElevation || AxisRefType == NodeAxisRefType.Vertical || AxisRefType == NodeAxisRefType.Reference)
      {
        numRemainingParameters = 2;
      }
      else if ((Restraints != null && Restraints.Count() > 0) || NodeRestraint != NodeRestraint.Free)
      {
        numRemainingParameters = 1;
      }

      if (numRemainingParameters >= 1)
      {
        AddItems(ref items, AddRestraints());
      }
      if (numRemainingParameters >= 2)
      {
        AddItems(ref items, AddAxis());
      }
      if (numRemainingParameters >= 3)
      {
        AddItems(ref items, MeshSize ?? 0);
      }
      if (numRemainingParameters >= 4)
      {
        AddItems(ref items, SpringPropertyIndex ?? 0);
      }
      if (numRemainingParameters >= 5)
      {
        AddItems(ref items, MassPropertyIndex ?? 0);
      }

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region to_gwa_fns

    private string AddRestraints()
    {
      if (NodeRestraint == NodeRestraint.Custom)
      {
        var custom = "";
        foreach (var r in Restraints)
        {
          custom += r.ToString().ToLowerInvariant();
        }
        return custom;
      }
      else
      {
        return NodeRestraint.ToString().ToLowerInvariant();
      }
    }

    private string AddAxis()
    {
      if (AxisRefType == NodeAxisRefType.Reference)
      {
        return AxisIndex.ToString();
      }
      else if (AxisRefType == NodeAxisRefType.NotSet)
      {
        return NodeAxisRefType.Global.ToString().ToUpperInvariant();
      }
      return AxisRefType.ToString().ToUpperInvariant();
    }
    #endregion

    #region from_gwa_fns
    private bool AddRestraints(string v)
    {
      var boolRestraints = v.RestraintBoolArrayFromCode();
      if (boolRestraints == null || boolRestraints.Count() < 6)
      {
        return false;
      }
      if (boolRestraints.All(r => r == false))
      {
        NodeRestraint = NodeRestraint.Free;
      }
      else if (boolRestraints.Take(3).All(r => r == true) && boolRestraints.Skip(3).Take(3).All(r => r == false))
      {
        NodeRestraint = NodeRestraint.Pin;
      }
      else if (boolRestraints.All(r => r == true))
      {
        NodeRestraint = NodeRestraint.Fix;
      }
      else
      {
        NodeRestraint = NodeRestraint.Custom;
        for (var i = 0; i < 6; i++)
        {
          if (boolRestraints[i])
          {
            if (Restraints == null)
            {
              Restraints = new List<AxisDirection6>();
            }
            //This list signifies the true/positive values
            Restraints.Add(axisDirs[i]);
          }
        }
      }

      return true;
    }

    private bool AddAxis(string v)
    {
      if (v.Trim().Equals("GLOBAL", StringComparison.InvariantCultureIgnoreCase))
      {
        AxisRefType = NodeAxisRefType.Global;
        return true;
      }
      if (v.Trim().Equals("X_ELEV", StringComparison.InvariantCultureIgnoreCase))
      {
        AxisRefType = NodeAxisRefType.XElevation;
        return true;
      }
      if (v.Trim().Equals("Y_ELEV", StringComparison.InvariantCultureIgnoreCase))
      {
        AxisRefType = NodeAxisRefType.YElevation;
        return true;
      }
      if (v.Trim().Equals("VERTICAL", StringComparison.InvariantCultureIgnoreCase))
      {
        AxisRefType = NodeAxisRefType.Vertical;
        return true;
      }
      else
      {
        AxisRefType = NodeAxisRefType.Reference;
        return AddNullableIndex(v, out AxisIndex);
      }
    }

    #endregion
  }
}
