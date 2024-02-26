using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.UI;

namespace Speckle.Connectors.Revit.Plugin
{
  public class RevitContext
  {
    public UIApplication? _uiApplication;

    public UIApplication? UIApplication
    {
      get => _uiApplication;
      set
      {
        if (_uiApplication != null)
        {
          throw new ArgumentException("UIApplication already set");
        }

        _uiApplication = value;
      }
    }
  }
}
