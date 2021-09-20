using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  //The load case & load combination keyword dependencies are there for the creation of results
  [GsaType(GwaKeyword.NODE, GwaSetCommandType.Set, true, true, true, GwaKeyword.AXIS, GwaKeyword.PROP_MASS, GwaKeyword.PROP_SPR, GwaKeyword.LOAD_TITLE, GwaKeyword.COMBINATION)]
  public class GsaNodeParser : GwaParser<GsaNode>
  {
    private static readonly List<AxisDirection6> axisDirs = Enum.GetValues(typeof(AxisDirection6)).Cast<AxisDirection6>().Where(e => e != AxisDirection6.NotSet).ToList();
    //Damper property is left out at this point

    public GsaNodeParser(GsaNode gsaNode) : base(gsaNode) { }

    public GsaNodeParser() : base(new GsaNode()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;
      //NODE.3 | num | name | colour | x | y | z | restraint | axis | mesh_size | springProperty | massProperty | damperProperty
      //Zero values are valid for origin, but not for vectors below
      if (!FromGwaByFuncs(items, out remainingItems, AddName, null, 
        (v) => double.TryParse(v, out record.X), (v) => double.TryParse(v, out record.Y), (v) => double.TryParse(v, out record.Z)))
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
        record.AxisRefType = NodeAxisRefType.Global;
      }

      items = remainingItems;
      if (items.Count() > 0)
      {
        FromGwaByFuncs(items, out remainingItems, (v) => AddNullableDoubleValue(v, out record.MeshSize));
      }

      items = remainingItems;
      if (items.Count() > 0)
      {
        FromGwaByFuncs(items, out remainingItems, (v) => AddNullableIndex(v, out record.SpringPropertyIndex));
      }

      items = remainingItems;
      if (items.Count() > 0)
      {
        FromGwaByFuncs(items, out remainingItems, (v) => AddNullableIndex(v, out record.MassPropertyIndex));
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
      AddItems(ref items, record.Name, "NO_RGB", record.X, record.Y, record.Z);

      int numRemainingParameters = 0;
      if (record.MassPropertyIndex.HasValue && record.MassPropertyIndex.Value > 0)
      {
        numRemainingParameters = 5;
      }
      else if (record.SpringPropertyIndex.HasValue && record.SpringPropertyIndex.Value > 0)
      {
        numRemainingParameters = 4;
      }
      else if (record.MeshSize.HasValue && record.MeshSize.Value > 0)
      {
        numRemainingParameters = 3;
      }
      else if (record.AxisRefType == NodeAxisRefType.XElevation || record.AxisRefType == NodeAxisRefType.YElevation 
          || record.AxisRefType == NodeAxisRefType.Vertical || record.AxisRefType == NodeAxisRefType.Reference)
          {
        numRemainingParameters = 2;
      }
      else if ((record.Restraints != null && record.Restraints.Count() > 0) || record.NodeRestraint != NodeRestraint.Free)
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
        AddItems(ref items, record.MeshSize ?? 0);
      }
      if (numRemainingParameters >= 4)
      {
        AddItems(ref items, record.SpringPropertyIndex ?? 0);
      }
      if (numRemainingParameters >= 5)
      {
        AddItems(ref items, record.MassPropertyIndex ?? 0);
      }

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region to_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    private string AddRestraints()
    {
      if (record.NodeRestraint == NodeRestraint.Custom)
      {
        var custom = "";
        foreach (var r in record.Restraints)
        {
          custom += r.ToString().ToLowerInvariant();
        }
        return custom;
      }
      else
      {
        return record.NodeRestraint.ToString().ToLowerInvariant();
      }
    }

    private string AddAxis()
    {
      if (record.AxisRefType == NodeAxisRefType.Reference)
      {
        return record.AxisIndex.ToString();
      }
      else if (record.AxisRefType == NodeAxisRefType.NotSet)
      {
        return AxisRefType.Global.ToString().ToUpperInvariant();
      }
      return record.AxisRefType.ToString().ToUpperInvariant();
    }
    #endregion

    #region from_gwa_fns
    private bool AddRestraints(string v)
    {
      var boolRestraints = RestraintBoolArrayFromCode(v);
      if (boolRestraints == null || boolRestraints.Count() < 6)
      {
        return false;
      }
      if (boolRestraints.All(r => r == false))
      {
        record.NodeRestraint = NodeRestraint.Free;
      }
      else if (boolRestraints.Take(3).All(r => r == true) && boolRestraints.Skip(3).Take(3).All(r => r == false))
      {
        record.NodeRestraint = NodeRestraint.Pin;
      }
      else if (boolRestraints.All(r => r == true))
      {
        record.NodeRestraint = NodeRestraint.Fix;
      }
      else
      {
        record.NodeRestraint = NodeRestraint.Custom;
        for (var i = 0; i < 6; i++)
        {
          if (boolRestraints[i])
          {
            if (record.Restraints == null)
            {
              record.Restraints = new List<AxisDirection6>();
            }
            //This list signifies the true/positive values
            record.Restraints.Add(axisDirs[i]);
          }
        }
      }

      return true;
    }

    private bool AddAxis(string v)
    {
      if (v.Trim().Equals("GLOBAL", StringComparison.InvariantCultureIgnoreCase))
      {
        record.AxisRefType = NodeAxisRefType.Global;
        return true;
      }
      if (v.Trim().Equals("X_ELEV", StringComparison.InvariantCultureIgnoreCase))
      {
        record.AxisRefType = NodeAxisRefType.XElevation;
        return true;
      }
      if (v.Trim().Equals("Y_ELEV", StringComparison.InvariantCultureIgnoreCase))
      {
        record.AxisRefType = NodeAxisRefType.YElevation;
        return true;
      }
      if (v.Trim().Equals("VERTICAL", StringComparison.InvariantCultureIgnoreCase))
      {
        record.AxisRefType = NodeAxisRefType.Vertical;
        return true;
      }
      else
      {
        record.AxisRefType = NodeAxisRefType.Reference;
        return AddNullableIndex(v, out record.AxisIndex);
      }
    }

    public bool[] RestraintBoolArrayFromCode(string code)
    {
      if (code == "free")
      {
        return new bool[] { false, false, false, false, false, false };
      }
      else if (code == "pin")
      {
        return new bool[] { true, true, true, false, false, false };
      }
      else if (code == "fix")
      {
        return new bool[] { true, true, true, true, true, true };
      }
      else
      {
        var fixities = new bool[6];

        var codeRemaining = code;
        int prevLength;
        do
        {
          prevLength = codeRemaining.Length;
          if (codeRemaining.Contains("xxx"))
          {
            fixities[0] = true;
            fixities[3] = true;
            codeRemaining = codeRemaining.Replace("xxx", "");
          }
          else if (codeRemaining.Contains("xx"))
          {
            fixities[3] = true;
            codeRemaining = codeRemaining.Replace("xx", "");
          }
          else if (codeRemaining.Contains("x"))
          {
            fixities[0] = true;
            codeRemaining = codeRemaining.Replace("x", "");
          }

          if (codeRemaining.Contains("yyy"))
          {
            fixities[1] = true;
            fixities[4] = true;
            codeRemaining = codeRemaining.Replace("yyy", "");
          }
          else if (codeRemaining.Contains("yy"))
          {
            fixities[4] = true;
            codeRemaining = codeRemaining.Replace("yy", "");
          }
          else if (codeRemaining.Contains("y"))
          {
            fixities[1] = true;
            codeRemaining = codeRemaining.Replace("y", "");
          }

          if (codeRemaining.Contains("zzz"))
          {
            fixities[2] = true;
            fixities[5] = true;
            codeRemaining = codeRemaining.Replace("zzz", "");
          }
          else if (codeRemaining.Contains("zz"))
          {
            fixities[5] = true;
            codeRemaining = codeRemaining.Replace("zz", "");
          }
          else if (codeRemaining.Contains("z"))
          {
            fixities[2] = true;
            codeRemaining = codeRemaining.Replace("z", "");
          }
        } while (codeRemaining.Length > 0 && (codeRemaining.Length < prevLength));

        return fixities;
      }
    }

    #endregion
  }
}
