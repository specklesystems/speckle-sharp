namespace Speckle.Converters.Common;

// POC: record?
public class ConversionContext<TDocument>
  where TDocument : class
{
  public ConversionContext(TDocument doc, string speckleUnits)
  {
    Document = doc;
    SpeckleUnits = speckleUnits;
  }

  public TDocument Document { get; }
  public string SpeckleUnits { get; private set; }
}
