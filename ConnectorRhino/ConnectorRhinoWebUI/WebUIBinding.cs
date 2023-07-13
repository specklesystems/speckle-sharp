using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using DUI3;
using Speckle.Core.Credentials;

namespace ConnectorRhinoWebUI
{
  /// <summary>
  /// Needs full scoping
  /// </summary>
  public class RhinoBaseBindings : IBasicConnectorBinding
  {
    public string Name { get; set;  } = "baseBinding";

    public IBridge Parent { get; set; }
    
    public string GetSourceApplicationName() => "Rhino";

    public string GetSourceApplicationVersion() => "42";

    public Account[] GetAccounts()
    {
      return AccountManager.GetAccounts().ToArray();
    }

    // etc.
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

