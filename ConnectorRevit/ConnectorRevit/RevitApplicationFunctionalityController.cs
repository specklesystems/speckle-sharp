using System.Threading.Tasks;
using Autodesk.Revit.UI;
using RevitSharedResources.Models;
using Speckle.BatchUploader.ClientSdk.Interfaces;

namespace ConnectorRevit
{
  internal class RevitApplicationController : IApplicationFunctionalityController
  {
    private readonly UIApplication application;

    public RevitApplicationController(UIApplication application)
    {
      this.application = application;
    }

    public async Task OpenDocument(string path)
    {
      await APIContext
        .Run(() =>
        {
          application.OpenAndActivateDocument(path);
        }).ConfigureAwait(false);
    }
  }
}
