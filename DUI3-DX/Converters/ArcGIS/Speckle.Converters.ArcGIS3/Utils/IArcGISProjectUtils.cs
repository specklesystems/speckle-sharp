namespace Speckle.Converters.ArcGIS3.Utils;

public interface IArcGISProjectUtils
{
  string GetDatabasePath();
  string AddDatabaseToProject(string databasePath);
}
