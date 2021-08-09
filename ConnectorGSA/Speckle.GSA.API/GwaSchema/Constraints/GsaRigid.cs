using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;


namespace Speckle.GSA.API.GwaSchema
{
  //polygon references not supported yet
  [GsaType(GwaKeyword.RIGID, GwaSetCommandType.SetAt, true, GwaKeyword.ANAL_STAGE, GwaKeyword.NODE, GwaKeyword.MEMB)]
  public class GsaRigid : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public int? PrimaryNode;
    public RigidConstraintType Type;
    public Dictionary<AxisDirection6,List<AxisDirection6>> Link;
    public List<int> ConstrainedNodes;
    public List<int> Stage;
    public int? ParentMember;

    public GsaRigid() : base()
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

      //RIGID.3 | name | primary_node | type | constrained_nodes | stage | parent_member
      return FromGwaByFuncs(remainingItems, out var _, AddName, AddPrimaryNode, AddType, (v) => AddNodes(v, out ConstrainedNodes), AddStage, AddParentMember);
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //RIGID.3 | name | primary_node | type | constrained_nodes | stage | parent_member
      AddItems(ref items, Name, PrimaryNode, AddType(), AddConstrainedNodes(), AddStage(), ParentMember);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddType()
    {
      if (Type.Equals(RigidConstraintType.NotSet))
      {
        return "";
      }
      else if (Type.Equals(RigidConstraintType.Custom))
      {
        string v = "";
        foreach (var key in Link.Keys)
        {
          v += key.ToString() + ":" + string.Join("",Link[key].ConvertAll(f => f.ToString())) + "-";
        }
        return (v.Length > 0) ? v.Remove(v.Length - 1) : v;
      }
      else
      {
        return Type.ToString();
      }
    }
    private string AddConstrainedNodes()
    {
      if (ConstrainedNodes != null && ConstrainedNodes.Count() > 0)
      {
        return string.Join(" ", ConstrainedNodes);
      }
      else
      {
        return "";
      }
    }
    private string AddStage()
    {
      if (Stage != null && Stage.Count() > 0)
      {
        return string.Join(" ", Stage);
      }
      else
      {
        return "";
      }
    }
    #endregion

    #region from_gwa_fns
    private bool AddPrimaryNode(string v)
    {
      PrimaryNode = (int.TryParse(v, out var pn) && pn > 0) ? (int?)pn : null;
      return true;
    }

    private bool AddType(string v)
    {
      if (Enum.TryParse<RigidConstraintType>(v, true, out var t))
      {
        Type = t;
      }
      else if (IsLink(v))
      {
        /* Convert explicit definition of the link type into a Dictionary
         * 
         * Custom links are written in the form constrained_node:primary_node-consatrained_node:primary_node 
         * e.g. X:XYY-Y:YXX give a linkage so that the constrained node x displacement depends on the 
         * primary node x and yy displacements and the constrained node y displacement depends on the 
         * primary node y and xx displacements
         */
        Type = RigidConstraintType.Custom;
        Link = new Dictionary<AxisDirection6, List<AxisDirection6>>();
        var constraints = v.Split('-');
        foreach (var constraint in constraints)
        {
          var c = constraint.Split(':');

          //Link key
          Enum.TryParse<AxisDirection6>(c[0], out var cKey);

          //Link value
          var cValue = new List<AxisDirection6>();
          var constrainedDirections = SplitStringByRepeatedCharacters(c[1]);
          foreach (var cDir in constrainedDirections)
          {
            Enum.TryParse<AxisDirection6>(cDir, out var d);
            cValue.Add(d);
          }
          Link.Add(cKey, cValue);
        }
      }
      else
      {
        Type = RigidConstraintType.NotSet;
        Link = null;
      }
      return true;
    }

    private bool AddStage(string v)
    {
      var entityItems = v.Split(' ');
      if (entityItems.Count() == 1 && entityItems.First().Equals("all", StringComparison.InvariantCultureIgnoreCase))
      {
        Stage = Instance.GsaModel.LookupIndices(GetKeyword<GsaAnalStage>()).ToList();
      }
      else
      {
        Stage = new List<int>();
        foreach (var item in entityItems)
        {
          if (int.TryParse(item, out var s) && s > 0)
          {
            Stage.Add(s);
          }
        }
      }
      return true;
    }

    private bool AddParentMember(string v)
    {
      ParentMember = (int.TryParse(v, out var pm) && pm >= 0) ? (int?)pm : null;
      return true;
    }
    #endregion

    #region helper
    private bool IsLink(string v)
    {
      string allowableLetters = "XYZ:-";

      foreach (char c in v)
      {
        if (!allowableLetters.Contains(c.ToString()))
          return false;
      }
      return true;
    }

    private List<string> SplitStringByRepeatedCharacters(string v)
    {
      var result = new List<string>();
      if (!string.IsNullOrWhiteSpace(v))
      {
        result.Add(v[0].ToString());
        for (int i = 1; i < v.Length; i++)
        {
          var thisChar = v[i];
          var prevChar = v[i - 1];
          if (!thisChar.Equals(prevChar))
          {
            result.Add("");
          }
          result[result.Count-1] += thisChar;
        }
      }
      return result;
    }
    #endregion
  }
}
