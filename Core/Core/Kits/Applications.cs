using System.Diagnostics.Contracts;

namespace Speckle.Core.Kits;

public enum HostAppVersion
{
  v,
  v6,
  v7,
  v8,
  v2019,
  v2020,
  v2021,
  v2022,
  v2023,
  v2024,
  v2025,
  vSandbox,
  vRevit,
  vRevit2021,
  vRevit2022,
  vRevit2023,
  vRevit2024,
  vRevit2025,
  v25,
  v26,
  v715,
  v716,
  v717
}

public readonly struct HostApplication
{
  public string Name { get; }
  public string Slug { get; }

  public HostApplication(string name, string slug)
  {
    Name = name;
    Slug = slug;
  }

  /// <summary>
  /// Returns the versioned app name given a specific version
  /// </summary>
  /// <param name="version"></param>
  /// <returns></returns>
  public string GetVersion(HostAppVersion version)
  {
    return Name.Replace(" ", "") + version.ToString().TrimStart('v');
  }
}

/// <summary>
/// List of Host Applications - their slugs should match our ghost tags and ci/cd slugs
/// </summary>
public static class HostApplications
{
  public static readonly HostApplication Rhino = new("Rhino", "rhino"),
    Grasshopper = new("Grasshopper", "grasshopper"),
    Revit = new("Revit", "revit"),
    Dynamo = new("Dynamo", "dynamo"),
    Unity = new("Unity", "unity"),
    GSA = new("GSA", "gsa"),
    Civil = new("Civil 3D", "civil3d"),
    AutoCAD = new("AutoCAD", "autocad"),
    MicroStation = new("MicroStation", "microstation"),
    OpenRoads = new("OpenRoads", "openroads"),
    OpenRail = new("OpenRail", "openrail"),
    OpenBuildings = new("OpenBuildings", "openbuildings"),
    ETABS = new("ETABS", "etabs"),
    SAP2000 = new("SAP2000", "sap2000"),
    CSiBridge = new("CSiBridge", "csibridge"),
    SAFE = new("SAFE", "safe"),
    TeklaStructures = new("Tekla Structures", "teklastructures"),
    Dxf = new("DXF Converter", "dxf"),
    Excel = new("Excel", "excel"),
    Unreal = new("Unreal", "unreal"),
    PowerBI = new("Power BI", "powerbi"),
    Blender = new("Blender", "blender"),
    QGIS = new("QGIS", "qgis"),
    ArcGIS = new("ArcGIS", "arcgis"),
    SketchUp = new("SketchUp", "sketchup"),
    Archicad = new("Archicad", "archicad"),
    TopSolid = new("TopSolid", "topsolid"),
    Python = new("Python", "python"),
    NET = new(".NET", "net"),
    Navisworks = new("Navisworks", "navisworks"),
    AdvanceSteel = new("Advance Steel", "advancesteel"),
    Other = new("Other", "other");

  /// <summary>
  /// Gets a HostApplication form a string. It could be the versioned name or a string coming from a process running.
  /// </summary>
  /// <param name="appname">String with the name of the app</param>
  /// <returns></returns>
  [Pure]
  public static HostApplication GetHostAppFromString(string? appname)
  {
    if (appname == null)
    {
      return Other;
    }

    appname = appname.ToLowerInvariant().Replace(" ", "");
    if (appname.Contains("dynamo"))
    {
      return Dynamo;
    }

    if (appname.Contains("revit"))
    {
      return Revit;
    }

    if (appname.Contains("autocad"))
    {
      return AutoCAD;
    }

    if (appname.Contains("civil"))
    {
      return Civil;
    }

    if (appname.Contains("rhino"))
    {
      return Rhino;
    }

    if (appname.Contains("grasshopper"))
    {
      return Grasshopper;
    }

    if (appname.Contains("unity"))
    {
      return Unity;
    }

    if (appname.Contains("gsa"))
    {
      return GSA;
    }

    if (appname.Contains("microstation"))
    {
      return MicroStation;
    }

    if (appname.Contains("openroads"))
    {
      return OpenRoads;
    }

    if (appname.Contains("openrail"))
    {
      return OpenRail;
    }

    if (appname.Contains("openbuildings"))
    {
      return OpenBuildings;
    }

    if (appname.Contains("etabs"))
    {
      return ETABS;
    }

    if (appname.Contains("sap"))
    {
      return SAP2000;
    }

    if (appname.Contains("csibridge"))
    {
      return CSiBridge;
    }

    if (appname.Contains("safe"))
    {
      return SAFE;
    }

    if (appname.Contains("teklastructures"))
    {
      return TeklaStructures;
    }

    if (appname.Contains("dxf"))
    {
      return Dxf;
    }

    if (appname.Contains("excel"))
    {
      return Excel;
    }

    if (appname.Contains("unreal"))
    {
      return Unreal;
    }

    if (appname.Contains("powerbi"))
    {
      return PowerBI;
    }

    if (appname.Contains("blender"))
    {
      return Blender;
    }

    if (appname.Contains("qgis"))
    {
      return QGIS;
    }

    if (appname.Contains("arcgis"))
    {
      return ArcGIS;
    }

    if (appname.Contains("sketchup"))
    {
      return SketchUp;
    }

    if (appname.Contains("archicad"))
    {
      return Archicad;
    }

    if (appname.Contains("topsolid"))
    {
      return TopSolid;
    }

    if (appname.Contains("python"))
    {
      return Python;
    }

    if (appname.Contains("net"))
    {
      return NET;
    }

    if (appname.Contains("navisworks"))
    {
      return Navisworks;
    }

    if (appname.Contains("advancesteel"))
    {
      return AdvanceSteel;
    }

    return new HostApplication(appname, appname);
  }
}
