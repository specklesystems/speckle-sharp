using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public abstract class ConverterAdapter<TOld, TNew, TProxy> : ITypedConverter<TOld, Base>
  where TProxy : class, TNew 
{
  private readonly ITypedConverter<TNew, Base> _converter;

  protected ConverterAdapter(ITypedConverter<TNew, Base> converter)
  {
    _converter = converter;
  }

  public Base Convert(TOld target) => _converter.Convert(Create(target));

  protected abstract TProxy Create(TOld target);
}
