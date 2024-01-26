namespace DesktopUI2.Models.TypeMappingOnReceive;

public interface ISingleValueToMap
{
  public string IncomingType { get; set; }
  public string IncomingTypeDisplayName { get; }
  public ISingleHostType InitialGuess { get; set; }
  public ISingleHostType MappedHostType { get; set; }
}
