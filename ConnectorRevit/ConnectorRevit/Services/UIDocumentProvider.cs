using UI = Autodesk.Revit.UI;
using RevitSharedResources.Interfaces;

namespace ConnectorRevit.Services
{
  /// <summary>
  /// Provides the current <see cref="UI.UIDocument"/> to any dependencies which may need it
  /// </summary>
  public class UIDocumentProvider : IEntityProvider<UI.UIDocument>
  {
    private UI.UIApplication revitApplication;

    public UIDocumentProvider(UI.UIApplication revitApplication)
    {
      this.revitApplication = revitApplication;
    }

    private UI.UIDocument uiDocument;

    public UI.UIDocument Entity 
    { 
      get => uiDocument ?? revitApplication.ActiveUIDocument; 
      set => uiDocument = value; 
    }
  }
}
