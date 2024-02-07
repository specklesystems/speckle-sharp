using System.Runtime.Serialization;
using System.Text;
using DesktopUI2.Models.TypeMappingOnReceive;

namespace ConnectorRevit.TypeMapping;

[DataContract]
public class RevitMappingValue : MappingValue
{
  public RevitMappingValue(string inType, ISingleHostType inGuess, string inFamily = null, bool inNewType = false)
    : base(inType, inGuess, inNewType)
  {
    IncomingFamily = inFamily;
  }

  [DataMember]
  public string IncomingFamily { get; set; }

  public override string IncomingTypeDisplayName
  {
    get
    {
      var sb = new StringBuilder();
      if (!string.IsNullOrEmpty(IncomingFamily))
      {
        sb.Append(IncomingFamily);
        sb.Append(' ');
      }
      sb.Append(IncomingType);
      return sb.ToString();
    }
  }
}
