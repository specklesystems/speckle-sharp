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
    private string _outgoingFamily;
    private string _outgoingType;

    public MappingValue(string inType, string inGuess, bool inNewType = false)
    {
      IncomingType = inType;
      InitialGuess = inGuess;
      NewType = inNewType;
    }

    [DataMember]
    public string IncomingType { get; set; }
    public bool Imported { get; set; }
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

    public string OutgoingFamily
    {
      get => _outgoingFamily;
      set => this.RaiseAndSetIfChanged(ref _outgoingFamily, value);
    }
  }
}
