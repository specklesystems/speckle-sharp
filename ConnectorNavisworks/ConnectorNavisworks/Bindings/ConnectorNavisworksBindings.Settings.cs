using System;
using System.Collections.Generic;
using Autodesk.Navisworks.Api;
using DesktopUI2.Models.Settings;

namespace Speckle.ConnectorNavisworks.Bindings;

public partial class ConnectorBindingsNavisworks
{
  // CAUTION: these strings need to have the same values as in the converter
  private const string InternalOrigin = "Model Origin (default)";
  private const string ProxyOrigin = "Project Base Origin";
  private const string BBoxOrigin = "Boundingbox Origin";

  // used to store the Stream State settings when sending
  private List<ISetting> CurrentSettings { get; set; }

  public override List<ISetting> GetSettings()
  {
    var referencePoints = new List<string> { InternalOrigin, ProxyOrigin, BBoxOrigin };
    var units = new List<string>(Enum.GetNames(typeof(Units)));

    return new List<ISetting>
    {
      new ListBoxSetting
      {
        Slug = "reference-point",
        Name = "Reference Point",
        Icon = "LocationSearching",
        Values = referencePoints,
        Selection = InternalOrigin,
        Description = "Sends or receives stream objects in relation to this document point"
      },
      new NumericSetting
      {
        Slug = "x-coordinate",
        Name = "X Coordinate Project Origin",
        Icon = "ArrowXAxis",
        Value = 0,
        Increment = 0.001,
        Spinner = false
      },
      new NumericSetting
      {
        Slug = "y-coordinate",
        Name = "Y Coordinate Project Origin",
        Icon = "ArrowYAxis",
        Value = 0,
        Increment = 0.001,
        Spinner = false
      },
      new ListBoxSetting
      {
        Slug = "units",
        Name = "Coordinate Units",
        Values = units,
        Selection = "Meters",
        Description = "Units for the Project Basepoint coordinates."
      },
      new CheckBoxSetting
      {
        Slug = "current-view",
        Name = "Include View",
        IsChecked = true,
        Description = "Include the current display view in the commit."
      },
      new CheckBoxSetting
      {
        Slug = "full-tree",
        Name = "Include Full Hierarchy",
        IsChecked = false,
        Description = "Include the full root to leaf selection hierarchy of nodes in the commit."
      },
      new CheckBoxSetting
      {
        Slug = "internal-properties",
        Name = "Expose Internal Properties",
        IsChecked = false,
        Description =
          "Include the internal properties that reflect option types. Can be useful for downstream data analysis."
      },
      new CheckBoxSetting
      {
        Slug = "internal-property-names",
        Name = "Internal Property Names",
        IsChecked = false,
        Description =
          "Commit properties with the Navisworks internal names. Can be useful for downstream data analysis removing internationalization."
      },
      new CheckBoxSetting
      {
        Slug = "coalesce-data",
        Name = "Coalesce Data from First Object to Geometry",
        IsChecked = false,
        Description =
          "All properties from the Geometry up the tree to the next First Object will be coalesced into the Geometry Node."
      }
    };
  }
}
