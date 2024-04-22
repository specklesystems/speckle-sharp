using Speckle.Core.Models;

namespace Speckle.Converters.Common;

public interface ISpeckleConverterToHost<out TOut>
  where TOut : class
{
  TOut Convert(Base target);
}
