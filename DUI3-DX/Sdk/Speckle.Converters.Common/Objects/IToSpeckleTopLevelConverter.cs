using Speckle.Core.Models;

namespace Speckle.Converters.Common.Objects;

// POC: NEXT UP
// * begin scope: https://stackoverflow.com/questions/49595198/autofac-resolving-through-factory-methods
// Interceptors?

public interface IToSpeckleTopLevelConverter
{
  Base Convert(object target);
}
