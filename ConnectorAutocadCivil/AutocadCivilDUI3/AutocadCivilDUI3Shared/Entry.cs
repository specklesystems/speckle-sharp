using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.Windows;
using Autodesk.AutoCAD.Runtime;
using Speckle.ConnectorAutocadDUI3;

namespace AutocadCivilDUI3Shared;

public class App : IExtensionApplication
{
  public void Initialize()
  {
    AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);
  }

  public void Terminate()
  {
    // Shh.
  }
  
  Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
  {
    Assembly a = null;
    var name = args.Name.Split(',')[0];
    string path = Path.GetDirectoryName(typeof(App).Assembly.Location);

    string assemblyFile = Path.Combine(path, name + ".dll");

    if (File.Exists(assemblyFile))
      a = Assembly.LoadFrom(assemblyFile);

    return a;
  }
}

public class SpeckleAutocadDUI3Command
{
  private static PaletteSet PaletteSet { get; set; }
  private static readonly Guid Id = new Guid("6AD40744-85BF-4B62-9408-5D3CCEB8B876");
  
  [CommandMethod("SpeckleDUI3")]
  public void SpeckleCommand()
  {
    if (PaletteSet != null)
    {
      FocusPalette();
      return;
    }

    PaletteSet = new PaletteSet("Speckle DUI3", Id);
    PaletteSet.Size = new Size(400, 500);
    PaletteSet.DockEnabled = (DockSides)((int)DockSides.Left + (int)DockSides.Right);

    // NOTE: Autocad 2022 seems to support Webview2 rather well, hence I (Dim) have removed
    // all references to Cef. CefSharp also worked rather fine, and we would need to match
    // the correct versions, etc.. But it seems it's not needed!
    var panelWebView = new DUI3PanelWebView();
    
    PaletteSet.AddVisual("Speckle DUI3 WebView", panelWebView);

    FocusPalette();
  }

  private void FocusPalette()
  {
    PaletteSet.KeepFocus = true;
    PaletteSet.Visible = true;
  }
}

