using System.Runtime.Serialization;
using ReactiveUI;

namespace DesktopUI2.Models.TypeMappingOnReceive;

[DataContract]
public class MappingValue : ReactiveObject, ISingleValueToMap
{
  public MappingValue(string inType, ISingleHostType inGuess, bool inNewType = false)
  {
    IncomingType = inType;
    InitialGuess = inGuess;
    NewType = inNewType;
  }

  [DataMember]
  public string IncomingType { get; set; }
  public bool NewType { get; set; }

  private ISingleHostType _initialGuess;

  [DataMember]
  public ISingleHostType InitialGuess
  {
    get => _initialGuess;
    set => this.RaiseAndSetIfChanged(ref _initialGuess, value);
  }

  private ISingleHostType _mappedHostType;

  [DataMember]
  public ISingleHostType MappedHostType
  {
    get => _mappedHostType;
    set => this.RaiseAndSetIfChanged(ref _mappedHostType, value);
  }

  public virtual string IncomingTypeDisplayName => IncomingType;
}
