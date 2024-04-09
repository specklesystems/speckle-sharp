using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Desktop.Core.Geoprocessing;

namespace Speckle.Converters.ArcGIS3.Utils;

public class ArcGISProjectUtils
{
  // https://pro.arcgis.com/en/pro-app/3.1/sdk/api-reference/topic14999.html
  public async Task NewFileGDB(string fGdbPath, string fGdbName)
  {
    try
    {
      await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
      {
        var fGdbVersion = "Current"; // create the 'latest' version of file Geodatabase
        var parameters = Geoprocessing.MakeValueArray(fGdbPath, fGdbName, fGdbVersion);
        var cts = new CancellationTokenSource();
        var results = Geoprocessing.ExecuteToolAsync(
          "management.CreateFileGDB",
          parameters,
          null,
          cts.Token,
          (eventName, o) => { }
        );

        // Opens a file geodatabase. This will open the geodatabase if the folder exists and contains a valid geodatabase.
        Geodatabase geodatabase = new Geodatabase(
          new FileGeodatabaseConnectionPath(new Uri(fGdbPath + "\\" + fGdbName)) // @"C:\Data\LocalGovernment.gdb"))
        );

        return true;
      });
    }
    catch (GeodatabaseNotFoundOrOpenedException exception)
    {
      // Handle Exception.
    }
  }
}
