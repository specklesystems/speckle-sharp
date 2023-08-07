using System;
using System.Collections.Generic;
using System.Windows.Controls;
using DUI3;
using JetBrains.Annotations;
using Rhino;
using Rhino.PlugIns;
using Rhino.UI;

namespace ConnectorRhinoWebUI
{
  ///<summary>
  /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
  /// class. DO NOT create instances of this class yourself. It is the
  /// responsibility of Rhino to create an instance of this class.</para>
  /// <para>To complete plug-in information, please also see all PlugInDescription
  /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
  /// "Show All Files" to see it in the "Solution Explorer" window).</para>
  ///</summary>
  public class ConnectorRhinoWebUiPlugin : PlugIn
  {
    public static ConnectorRhinoWebUiPlugin Instance { get; private set; }
    
    public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;
    
    public ConnectorRhinoWebUiPlugin()
    {
      Instance = this;
      RhinoApp.Idle += OnIdle;
    }

    private void OnIdle(object sender, EventArgs e)
    {
      // Run Speckle command whenever Rhino become idle.
      RhinoApp.RunScript("SpeckleWebUIWebView2", false);
      
      // Unsubscribe from idle event after calling command, because we wont attempt to call command in same instance.
      RhinoApp.Idle -= OnIdle;
    }
  }
}
