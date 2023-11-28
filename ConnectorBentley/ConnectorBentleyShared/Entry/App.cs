using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.Elements;
using Bentley.GeometryNET;
using Bentley.MstnPlatformNET;
using DesktopUI2;
using Speckle.ConnectorBentley.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Speckle.ConnectorBentley.Entry;

[AddIn(MdlTaskID = "Speckle")]
public class SpeckleBentleyApp : AddIn
{
  public static SpeckleBentleyApp App;

  public SpeckleBentleyApp(IntPtr mdlDesc)
    : base(mdlDesc) { }

  protected override int Run(string[] commandLine)
  {
    App = this;
    return 0;
  }

  internal static SpeckleBentleyApp Instance
  {
    get { return App; }
  }

  // for DUI2
  public void Start(string unparsed)
  {
    AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);

    SpeckleBentleyCommand.InitAvalonia();
    SpeckleBentleyCommand.Bindings = new ConnectorBindingsBentley();
    SpeckleBentleyCommand.CreateOrFocusSpeckle();
  }

  // This is necessary for finding assemblies when building Avalonia
  static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
  {
    Assembly a = null;
    var name = args.Name.Split(',')[0];
    string path = Path.GetDirectoryName(typeof(App).Assembly.Location);

    string assemblyFile = Path.Combine(path, name + ".dll");

    if (File.Exists(assemblyFile))
    {
      a = Assembly.LoadFrom(assemblyFile);
    }

    return a;
  }
}
