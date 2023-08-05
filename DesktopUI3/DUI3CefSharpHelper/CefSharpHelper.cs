using System.Collections.Generic;
using DUI3;

namespace DUI3CefSharpHelper;

public class CefSharpHelper
{
  private List<IBinding> _bindings;

  public CefSharpHelper(List<IBinding> bindings)
  {
    _bindings = bindings;
  }
}

