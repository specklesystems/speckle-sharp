namespace Build;

public static class Consts
{
  public static readonly string[] Solutions = { "DUI3-DX.slnf" };
  public static readonly string[] Frameworks = { "net48" };
  public static readonly (string, string)[] Projects =
  {
    ("DUI3-DX\\Connectors\\Revit\\Speckle.Connectors.Revit2023", "net48"),
    ("DUI3-DX\\Connectors\\Revit\\Speckle.Connectors.ArcGIS3", "net48")
  };

  public static readonly string Root = "C:\\Users\\adam\\Git\\speckle-sharp";
}
