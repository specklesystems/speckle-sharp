using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Speckle.Connectors.Utils;

public sealed class ReceiveConversionResult
{
  public string? ResultId { get; }

  [JsonIgnore]
  public object? Result { get; }
  public Exception? Error { get; }

  [JsonIgnore]
  public Base Target { get; }

  public string TargetId => Target.id;
  public string? TargetAppId => Target.applicationId;

  //[MemberNotNullWhen(true, nameof(Result))]
  //[MemberNotNullWhen(true, nameof(ResultId))]
  //[MemberNotNullWhen(false, nameof(Error))]
  public bool IsSuccessful => Result is not null;

  public ReceiveConversionResult(Base target, object result, string resultId)
  {
    Target = target;
    Result = result;
    ResultId = resultId;
  }

  public ReceiveConversionResult(Base target, Exception error)
  {
    Target = target;
    Error = error;
  }

  public override string ToString() =>
    IsSuccessful
      ? $"Successfully converted {Target} to {Result}"
      : $"Failed to convert {Target}: {Error!.GetType()}: {Error!.Message}";
}
