using Speckle.Core.Models;

namespace Speckle.Converters.Common;

public interface ISpeckleConverterToSpeckle<in TIn>
  where TIn : class
{
  Base Convert(TIn target);
}
