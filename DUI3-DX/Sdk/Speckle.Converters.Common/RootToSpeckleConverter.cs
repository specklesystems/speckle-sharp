using Speckle.Core.Models;
using Speckle.InterfaceGenerator;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Common;

[GenerateAutoInterface]
public class RootToSpeckleConverter : IRootToSpeckleConverter
{
  private readonly IRootConvertManager _rootConvertManager;
  private readonly IProxyMapper _proxyMapper;
  private readonly IRootElementProvider _rootElementProvider;

  private readonly Type _revitElementType;

  public RootToSpeckleConverter(
    IProxyMapper proxyMapper,
    IRootConvertManager rootConvertManager,
    IRootElementProvider rootElementProvider
  )
  {
    _proxyMapper = proxyMapper;
    _rootConvertManager = rootConvertManager;
    _rootElementProvider = rootElementProvider;
    _revitElementType = _proxyMapper.GetHostTypeFromMappedType(_rootElementProvider.GetRootType()).NotNull();
  }

  public Base Convert(object target)
  {
    Type revitType = _rootConvertManager.GetTargetType(target);
    var wrapper = _proxyMapper.WrapIfExists(revitType, target);
    if (wrapper == null)
    {
      //try to fallback to element type
      if (_rootConvertManager.IsSubClass(_revitElementType, revitType))
      {
        return _rootConvertManager.Convert(
          _rootElementProvider.GetRootType(),
          _proxyMapper.CreateProxy(_rootElementProvider.GetRootType(), target)
        );
      }
      throw new NotSupportedException($"No wrapper found for Revit type: {revitType.Name}");
    }
    var (wrappedType, wrappedObject) = wrapper;
    return _rootConvertManager.Convert(wrappedType, wrappedObject);
  }
}
