using Speckle.Converters.Common;
using Speckle.Revit2023.Interfaces;

namespace Speckle.Converters.Revit2023.Tests;

public static class RevitConversionContextStack
{
  public const double TOLERANCE = 0.0164042; // 5mm in ft
}

public interface IRevitConversionContextStack : IConversionContextStack<IRevitDocument, IRevitForgeTypeId> { }

public interface IScalingServiceToSpeckle
{
  double ScaleLength(double length);
  double Scale(double value, IRevitForgeTypeId forgeTypeId);
}
