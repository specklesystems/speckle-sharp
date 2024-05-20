using Speckle.Core.Models;

namespace Speckle.Converters.Common;

public interface IRootToSpeckleConverter
{
  Base Convert(object target);
}
