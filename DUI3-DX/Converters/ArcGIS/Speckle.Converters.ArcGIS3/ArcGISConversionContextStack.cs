using System.Diagnostics.CodeAnalysis;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Speckle.Converters.ArcGIS3.Utils;
using Speckle.Converters.Common;

namespace Speckle.Converters.ArcGIS3;

public class ArcGISDocument
{
  public Project Project { get; }
  public Map Map { get; }
  public Uri SpeckleDatabasePath { get; }
  public CRSoffsetRotation ActiveCRSoffsetRotation { get; set; }

  public ArcGISDocument()
  {
    Project = Project.Current;
    Map = MapView.Active.Map;
    SpeckleDatabasePath = EnsureOrAddSpeckleDatabase();
    // CRS of either: incoming commit to be applied to all received objects, or CRS to convert all objects to, before sending
    // created per Send/Receive operation, will be the same for all objects in the operation
    ActiveCRSoffsetRotation = new CRSoffsetRotation(MapView.Active.Map.SpatialReference);
  }

  private const string FGDB_NAME = "Speckle.gdb";

  public Uri EnsureOrAddSpeckleDatabase()
  {
    return AddDatabaseToProject(GetDatabasePath());
  }

  public Uri GetDatabasePath()
  {
    try
    {
      var parentDirectory = Directory.GetParent(Project.Current.URI);
      if (parentDirectory == null)
      {
        throw new ArgumentException($"Project directory {Project.Current.URI} not found");
      }
      var fGdbPath = new Uri(parentDirectory.FullName);
      return new Uri($"{fGdbPath}/{FGDB_NAME}");
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
  }

  public Uri AddDatabaseToProject(Uri databasePath)
  {
    // Create a FileGeodatabaseConnectionPath with the name of the file geodatabase you wish to create
    FileGeodatabaseConnectionPath fileGeodatabaseConnectionPath = new(databasePath);
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
    var parentFolder = Path.GetDirectoryName(databasePath.AbsolutePath);
    if (parentFolder == null)
    {
      // POC: customize the exception type
      throw new ArgumentException($"Invalid path: {databasePath}");
    }
    var fGdbName = databasePath.Segments[^1];
    Item folderToAdd = ItemFactory.Instance.Create(parentFolder);
    // POC: QueuedTask
    QueuedTask.Run(() => Project.Current.AddItem(folderToAdd as IProjectItem));

    // Add a file geodatabase or a SQLite or enterprise database connection to a project
    var gdbToAdd = folderToAdd
      .GetItems()
      .FirstOrDefault(folderItem => folderItem.Name.Equals(fGdbName, StringComparison.Ordinal));
    if (gdbToAdd is not null)
    {
      // POC: QueuedTask
      var addedGeodatabase = QueuedTask.Run(() => Project.Current.AddItem(gdbToAdd as IProjectItem));
    }

    return databasePath;
  }
}

// POC: Suppressed naming warning for now, but we should evaluate if we should follow this or disable it.
[SuppressMessage(
  "Naming",
  "CA1711:Identifiers should not have incorrect suffix",
  Justification = "Name ends in Stack but it is in fact a Stack, just not inheriting from `System.Collections.Stack`"
)]
public class ArcGISConversionContextStack : ConversionContextStack<ArcGISDocument, ACG.Unit>
{
  public ArcGISConversionContextStack(
    IHostToSpeckleUnitConverter<ACG.Unit> unitConverter,
    ArcGISDocument arcGisDocument
  )
    : base(arcGisDocument, MapView.Active.Map.SpatialReference.Unit, unitConverter) { }
}
