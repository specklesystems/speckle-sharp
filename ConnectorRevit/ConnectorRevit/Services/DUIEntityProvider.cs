using RevitSharedResources.Interfaces;

namespace ConnectorRevit.Services
{
  public class DUIEntityProvider<TDUI> : IEntityProvider<TDUI>
  {
    public TDUI Entity { get; set; }
  }
}
