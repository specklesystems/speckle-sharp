using Speckle.Converters.Common;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.DependencyInjection;

public class RhinoRootElementProvider : IRootElementProvider
{
  public Type GetRootType() => typeof(IRhinoObject);
}
