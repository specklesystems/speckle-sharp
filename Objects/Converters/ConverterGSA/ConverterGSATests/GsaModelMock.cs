using Speckle.ConnectorGSA.Proxy.Cache;
using Speckle.GSA.API;

namespace ConverterGSATests
{
  public class GsaModelMock : GsaModelBase
  {
    public override IGSACache Cache { get; set; } = new GsaCache();
    public override IGSAProxy Proxy { get; set; } = new GsaProxyMockForConverterTests();
    public override IGSAMessenger Messenger { get; set; } = new GsaMessengerMock();
  }
}
