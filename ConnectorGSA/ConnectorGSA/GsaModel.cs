using Speckle.GSA.API;
using Speckle.ConnectorGSA.Proxy.Cache;

namespace ConnectorGSA
{
  public class GsaModel : GsaModelBase
  {
    public static IGSAModel Instance = new GsaModel();

    private static IGSACache cache = new GsaCache();
    private static IGSAProxy proxy = new Speckle.ConnectorGSA.Proxy.GsaProxy();
    private static IGSAMessenger messenger = new GsaMessenger();

    public override IGSACache Cache { get => cache; set => cache = value; }
    public override IGSAProxy Proxy { get => proxy; set => proxy = value; }
    public override IGSAMessenger Messenger { get => messenger; set => messenger = value; }

    public GsaModel()
    {
      if (Speckle.GSA.API.Instance.GsaModel == null)
      {
        Speckle.GSA.API.Instance.GsaModel = this;
      }
    }
  }
}
