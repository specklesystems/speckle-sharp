using Speckle.Converters.Common;
using Speckle.Revit2023.Interfaces;

namespace Speckle.Connectors.Revit2023.Tests;

public interface IRevitConversionContextStack : IConversionContextStack<IRevitDocument, IRevitForgeTypeId> { }

public interface IScalingServiceToSpeckle
{
  double ScaleLength(double length);
  double Scale(double value, IRevitForgeTypeId forgeTypeId);
}
