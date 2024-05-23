using System.Reflection;

namespace Speckle.Autofac;

public static class AssemblyResolver
{
  public static Assembly? OnAssemblyResolve<T>(object? sender, ResolveEventArgs args)
  {
    // POC: tight binding to files
    string name = args.Name.Split(',')[0];
    string? path = Path.GetDirectoryName(typeof(T).Assembly.Location);

    if (path == null)
    {
      return null;
    }
    string assemblyFile = Path.Combine(path, name + ".dll");

    Assembly? assembly = null;
    if (File.Exists(assemblyFile))
    {
      assembly = Assembly.LoadFrom(assemblyFile);
    }
    return assembly;
  }  
  
  public static Assembly? OnAssemblyResolve(string location, ResolveEventArgs args)
  {
    // POC: tight binding to files
    string name = args.Name.Split(',')[0];
    string? path = Path.GetDirectoryName(location);

    if (path == null)
    {
      return null;
    }
    string assemblyFile = Path.Combine(path, name + ".dll");

    Assembly? assembly = null;
    if (File.Exists(assemblyFile))
    {
      assembly = Assembly.LoadFrom(assemblyFile);
    }
    return assembly;
  }
}
