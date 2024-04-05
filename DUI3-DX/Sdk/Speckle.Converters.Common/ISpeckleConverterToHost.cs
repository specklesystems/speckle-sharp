using Speckle.Core.Models;

namespace Speckle.Converters.Common;

public interface ISpeckleConverterToHost
{
  object Convert(Base target);
}
