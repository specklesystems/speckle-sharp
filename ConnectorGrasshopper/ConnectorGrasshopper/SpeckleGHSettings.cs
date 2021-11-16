using System;

namespace ConnectorGrasshopper
{
  public static class SpeckleGHSettings
  {
    private const string SELECTED_KIT_NAME = "Speckle2:kit.default.name";
    private const string USE_SCHEMA_TAG_STRATEGY = "Speckle2:conversion.schema.tag";

    // For future disabling of structural tabs
    private const string SHOW_STRUCTURAL_TABS = "Speckle2:tabs.structural.show";
    private const string SHOW_BUILT_ELEMENT_TABS = "Speckle2:tabs.builtelements.show";

    /// <summary>
    /// Gets or sets the default selected kit name to be used in this Grasshopper instance.
    /// </summary>
    public static string SelectedKitName
    {
      get => Grasshopper.Instances.Settings.GetValue(SELECTED_KIT_NAME, "Objects");
      set
      {
        Grasshopper.Instances.Settings.SetValue(SELECTED_KIT_NAME, value);
        Grasshopper.Instances.Settings.WritePersistentSettings();
      }
    }

    /// <summary>
    /// Gets or sets the output type of the Schema builder nodes:
    /// If true: Output will be the `main geometry` with the schema attached as a property.
    /// If false: Output will be the Schema object with the geometry assigned as it's property.
    /// Defaults to false.
    /// </summary>
    public static bool UseSchemaTag
    {
      get => Grasshopper.Instances.Settings.GetValue(USE_SCHEMA_TAG_STRATEGY, false);
      set
      {
        Grasshopper.Instances.Settings.SetValue(USE_SCHEMA_TAG_STRATEGY, value);
        Grasshopper.Instances.Settings.WritePersistentSettings();
      }
    }
  }
}
