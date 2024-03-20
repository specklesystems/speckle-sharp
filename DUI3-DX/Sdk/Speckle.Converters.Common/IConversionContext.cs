using System;

namespace Speckle.Converters.Common;

public interface IConversionContext<TDocument, THostUnit>
  where TDocument : class
{
  public TDocument Document { get; }
  public string SpeckleUnits { get; }
}
