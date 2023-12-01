using Speckle.Core.Kits;

namespace DUI3.Utils;

public static class Converters
{
  public static ISpeckleConverter GetConverter<T>(T document, string appNameVersion)
  {
    ISpeckleConverter converter = KitManager.GetDefaultKit().LoadConverter(appNameVersion);
    converter.SetContextDocument(document);
    return converter;
  }
}
