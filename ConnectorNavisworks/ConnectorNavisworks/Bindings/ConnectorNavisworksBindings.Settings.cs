using System;
using System.Collections.Generic;
using Autodesk.Navisworks.Api;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using static Speckle.ConnectorNavisworks.Utils;

namespace Speckle.ConnectorNavisworks.Bindings
{
  public partial class ConnectorBindingsNavisworks
  {
    // CAUTION: these strings need to have the same values as in the converter
    const string InternalOrigin = "Model Origin (default)";
    const string ProxyOrigin = "Project Base Origin";
    const string BBoxOrigin = "Boundingbox Origin";

    // used to store the Stream State settings when sending
    private List<ISetting> CurrentSettings { get; set; }

    public override List<ISetting> GetSettings()
    {
      List<string> referencePoints = new List<string>() { InternalOrigin, ProxyOrigin, BBoxOrigin };
      List<string> units = new List<string>(Enum.GetNames(typeof(Units)));

      return new List<ISetting>
      {
        new ListBoxSetting
        {
          Slug = "reference-point", Name = "Reference Point", Icon = "LocationSearching", Values = referencePoints,
          Selection = InternalOrigin,
          Description = "Sends or receives stream objects in relation to this document point"
        },
        new NumericSetting
        {
          Slug = "x-coordinate", Name = "X Coordinate Project Origin", Icon = "ArrowXAxis",
          Value = 0,
          Increment = 0.001,
          Spinner = false
        },
        new NumericSetting
        {
          Slug = "y-coordinate", Name = "Y Coordinate Project Origin", Icon = "ArrowYAxis",
          Value = 0,
          Increment = 0.001,
          Spinner = false
        },
        new ListBoxSetting
        {
          Slug = "units", Name = "Coordinate Units", Values = units,
          Selection = "Meters",
          Description = "Units for the Project Basepoint coordinates."
        },
        new CheckBoxSetting
        {
          Slug = "current-view", Name = "Include View", IsChecked = false,
          Description = "Include the current display view in the commit."
        }
      };
    }
  }
}