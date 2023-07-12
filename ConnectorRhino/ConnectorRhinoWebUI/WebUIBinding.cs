using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using DUI3;
using Speckle.Core.Credentials;

namespace ConnectorRhinoWebUI
{
  public class RhinoBaseBindings : IBasicConnectorBinding
  {
    public string Name { get; set;  } = "baseBindings";

    public IBridge Parent { get; set; }
    
    public string GetSourceApplicationName() => "Rhino";

    public string GetSourceApplicationVersion() => "42";

    public Account[] GetAccounts()
    {
      return AccountManager.GetAccounts().ToArray();
    }

    // etc.
  }
}

