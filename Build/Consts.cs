namespace Build;

public static class Consts
{
  public static readonly string[] Solutions = { "DUI3-DX.slnf" };
  public static readonly (string, string)[] Projects =
  {
    ("DUI3-DX\\Connectors\\ArcGIS\\Speckle.Connectors.ArcGIS3", "net6.0-windows"),
    ("DUI3-DX\\Connectors\\Autocad\\Speckle.Connectors.Autocad2023", "net48"),
    ("DUI3-DX\\Connectors\\Revit\\Speckle.Connectors.Revit2023", "net48"),
    ("DUI3-DX\\Connectors\\Rhino\\Speckle.Connectors.Rhino7", "net48")
  };
}
