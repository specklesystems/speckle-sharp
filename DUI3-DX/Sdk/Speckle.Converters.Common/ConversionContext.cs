﻿using Speckle.InterfaceGenerator;

namespace Speckle.Converters.Common;

[GenerateAutoInterface]
public class ConversionContext<TDocument> : IConversionContext<TDocument>
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
