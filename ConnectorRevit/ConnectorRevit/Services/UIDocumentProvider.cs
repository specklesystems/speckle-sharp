using Autodesk.Revit.UI;
using RevitSharedResources.Interfaces;

namespace ConnectorRevit.Services
{
  public class UIDocumentProvider : IEntityProvider<UIDocument>
  {
    private UIApplication revitApplication;

    public UIDocumentProvider(UIApplication revitApplication)
    {
      this.revitApplication = revitApplication;
    }

    private UIDocument uiDocument;

    public UIDocument Entity 
    { 
      get => uiDocument ?? revitApplication.ActiveUIDocument; 
      set => uiDocument = value; 
    }
  }
}
