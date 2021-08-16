using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  //polygon references not supported yet
  [GsaType(GwaKeyword.GEN_REST, GwaSetCommandType.SetAt, true, GwaKeyword.ANAL_STAGE, GwaKeyword.NODE)]
  public class GsaGenRestParser : GwaParser<GsaGenRest>
  {
    public GsaGenRestParser(GsaGenRest gsaGenRest) : base(gsaGenRest) { }

    public GsaGenRestParser() : base(new GsaGenRest()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      //GEN_REST.2 | name | x | y | z | xx | yy | zz | list | stage
      return FromGwaByFuncs(remainingItems, out var _, AddName, AddX, AddY, AddZ, AddXX, AddYY, AddZZ, (v) => AddEntities(v, out record.Node), AddStage);
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //GEN_REST.2 | name | x | y | z | xx | yy | zz | list | stage
      AddItems(ref items, record.Name, record.X.ToDouble(), record.Y.ToDouble(), record.Z.ToDouble(), record.XX.ToDouble(), record.YY.ToDouble(), record.ZZ.ToDouble(), 
        AddNode(), AddStage());

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddNode()
    {
      if (record.Node != null && record.Node.Count() > 0)
      {
        return string.Join(" ", record.Node);
      }
      return "";
    }
    private string AddStage()
    {
      if (record.Stage != null && record.Stage.Count() > 0)
      {
        return string.Join(" ", record.Stage);
      }
      return "";
    }
    #endregion

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    private bool AddX(string v)
    {
      if (Enum.TryParse<RestraintCondition>(v, true, out var value))
      {
        record.X = value;
        return true;
      }
      return false;
    }

    private bool AddY(string v)
    {
      if (Enum.TryParse<RestraintCondition>(v, true, out var value))
      {
        record.Y = value;
        return true;
      }
      return false;
    }

    private bool AddZ(string v)
    {
      if (Enum.TryParse<RestraintCondition>(v, true, out var value))
      {
        record.Z = value;
        return true;
      }
      return false;
    }

    private bool AddXX(string v)
    {
      if (Enum.TryParse<RestraintCondition>(v, true, out var value))
      {
        record.XX = value;
        return true;
      }
      return false;
    }

    private bool AddYY(string v)
    {
      if (Enum.TryParse<RestraintCondition>(v, true, out var value))
      {
        record.YY = value;
        return true;
      }
      return false;
    }

    private bool AddZZ(string v)
    {
      if (Enum.TryParse<RestraintCondition>(v, true, out var value))
      {
        record.ZZ = value;
        return true;
      }
      return false;
    }

    private bool AddStage(string v)
    {
      var entityItems = v.Split(' ');
      if (entityItems.Count() == 1 && entityItems.First().Equals("all", StringComparison.InvariantCultureIgnoreCase))
      {
        record.Stage = Instance.GsaModel.LookupIndices(GwaKeyword.ANAL_STAGE).ToList();
      }
      else
      {
        record.Stage = new List<int>();
        foreach (var item in entityItems)
        {
          if (int.TryParse(item, out var s) && s > 0)
          {
            record.Stage.Add(s);
          }
        }
      }
      return true;
    }
    #endregion
  }
}
