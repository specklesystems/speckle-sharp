using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.LOAD_NODE, GwaSetCommandType.SetAt, true, true, true, GwaKeyword.NODE, GwaKeyword.AXIS)]
  public class GsaLoadNodeParser : GwaParser<GsaLoadNode>
  {
    public GsaLoadNodeParser(GsaLoadNode gsaLoadNode) : base(gsaLoadNode) { }

    public GsaLoadNodeParser() : base(new GsaLoadNode()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      //LOAD_NODE.2 | name | list | case | axis | dir | value
      return FromGwaByFuncs(remainingItems, out var _, AddName, AddList, AddCase, AddAxis, AddDir, AddValue);
    }

    //Doesn't take version into account yet
    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //LOAD_NODE.2 | name | list | case | axis | dir | value
      AddItems(ref items, record.Name, IndicesList(record.NodeIndices), record.LoadCaseIndex ?? 0, (record.GlobalAxis ? "GLOBAL" : (object)record.AxisIndex), 
        AddLoadDirection(), record.Value ?? 0);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddLoadDirection()
    {
      return (record.LoadDirection == AxisDirection6.NotSet) ? "X" : record.LoadDirection.ToString();
    }
    #endregion

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    private bool AddList(string v)
    {
      record.NodeIndices = Instance.GsaModel.Proxy.ConvertGSAList(v, GSAEntity.NODE).ToList();
      return (record.NodeIndices.Count() > 0);
    }

    private bool AddCase(string v)
    {
      record.LoadCaseIndex = (int.TryParse(v, out var loadCaseIndex) && loadCaseIndex > 0) ? (int?)loadCaseIndex : null;
      return true;
    }

    private bool AddAxis(string v)
    {
      if (v.Equals("GLOBAL", StringComparison.InvariantCultureIgnoreCase))
      {
        record.GlobalAxis = true;
      }
      else
      {
        record.AxisIndex = (int.TryParse(v, out var axisIndex) && axisIndex > 0) ? (int?)axisIndex : null;
      }
      return true;
    }

    private bool AddDir(string v)
    {
      return EnumParse(v, out record.LoadDirection);
    }

    private bool AddValue(string v)
    {
      record.Value = (double.TryParse(v, out var value) && value != 0) ? (double?)value : null;
      return true;
    }
    #endregion
  }
}
