using RevitSharedResources.Interfaces;

namespace ConnectorRevit.Services
{
  /// <summary>
  /// This is workaround for DUI not implementing proper dependency injection.
  /// If a DUI object is required for an object in the DI container, then an instance of this class is 
  /// configured to return the required object.
  /// </summary>
  /// <typeparam name="TDUI"></typeparam>
  public class DUIEntityProvider<TDUI> : IEntityProvider<TDUI>
  {
    public TDUI Entity { get; set; }
  }
}
