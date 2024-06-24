using System.Drawing;
using System.Reflection;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.Autocad.DependencyInjection;
using Speckle.Core.Kits;
using Speckle.Connectors.Autocad.Interfaces;
using Speckle.Connectors.DUI.WebView;

namespace Speckle.Connectors.Autocad.Plugin;

public class AutocadCommand
{
  private static PaletteSet? PaletteSet { get; set; }
  private static readonly Guid s_id = new("3223E594-1B09-4E54-B3DD-8EA0BECE7BA5");
  private IAutocadPlugin? _autocadPlugin;

  public SpeckleContainer? Container { get; private set; }

  [CommandMethod("SpeckleNewUI")]
  public void Command()
  {
    if (PaletteSet != null)
    {
      FocusPalette();
      return;
    }

    PaletteSet = new PaletteSet("Speckle DUI3", s_id)
    {
      Size = new Size(400, 500),
      DockEnabled = (DockSides)((int)DockSides.Left + (int)DockSides.Right)
    };

    var builder = SpeckleContainerBuilder.CreateInstance();

#if CIVIL3D2024
    AutocadSettings autocadSettings = new (HostApplications.Civil3D, HostAppVersion.v2024);
#elif AUTOCAD2023
    AutocadSettings autocadSettings = new(HostApplications.AutoCAD, HostAppVersion.v2023);
#else
    AutocadSettings autocadSettings = new(HostApplications.AutoCAD, HostAppVersion.v2023);
#endif
    var executingAssembly = Assembly.GetExecutingAssembly();
    Container = builder
      .LoadAutofacModules(Assembly.GetExecutingAssembly(), autocadSettings.Modules)
      .AddSingleton(autocadSettings)
      .Build();

    // Resolve root plugin object and initialise.
    _autocadPlugin = Container.Resolve<IAutocadPlugin>();
    _autocadPlugin.Initialise();

    var panelWebView = Container.Resolve<DUI3ControlWebView>();

    PaletteSet.AddVisual("Speckle DUI3 WebView", panelWebView);

    FocusPalette();
  }

  private void FocusPalette()
  {
    if (PaletteSet != null)
    {
      PaletteSet.KeepFocus = true;
      PaletteSet.Visible = true;
    }
  }
}
