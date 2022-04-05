using System;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using Rhino;

namespace SpeckleRhino
{
  public class RhinoViewModel: MainWindowViewModel
  {
    public RhinoDoc doc { get; set; }

    public RhinoViewModel()
    {
    }
  }
}
