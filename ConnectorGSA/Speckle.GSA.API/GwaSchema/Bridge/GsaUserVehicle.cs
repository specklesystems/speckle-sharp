using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;


namespace Speckle.GSA.API.GwaSchema.Bridge
{
  [GsaType(GwaKeyword.USER_VEHICLE, GwaSetCommandType.Set, true, GwaKeyword.LOAD_TITLE, GwaKeyword.TASK)]
  public class GsaUserVehicle : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public double? Width;
    public int? NumAxle;
    public List<double> AxlePosition;
    public List<double> AxleOffset;
    public List<double> AxleLeft;
    public List<double> AxleRight;

    public GsaUserVehicle() : base()
    {
      //Defaults
      Version = 1;
    }

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
      AxlePosition = new List<double>();
      AxleOffset = new List<double>();
      AxleLeft = new List<double>();
      AxleRight = new List<double>();
      for (var i = 0; i < NumAxle; i++)
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
      AddItems(ref items, Name, Width, NumAxle);

      for (var i = 0; i < NumAxle; i++)
      {
        items.Add(AxlePosition[i].ToString());
        items.Add(AxleOffset[i].ToString());
        items.Add(AxleLeft[i].ToString());
        items.Add(AxleRight[i].ToString());
      }

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    #endregion

    #region from_gwa_fns
    private bool AddWidth(string v)
    {
      Width = (double.TryParse(v, out var value) && value != 0) ? (double?)value : null;
      return true;
    }
    private bool AddNumAxle(string v)
    {
      NumAxle = (int.TryParse(v, out var value) && value > 0) ? (int?)value : null;
      return true;
    }

    private bool AddAxlePosition(string v)
    {
      if (double.TryParse(v, out var value) && value >= 0)
      {
        AxlePosition.Add(value);
        return true;
      }
      return false;
    }

    private bool AddAxleOffset(string v)
    {
      if (double.TryParse(v, out var value) && value >= 0)
      {
        AxleOffset.Add(value);
        return true;
      }
      return false;
    }

    private bool AddAxleLeft(string v)
    {
      if (double.TryParse(v, out var value) && value >= 0)
      {
        AxleLeft.Add(value);
        return true;
      }
      return false;
    }

    private bool AddAxleRight(string v)
    {
      if (double.TryParse(v, out var value) && value >= 0)
      {
        AxleRight.Add(value);
        return true;
      }
      return false;
    }
    #endregion
  }
}
