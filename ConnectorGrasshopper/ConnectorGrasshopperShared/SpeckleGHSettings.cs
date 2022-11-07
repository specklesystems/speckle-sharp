﻿using System;
using System.Reflection;
using Grasshopper.GUI;
using Grasshopper.GUI.Ribbon;

namespace ConnectorGrasshopper
{
  public static class SpeckleGHSettings
  {
    public const string SELECTED_KIT_NAME = "Speckle2:kit.default.name";
    public const string MESH_SETTINGS = "Speckle2:kit.meshing.settings";
    public const string USE_SCHEMA_TAG_STRATEGY = "Speckle2:conversion.schema.tag";
    public const string SHOW_DEV_COMPONENTS = "Speckle2:dev.components.show";
    public const string HEADLESS_TEMPLATE_FILENAME = "Speckle2:headless.template.name";
    
    public static event EventHandler<SettingsChangedEventArgs> SettingsChanged;
    public static event EventHandler OnMeshSettingsChanged;

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
        OnSettingsChanged(SELECTED_KIT_NAME);
      }
    }
    
    public static SpeckleMeshSettings MeshSettings
    {
      get => (SpeckleMeshSettings)Grasshopper.Instances.Settings.GetValue(MESH_SETTINGS, (int)SpeckleMeshSettings.Default);
      set
      {
        Grasshopper.Instances.Settings.SetValue(MESH_SETTINGS, (int)value);
        Grasshopper.Instances.Settings.WritePersistentSettings();
        OnMeshSettingsChanged?.Invoke(null, EventArgs.Empty);
        OnSettingsChanged(MESH_SETTINGS);
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
        OnSettingsChanged(USE_SCHEMA_TAG_STRATEGY);
      }
    }
    
    
    public static bool ShowDevComponents
    {
      get => Grasshopper.Instances.Settings.GetValue(SHOW_DEV_COMPONENTS, false);
      set
      {
        Grasshopper.Instances.Settings.SetValue(SHOW_DEV_COMPONENTS, value);
        Grasshopper.Instances.Settings.WritePersistentSettings();
        OnSettingsChanged(SHOW_DEV_COMPONENTS);
        PopulateRibbon();
      }
    }
    
    public static string HeadlessTemplateFilename
    {
      get => Grasshopper.Instances.Settings.GetValue(HEADLESS_TEMPLATE_FILENAME, "headless.3dm");
      set
      {
        Grasshopper.Instances.Settings.SetValue(HEADLESS_TEMPLATE_FILENAME, value);
        Grasshopper.Instances.Settings.WritePersistentSettings();
        OnSettingsChanged(HEADLESS_TEMPLATE_FILENAME);
      }
    }
    
    public static bool GetTabVisibility(string name)
    {
      var tabVisibility = Grasshopper.Instances.Settings.GetValue($"Speckle2:tabs.{name}", false);
      return tabVisibility;
    }

    public static void SetTabVisibility(string name, bool value)
    {
      var key = $"Speckle2:tabs.{name}";
      Grasshopper.Instances.Settings.SetValue(key, value);
      Grasshopper.Instances.Settings.WritePersistentSettings();
      OnSettingsChanged(key);
      PopulateRibbon();
    }

    private static void OnSettingsChanged(string key)
    {
      SettingsChanged?.Invoke(null, new SettingsChangedEventArgs(key));
    }

    private static void PopulateRibbon()
    {
      var ribbon = typeof(GH_DocumentEditor)
        .GetProperty("Ribbon", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?.GetValue(Grasshopper.Instances.DocumentEditor) as GH_Ribbon;
      ribbon?.PopulateRibbon();
    }
  }
  
  public class SettingsChangedEventArgs : EventArgs
  {
    public string Key;

    public SettingsChangedEventArgs(string key)
    {
      Key = key;
    }
  }

  public enum SpeckleMeshSettings
  {
    Default,
    CurrentDoc
  }
}
