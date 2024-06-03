using System.Collections.Generic;

namespace Build;

public static class Consts
{
  public static readonly string[] Solutions = { "DUI3-DX.slnf" };

  public static readonly InstallerProject[] InstallerManifests =
  {
    new(
      "arcgis",
      new InstallerAsset[] { new("DUI3-DX/Connectors/ArcGIS/Speckle.Connectors.ArcGIS3", "net6.0-windows") }
    ),
    new("rhino", new InstallerAsset[] { new("DUI3-DX/Connectors/Rhino/Speckle.Connectors.Rhino7", "net48") }),
    new("revit", new InstallerAsset[] { new("DUI3-DX/Connectors/Revit/Speckle.Connectors.Revit2023", "net48") }),
    new("autocad", new InstallerAsset[] { new("DUI3-DX/Connectors/Autocad/Speckle.Connectors.Autocad2023", "net48") })
  };
}

public readonly struct InstallerProject
{
  public string HostAppSlug { get; init; }
  public IReadOnlyList<InstallerAsset> Projects { get; init; }

  public InstallerProject(string hostAppSlug, IReadOnlyList<InstallerAsset> projects)
  {
    HostAppSlug = hostAppSlug;
    Projects = projects;
  }

  public override string ToString() => $"{HostAppSlug}";
}

public readonly struct InstallerAsset
{
  public InstallerAsset(string projectPath, string targetName)
  {
    ProjectPath = projectPath;
    TargetName = targetName;
  }

  public string ProjectPath { get; init; }
  public string TargetName { get; init; }
}
