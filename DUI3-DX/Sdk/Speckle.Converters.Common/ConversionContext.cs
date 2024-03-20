namespace Speckle.Converters.Common;

public abstract class ConversionContext<TDocument, THostUnit> : IConversionContext<TDocument, THostUnit>
  where TDocument : class
{
  private readonly IHostToSpeckleUnitConverter<THostUnit> _unitConverter;
  private readonly THostUnit _hostUnits;

  protected ConversionContext(TDocument doc, THostUnit hostUnit, IHostToSpeckleUnitConverter<THostUnit> unitConverter)
  {
    _unitConverter = unitConverter;
    Document = doc;
    _hostUnits = hostUnit;
    SpeckleUnits = _unitConverter.ConvertOrThrow(_hostUnits);
  }

  public TDocument Document { get; protected set; }
  public string SpeckleUnits { get; private set; }
}
