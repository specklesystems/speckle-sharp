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
  private static readonly RhinoDocumentStore s_store = new();

  public static List<IBinding> CreateBindings()
  {
    BasicConnectorBinding baseBindings = new(s_store);
    SendBinding sendBindings = new(s_store);
    ReceiveBinding receiveBindings = new(s_store);
    SelectionBinding selectionBindings = new();

    // Where we pass connector specific onboardings to config binding.
    // Below code is just a sample for now!
    Dictionary<string, OnboardingData> sampleOnboardingsData =
      new()
      {
        {
          "mapper",
          new OnboardingData()
          {
            Title = "Mapper",
            Blurb = "Map your objects for Revit!",
            Completed = false,
            Page = "/onboarding/rhino/mapper"
          }
        }
      };

    List<IBinding> bindingsList =
      new()
      {
        new ConfigBinding(Utils.Utils.AppName, sampleOnboardingsData),
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
