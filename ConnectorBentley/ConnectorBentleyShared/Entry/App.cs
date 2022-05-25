using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;
using Stylet.Xaml;

using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.Elements;
using Bentley.GeometryNET;
using Bentley.MstnPlatformNET;

using Speckle.ConnectorBentley.UI;
using System.Reflection;
using System.IO;

namespace Speckle.ConnectorBentley.Entry
{
  [AddIn(MdlTaskID = "Speckle")]
  public class SpeckleBentleyApp : AddIn
  {
    public static SpeckleBentleyApp App;

    public SpeckleBentleyApp(IntPtr mdlDesc) : base(mdlDesc)
    {
    }

    protected override int Run(string[] commandLine)
    {
      App = this;
      return 0;
    }

    internal static SpeckleBentleyApp Instance
    {
      get { return App; }
    }

    public void Start(string unparsed)
    {
      SpeckleBentleyCommand.ShowPanel();
    }

    // for DUI2
    public void Start2(string unparsed)
    {
      AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);

      SpeckleBentleyCommand2.InitAvalonia();
      SpeckleBentleyCommand2.Bindings = new ConnectorBindingsBentley2();
      SpeckleBentleyCommand2.CreateOrFocusSpeckle();
    }

    // This is necessary for finding assemblies when building Avalonia 
    static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
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
  
}