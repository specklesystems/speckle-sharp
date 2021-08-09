using System;
using System.Collections.Generic;
using System.Linq;


namespace Speckle.GSA.API.GwaSchema
{
  [GsaType(GwaKeyword.LOAD_NODE, GwaSetCommandType.SetAt, true, true, true, GwaKeyword.NODE, GwaKeyword.AXIS)]
  public class GsaLoadNode : GsaRecord
  {
    //As many of these should be nullable, or in the case of enums, a "NotSet" option, to facilitate merging objects received from Speckle 
    //with existing objects in the GSA model
    public string Name { get => name; set { name = value; } }
    public List<int> NodeIndices = new List<int>();
    public int? LoadCaseIndex;
    public bool GlobalAxis;
    public int? AxisIndex;
    public AxisDirection6 LoadDirection;
    public double? Value;

    public GsaLoadNode(): base()
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
      AddItems(ref items, Name, IndicesList(NodeIndices), LoadCaseIndex ?? 0, (GlobalAxis ? "GLOBAL" : (object)AxisIndex), AddLoadDirection(), Value ?? 0);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddLoadDirection()
    {
      return (LoadDirection == AxisDirection6.NotSet) ? "X" : LoadDirection.ToString();
    }
    #endregion

    #region from_gwa_fns
    private bool AddList(string v)
    {
      NodeIndices = Instance.GsaModel.ConvertGSAList(v, GSAEntity.NODE).ToList();
      return (NodeIndices.Count() > 0);
    }

    private bool AddCase(string v)
    {
      LoadCaseIndex = (int.TryParse(v, out var loadCaseIndex) && loadCaseIndex > 0) ? (int?)loadCaseIndex : null;
      return true;
    }

    private bool AddAxis(string v)
    {
      if (v.Equals("GLOBAL", StringComparison.InvariantCultureIgnoreCase))
      {
        GlobalAxis = true;
      }
      else
      {
        AxisIndex = (int.TryParse(v, out var axisIndex) && axisIndex > 0) ? (int?)axisIndex : null;
      }
      return true;
    }

    private bool AddDir(string v)
    {
      return EnumParse(v, out LoadDirection);
    }

    private bool AddValue(string v)
    {
      Value = (double.TryParse(v, out var value) && value != 0) ? (double?)value : null;
      return true;
    }
    #endregion
  }
}
