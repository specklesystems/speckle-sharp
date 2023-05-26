namespace Speckle.Core.Kits;

public enum HostAppVersion
{
  v,
  v6,
  v7,
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

public class HostApplication
{
  public HostApplication(string name, string slug)
  {
    Name = name;
    Slug = slug;
  }

  public string Name { get; private set; }
  public string Slug { get; private set; }

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
  public static HostApplication Rhino = new("Rhino", "rhino");
  public static HostApplication Grasshopper = new("Grasshopper", "grasshopper");
  public static HostApplication Revit = new("Revit", "revit");
  public static HostApplication Dynamo = new("Dynamo", "dynamo");
  public static HostApplication Unity = new("Unity", "unity");
  public static HostApplication GSA = new("GSA", "gsa");
  public static HostApplication Civil = new("Civil 3D", "civil3d");
  public static HostApplication AutoCAD = new("AutoCAD", "autocad");
  public static HostApplication MicroStation = new("MicroStation", "microstation");
  public static HostApplication OpenRoads = new("OpenRoads", "openroads");
  public static HostApplication OpenRail = new("OpenRail", "openrail");
  public static HostApplication OpenBuildings = new("OpenBuildings", "openbuildings");
  public static HostApplication ETABS = new("ETABS", "etabs");
  public static HostApplication SAP2000 = new("SAP2000", "sap2000");
  public static HostApplication CSiBridge = new("CSiBridge", "csibridge");
  public static HostApplication SAFE = new("SAFE", "safe");
  public static HostApplication TeklaStructures = new("Tekla Structures", "teklastructures");
  public static HostApplication Dxf = new("DXF Converter", "dxf");
  public static HostApplication Excel = new("Excel", "excel");
  public static HostApplication Unreal = new("Unreal", "unreal");
  public static HostApplication PowerBI = new("Power BI", "powerbi");
  public static HostApplication Blender = new("Blender", "blender");
  public static HostApplication QGIS = new("QGIS", "qgis");
  public static HostApplication ArcGIS = new("ArcGIS", "arcgis");
  public static HostApplication SketchUp = new("SketchUp", "sketchup");
  public static HostApplication Archicad = new("Archicad", "archicad");
  public static HostApplication TopSolid = new("TopSolid", "topsolid");
  public static HostApplication Python = new("Python", "python");
  public static HostApplication NET = new(".NET", "net");
  public static HostApplication Navisworks = new("Navisworks", "navisworks");
  public static HostApplication AdvanceSteel = new("Advance Steel", "advancesteel");
  public static HostApplication Other = new("Other", "other");

  /// <summary>
  /// Gets a HostApplication form a string. It could be the versioned name or a string coming from a process running.
  /// </summary>
  /// <param name="appname">String with the name of the app</param>
  /// <returns></returns>
  public static HostApplication GetHostAppFromString(string appname)
  {
    if (appname == null)
      return Other;
    appname = appname.ToLowerInvariant().Replace(" ", "");
    if (appname.Contains("dynamo"))
      return Dynamo;
    if (appname.Contains("revit"))
      return Revit;
    if (appname.Contains("autocad"))
      return AutoCAD;
    if (appname.Contains("civil"))
      return Civil;
    if (appname.Contains("rhino"))
      return Rhino;
    if (appname.Contains("grasshopper"))
      return Grasshopper;
    if (appname.Contains("unity"))
      return Unity;
    if (appname.Contains("gsa"))
      return GSA;
    if (appname.Contains("microstation"))
      return MicroStation;
    if (appname.Contains("openroads"))
      return OpenRoads;
    if (appname.Contains("openrail"))
      return OpenRail;
    if (appname.Contains("openbuildings"))
      return OpenBuildings;
    if (appname.Contains("etabs"))
      return ETABS;
    if (appname.Contains("sap"))
      return SAP2000;
    if (appname.Contains("csibridge"))
      return CSiBridge;
    if (appname.Contains("safe"))
      return SAFE;
    if (appname.Contains("teklastructures"))
      return TeklaStructures;
    if (appname.Contains("dxf"))
      return Dxf;
    if (appname.Contains("excel"))
      return Excel;
    if (appname.Contains("unreal"))
      return Unreal;
    if (appname.Contains("powerbi"))
      return PowerBI;
    if (appname.Contains("blender"))
      return Blender;
    if (appname.Contains("qgis"))
      return QGIS;
    if (appname.Contains("arcgis"))
      return ArcGIS;
    if (appname.Contains("sketchup"))
      return SketchUp;
    if (appname.Contains("archicad"))
      return Archicad;
    if (appname.Contains("topsolid"))
      return TopSolid;
    if (appname.Contains("python"))
      return Python;
    if (appname.Contains("net"))
      return NET;
    if (appname.Contains("navisworks"))
      return Navisworks;
    if (appname.Contains("advancesteel"))
      return AdvanceSteel;
    return new HostApplication(appname, appname);
  }
}
