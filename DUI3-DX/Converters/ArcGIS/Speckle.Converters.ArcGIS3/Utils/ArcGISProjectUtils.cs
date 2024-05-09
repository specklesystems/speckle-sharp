using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace Speckle.Converters.ArcGIS3.Utils;

public class ArcGISProjectUtils : IArcGISProjectUtils
{
  private const string FGDB_NAME = "Speckle.gdb";

  public string GetDatabasePath()
  {
    string fGdbPath;
    try
    {
      var parentDirectory = Directory.GetParent(Project.Current.URI);
      if (parentDirectory == null)
      {
        throw new ArgumentException($"Project directory {Project.Current.URI} not found");
      }
      fGdbPath = parentDirectory.ToString();
    }
    catch (Exception ex)
      when (ex
          is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or NotSupportedException
            or System.Security.SecurityException
      )
    {
      throw;
    }

    return $"{fGdbPath}\\{FGDB_NAME}";
  }

  public string AddDatabaseToProject(string databasePath)
  {
    // Create a FileGeodatabaseConnectionPath with the name of the file geodatabase you wish to create
    FileGeodatabaseConnectionPath fileGeodatabaseConnectionPath = new(new Uri(databasePath));
    // Create actual database in the specified Path unless already exists
    try
    {
      Geodatabase geodatabase = SchemaBuilder.CreateGeodatabase(fileGeodatabaseConnectionPath);
      geodatabase.Dispose();
    }
    catch (ArcGIS.Core.Data.Exceptions.GeodatabaseWorkspaceException)
    {
      // geodatabase already exists, do nothing
    }

    // Add a folder connection to a project
    var parentFolder = Directory.GetParent(databasePath);
    if (parentFolder == null)
    {
      // POC: customize the exception type
      throw new ArgumentException($"Invalid path: {databasePath}");
    }

    string parentFolderPath = parentFolder.ToString();
    var fGdbName = databasePath.Replace(parentFolderPath + '\\', string.Empty, StringComparison.Ordinal);

    string fGdbPath = parentFolder.ToString();
    Item folderToAdd = ItemFactory.Instance.Create(fGdbPath);
    // POC: QueuedTask
    QueuedTask.Run(() => Project.Current.AddItem(folderToAdd as IProjectItem));

    // Add a file geodatabase or a SQLite or enterprise database connection to a project
    var gdbToAdd = folderToAdd.GetItems().FirstOrDefault(folderItem => folderItem.Name.Equals(fGdbName, StringComparison.Ordinal));
    if (gdbToAdd is not null)
    {
      // POC: QueuedTask
      var addedGeodatabase = QueuedTask.Run(() => Project.Current.AddItem(gdbToAdd as IProjectItem));
    }

    return fGdbName;
  }
}
