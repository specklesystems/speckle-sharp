using Speckle.Core.Models;

namespace Speckle.Converters.Common.Objects;

public interface IToSpeckleConverter<TIn>
{
  Base ConvertToSpeckle(TIn target);
}
