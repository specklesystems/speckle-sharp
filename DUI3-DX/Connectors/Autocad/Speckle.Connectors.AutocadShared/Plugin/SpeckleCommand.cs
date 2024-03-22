using System.Drawing;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Autofac;
using Speckle.Autofac.DependencyInjection;
using Speckle.Autofac.Files;
using Speckle.Connectors.Autocad.DependencyInjection;
using Speckle.Connectors.Autocad.HostApp;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models.Extensions;

namespace Speckle.Connectors.Autocad.Plugin;

public class SpeckleCommand
{
  private static PaletteSet PaletteSet { get; set; }
  private static readonly Guid s_id = new("3223E594-1B09-4E54-B3DD-8EA0BECE7BA5");
  private IAutocadPlugin? _autocadPlugin;

  public AutofacContainer? Container { get; private set; } // NOT SURE!

  [CommandMethod("SpeckleDUI3DX")]
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

    Container = new AutofacContainer(new StorageInfo());
    //Container.PreBuildEvent += ContainerPreBuildEvent;

    var autocadSettings = new AutocadSettings(HostApplications.AutoCAD, HostAppVersion.v2023);

    Container
      .AddModule(new AutofacAutocadModule())
      .LoadAutofacModules(autocadSettings.Modules)
      .AddSingletonInstance(autocadSettings)
      .Build();

    // Resolve root plugin object and initialise.
    _autocadPlugin = Container.Resolve<IAutocadPlugin>();
    _autocadPlugin.Initialise();

    // NOTE: Autocad 2022 seems to support Webview2 rather well, hence I (Dim) have removed
    // all references to Cef. CefSharp also worked rather fine, and we would need to match
    // the correct versions, etc.. But it seems it's not needed!
    var panelWebView = Container.Resolve<Dui3PanelWebView>();

    PaletteSet.AddVisual("Speckle DUI3 WebView", panelWebView);

    FocusPalette();
  }

  private void FocusPalette()
  {
    PaletteSet.KeepFocus = true;
    PaletteSet.Visible = true;
  }

  private void ContainerPreBuildEvent(object sender, ContainerBuilder containerBuilder)
  {
    //containerBuilder.InjectNamedTypes<IHostObjectToSpeckleConversion>();
  }
}
