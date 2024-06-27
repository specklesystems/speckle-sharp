using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public abstract class BaseTopLevelConverterToHost<TSpeckle, THost> : IToHostTopLevelConverter
  where TSpeckle : Base
  where THost : notnull
{
  public abstract THost Convert(TSpeckle target);

  public object Convert(Base target)
  {
    var result = Convert((TSpeckle)target);
    return result;
  }
}
