using System;
using System.Drawing;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Windows;
using Autodesk.AutoCAD.Runtime;

using Speckle.ConnectorAutocadDUI3;

namespace AutocadCivilDUI3Shared;


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

// #if AUTOCAD2023DUI3
    // Just because we shouldn't, doesn't mean we can't.
    // var panelCefSharp = new DUI3PanelCefSharp();
    // PaletteSet.AddVisual("Speckle DUI3 CefSharp", panelCefSharp);
    
    var panelWebView = new DUI3PanelWebView();
    PaletteSet.AddVisual("Speckle DUI3 WebView", panelWebView);
// #endif

    FocusPalette();
  }

  private void FocusPalette()
  {
    PaletteSet.KeepFocus = true;
    PaletteSet.Visible = true;
  }
}

