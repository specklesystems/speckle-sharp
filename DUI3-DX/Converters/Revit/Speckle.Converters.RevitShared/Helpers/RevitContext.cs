using System;
using Autodesk.Revit.UI;

namespace Speckle.Converters.RevitShared.Helpers;

public class RevitContext
{
  private UIApplication? _uiApplication;

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
