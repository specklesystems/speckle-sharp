using System.Text;
using DesktopUI2.Models.TypeMappingOnReceive;

namespace ConnectorRevit.TypeMapping
{
  internal class RevitHostType : HostType
  {
    public RevitHostType(string hostFamilyName, string hostTypeName) : base(hostTypeName)
    {
      HostFamilyName = hostFamilyName;
    }
    public string HostFamilyName { get; }
    private string _hostTypeDisplayName;
    public override string HostTypeDisplayName
    {
      get
      {
        if (_hostTypeDisplayName != null) return _hostTypeDisplayName;

        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(HostFamilyName))
        {
          sb.Append(HostFamilyName);
          sb.Append(' ');
        }
        sb.Append(HostTypeName);
        _hostTypeDisplayName = sb.ToString();
        return _hostTypeDisplayName;
      }
    }
  }
}
