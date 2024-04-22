using Speckle.Core.Models;

namespace Speckle.Converters.Common.Objects;

// POC: NEXT UP
// * begin scope: https://stackoverflow.com/questions/49595198/autofac-resolving-through-factory-methods
// Interceptors?

public interface IHostObjectToSpeckleConversion<in TIn>
  where TIn : class
{
  Base Convert(TIn target);
}
