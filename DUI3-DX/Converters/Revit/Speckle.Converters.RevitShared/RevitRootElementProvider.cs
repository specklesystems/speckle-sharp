using Speckle.Converters.Common;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared;

public class RevitRootElementProvider : IRootElementProvider
{
  private static readonly Type s_wrappedElementType = typeof(IRevitElement);

  public Type GetRootType() => s_wrappedElementType;
}
