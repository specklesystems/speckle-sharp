using System.Linq;
using DUI3;
using Rhino;
using Speckle.Core.Credentials;

namespace ConnectorRhinoWebUI
{
  /// <summary>
  /// Needs full scoping
  /// </summary>
  public class RhinoBaseBindings : IBasicConnectorBinding
  {
    public string Name { get; set; } = "baseBinding";

    public IBridge Parent { get; set; }

    public RhinoBaseBindings()
    {
      RhinoDoc.EndOpenDocumentInitialViewUpdate += (sender, e) =>
      {
        if (e.Merge) return;
        if (e.Document == null) return;
        Parent.SendToBrowser("documentChanged");
      };
    }

    public string GetSourceApplicationName() => "Rhino";

    public string GetSourceApplicationVersion() => "42";

    public Account[] GetAccounts() => AccountManager.GetAccounts().ToArray();

    public DocumentInfo GetDocumentInfo() => new DocumentInfo
    {
      Location = RhinoDoc.ActiveDoc.Path,
      Name = RhinoDoc.ActiveDoc.Name,
      Id = RhinoDoc.ActiveDoc.RuntimeSerialNumber.ToString()
    };

  }


  /// <summary>
  /// Really just for testing purposes.
  /// </summary>
  public class RhinoRandomBinding : IBinding
  {
    public string Name { get; set; } = "rhinoRandomBinding";

    public IBridge Parent { get; set; }

    public string MakeGreeting(string name)
    {
      return $"Hello {name}! Hope you're  having a good day.";
    }

  }
}

