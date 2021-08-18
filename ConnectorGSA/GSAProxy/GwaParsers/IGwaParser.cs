using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  public interface IGwaParser
  {
    GsaRecord Record { get; }
    Type GsaSchemaType { get; }
    bool FromGwa(string gwa);
    bool Gwa(out List<string> gwa, bool includeSet = false);
  }
}
