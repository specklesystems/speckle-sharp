using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace Speckle.Converters.ArcGIS3.Utils;

public class ArcGISProjectUtils
{
  public async Task addDatabaseToProject(string fGdbPath, string fGdbName)
  {
    // Create a FileGeodatabaseConnectionPath with the name of the file geodatabase you wish to create
    FileGeodatabaseConnectionPath fileGeodatabaseConnectionPath = new FileGeodatabaseConnectionPath(
      new Uri(fGdbPath + "\\" + fGdbName)
    );
    // Create actual database in the specified Path unless already exists
    try
    {
      Geodatabase geodatabase = SchemaBuilder.CreateGeodatabase(fileGeodatabaseConnectionPath);
    }
    catch (ArcGIS.Core.Data.Exceptions.GeodatabaseWorkspaceException) { }

    /// Add a folder connection to a project
    Item folderToAdd = ItemFactory.Instance.Create(fGdbPath);
    bool wasAdded = await QueuedTask.Run(() => Project.Current.AddItem(folderToAdd as IProjectItem));

    /// Add a file geodatabase or a SQLite or enterprise database connection to a project
    Item gdbToAdd = folderToAdd.GetItems().FirstOrDefault(folderItem => folderItem.Name.Equals(fGdbName));
    var addedGeodatabase = await QueuedTask.Run(() => Project.Current.AddItem(gdbToAdd as IProjectItem));
  }
}
