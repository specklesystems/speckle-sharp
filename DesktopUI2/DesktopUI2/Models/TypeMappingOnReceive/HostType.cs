namespace DesktopUI2.Models.TypeMappingOnReceive
{
  public class HostType : ISingleHostType
  {
    public string HostTypeName { get; }

    public HostType(string hostTypeName)
    {
      HostTypeName = hostTypeName;
    }

    public virtual string HostTypeDisplayName => HostTypeName;
  }
}
