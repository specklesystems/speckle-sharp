using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using ReactiveUI;

namespace DesktopUI2.Models.TypeMappingOnReceive
{
  [DataContract]
  public class MappingValue : ReactiveObject, ISingleValueToMap
  {
    private string _initialGuess;
    private string _outgoingType;

    public MappingValue(string inType, string inGuess, string inFamily = null, bool inNewType = false)
    {
      IncomingType = inType;
      InitialGuess = inGuess;
      IncomingFamily = inFamily;
      NewType = inNewType;
    }

    [DataMember]
    public string IncomingType { get; set; }
    [DataMember]
    public string IncomingFamily { get; set; }
    public bool NewType { get; set; }

    [DataMember]
    public string InitialGuess
    {
      get => _initialGuess;
      set => this.RaiseAndSetIfChanged(ref _initialGuess, value);
    }

    [DataMember]
    public string OutgoingType
    {
      get => _outgoingType;
      set => this.RaiseAndSetIfChanged(ref _outgoingType, value);
    }

    public string IncomingTypeDisplayName
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
}
