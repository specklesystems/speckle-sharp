using Speckle.Core.Models;

namespace Speckle.Converters.Common;

public interface ISpeckleConverterToSpeckle
{
  Base Convert(object target);
}
