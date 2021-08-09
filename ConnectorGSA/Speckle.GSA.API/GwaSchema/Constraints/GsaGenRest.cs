using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;


namespace Speckle.GSA.API.GwaSchema
{
  //polygon references not supported yet
  [GsaType(GwaKeyword.GEN_REST, GwaSetCommandType.SetAt, true, GwaKeyword.ANAL_STAGE, GwaKeyword.NODE)]
  public class GsaGenRest : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public RestraintCondition X;
    public RestraintCondition Y;
    public RestraintCondition Z;
    public RestraintCondition XX;
    public RestraintCondition YY;
    public RestraintCondition ZZ;
    public List<int> Node;
    public List<int> Stage;

    public GsaGenRest() : base()
    {
      //Defaults
      Version = 2;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      //GEN_REST.2 | name | x | y | z | xx | yy | zz | list | stage
      return FromGwaByFuncs(remainingItems, out var _, AddName, AddX, AddY, AddZ, AddXX, AddYY, AddZZ, (v) => AddEntities(v, out Node), AddStage);
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //GEN_REST.2 | name | x | y | z | xx | yy | zz | list | stage
      AddItems(ref items, Name, X.ToDouble(), Y.ToDouble(), Z.ToDouble(), XX.ToDouble(), YY.ToDouble(), ZZ.ToDouble(), AddNode(), AddStage());

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddNode()
    {
      if (Node != null && Node.Count() > 0)
      {
        return string.Join(" ", Node);
      }
      return "";
    }
    private string AddStage()
    {
      if (Stage != null && Stage.Count() > 0)
      {
        return string.Join(" ", Stage);
      }
      return "";
    }
    #endregion

    #region from_gwa_fns
    private bool AddX(string v)
    {
      if (Enum.TryParse<RestraintCondition>(v, true, out var value))
      {
        X = value;
        return true;
      }
      return false;
    }

    private bool AddY(string v)
    {
      if (Enum.TryParse<RestraintCondition>(v, true, out var value))
      {
        Y = value;
        return true;
      }
      return false;
    }

    private bool AddZ(string v)
    {
      if (Enum.TryParse<RestraintCondition>(v, true, out var value))
      {
        Z = value;
        return true;
      }
      return false;
    }

    private bool AddXX(string v)
    {
      if (Enum.TryParse<RestraintCondition>(v, true, out var value))
      {
        XX = value;
        return true;
      }
      return false;
    }

    private bool AddYY(string v)
    {
      if (Enum.TryParse<RestraintCondition>(v, true, out var value))
      {
        YY = value;
        return true;
      }
      return false;
    }

    private bool AddZZ(string v)
    {
      if (Enum.TryParse<RestraintCondition>(v, true, out var value))
      {
        ZZ = value;
        return true;
      }
      return false;
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
    #endregion
  }
}
