using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.Runtime;

namespace Speckle.Connectors.Autocad.Plugin;

public class AutocadExtensionApplication : IExtensionApplication
{
  public void Initialize() => AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);

  public void Terminate()
  {
    // TBD
  }

  private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
  {
    Assembly a = null;
    string name = args.Name.Split(',')[0];
    string path = Path.GetDirectoryName(typeof(AutocadExtensionApplication).Assembly.Location);

    string assemblyFile = Path.Combine(path, name + ".dll");

    if (File.Exists(assemblyFile))
    {
      a = Assembly.LoadFrom(assemblyFile);
    }

    return a;
  }
}
