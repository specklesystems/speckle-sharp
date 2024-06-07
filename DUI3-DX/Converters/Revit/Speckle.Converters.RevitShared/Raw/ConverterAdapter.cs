using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public abstract class ConverterAdapter<TOld, TNew, TProxy, TReturn> : ITypedConverter<TOld, TReturn>
  where TProxy : class, TNew 
{
  private readonly ITypedConverter<TNew, TReturn> _converter;

  protected ConverterAdapter(ITypedConverter<TNew, TReturn> converter)
  {
    _converter = converter;
  }

  public TReturn Convert(TOld target) => _converter.Convert(Create(target));

  protected abstract TProxy Create(TOld target);
}
