using RevitSharedResources.Interfaces;

namespace ConnectorRevit.Services
{
  internal class DUIEntityProvider<TDUI> : IEntityProvider<TDUI>
  {
    public TDUI Entity { get; set; }
  }
}
