using Speckle.GSA.API.GwaSchema;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.USER_VEHICLE, GwaSetCommandType.Set, true)]
  public class GsaUserVehicleParser : GwaParser<GsaUserVehicle>
  {
    public GsaUserVehicleParser(GsaUserVehicle gsaUserVehicle) : base(gsaUserVehicle) { }

    public GsaUserVehicleParser() : base(new GsaUserVehicle()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      var items = remainingItems;
      //USER_VEHICLE | no | name | width | num_axle | { axle | offset | load_left | load_right }
      if(!FromGwaByFuncs(remainingItems, out remainingItems, AddName, AddWidth, AddNumAxle))
      {
        return false;
      }

      //Loop through remaining items and add items to AxlePosition, AxleOffset, AxleLeft, AxleRight lists
      record.AxlePosition = new List<double>(); 
      record.AxleOffset = new List<double>();
      record.AxleLeft = new List<double>();
      record.AxleRight = new List<double>();
      for (var i = 0; i < record.NumAxle; i++)
      {
        items = remainingItems;
        if (!FromGwaByFuncs(items, out remainingItems, AddAxlePosition, AddAxleOffset, AddAxleLeft, AddAxleRight))
        {
          return false;
        }
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

      //USER_VEHICLE | no | name | width | num_axle | { axle | offset | load_left | load_right }
      AddItems(ref items, record.Name, record.Width, record.NumAxle);

      for (var i = 0; i < record.NumAxle; i++)
      {
        items.Add(record.AxlePosition[i].ToString());
        items.Add(record.AxleOffset[i].ToString());
        items.Add(record.AxleLeft[i].ToString());
        items.Add(record.AxleRight[i].ToString());
      }

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    #endregion

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    private bool AddWidth(string v)
    {
      record.Width = (double.TryParse(v, out var value) && value != 0) ? (double?)value : null;
      return true;
    }
    private bool AddNumAxle(string v)
    {
      record.NumAxle = (int.TryParse(v, out var value) && value > 0) ? (int?)value : null;
      return true;
    }

    private bool AddAxlePosition(string v)
    {
      if (double.TryParse(v, out var value) && value >= 0)
      {
        record.AxlePosition.Add(value);
        return true;
      }
      return false;
    }

    private bool AddAxleOffset(string v)
    {
      if (double.TryParse(v, out var value) && value >= 0)
      {
        record.AxleOffset.Add(value);
        return true;
      }
      return false;
    }

    private bool AddAxleLeft(string v)
    {
      if (double.TryParse(v, out var value) && value >= 0)
      {
        record.AxleLeft.Add(value);
        return true;
      }
      return false;
    }

    private bool AddAxleRight(string v)
    {
      if (double.TryParse(v, out var value) && value >= 0)
      {
        record.AxleRight.Add(value);
        return true;
      }
      return false;
    }
    #endregion
  }
}
