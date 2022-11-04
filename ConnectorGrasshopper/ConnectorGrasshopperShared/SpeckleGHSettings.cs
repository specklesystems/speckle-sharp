using System;

namespace ConnectorGrasshopper
{
  public static class SpeckleGHSettings
  {
    private const string SELECTED_KIT_NAME = "Speckle2:kit.default.name";
    private const string MESH_SETTINGS = "Speckle2:kit.meshing.settings";
    private const string USE_SCHEMA_TAG_STRATEGY = "Speckle2:conversion.schema.tag";
    private const string SHOW_DEV_COMPONENTS = "Speckle2:dev.components.show";
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
    public static event EventHandler OnMeshSettingsChanged;
    public static SpeckleMeshSettings MeshSettings
    {
      get => (SpeckleMeshSettings)Grasshopper.Instances.Settings.GetValue(MESH_SETTINGS, (int)SpeckleMeshSettings.Default);
      set
      {
        Grasshopper.Instances.Settings.SetValue(MESH_SETTINGS, (int)value);
        Grasshopper.Instances.Settings.WritePersistentSettings();
        OnMeshSettingsChanged?.Invoke(null, EventArgs.Empty);
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

    public static bool ShowDevComponents
    {
      get => Grasshopper.Instances.Settings.GetValue(SHOW_DEV_COMPONENTS, false);
      set
      {
        Grasshopper.Instances.Settings.SetValue(SHOW_DEV_COMPONENTS, value);
        Grasshopper.Instances.Settings.WritePersistentSettings();
      }
    }
    
    public static bool GetTabVisibility(string name)
    {
      var tabVisibility = Grasshopper.Instances.Settings.GetValue($"Speckle2:tabs.{name}", false);
      return tabVisibility;
    }

    public static void SetTabVisibility(string name, bool value)
    {
      Grasshopper.Instances.Settings.SetValue($"Speckle2:tabs.{name}", value);
      Grasshopper.Instances.Settings.WritePersistentSettings();
    }
  }

  public enum SpeckleMeshSettings
  {
    Default,
    CurrentDoc
  }
}
