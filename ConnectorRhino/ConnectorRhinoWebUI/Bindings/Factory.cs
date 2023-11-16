using System.Collections.Generic;
using ConnectorRhinoWebUI.Utils;
using DUI3;
using DUI3.Bindings;
using DUI3.Onboarding;

namespace ConnectorRhinoWebUI.Bindings;

/// <summary>
/// Creates the required bindings, in the correct order, and scaffolds any dependencies.
/// </summary>
public static class Factory
{
  private static readonly RhinoDocumentStore Store = new RhinoDocumentStore();
  public static List<IBinding> CreateBindings()
  {
    var baseBindings = new BasicConnectorBinding(Store);
    var sendBindings = new SendBinding(Store);
    var receiveBindings = new ReceiveBinding(Store);
    var selectionBindings = new SelectionBinding();

    Dictionary<string, OnboardingData> onboardings = new Dictionary<string, OnboardingData>()
    {
      {"mapper", new OnboardingData()
      {
        Title = "Mapper",
        Blurb = "Map your objects for Revit!",
        Completed = false,
        Page = "/onboarding/rhino/mapper"
      }}
    };

    var bindingsList = new List<IBinding>
    {
      new ConfigBinding(Utils.Utils.AppName, onboardings),
      new AccountBinding(),
      new TestBinding(),
      baseBindings,
      sendBindings,
      receiveBindings,
      selectionBindings
    };
      
    return bindingsList;
  }
}


