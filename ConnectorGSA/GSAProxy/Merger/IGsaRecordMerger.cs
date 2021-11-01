using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;

namespace Speckle.ConnectorGSA.Proxy.Merger
{
  public interface IGsaRecordMerger
  {
    void Initialise(List<Type> typesToRecognise);

    GsaRecord Merge(GsaRecord src, GsaRecord dest);
  }
}
