using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Speckle.Connectors.Utils;

public interface IConversionResult
{
  public string? ResultId { get; }

  public string? ErrorMessage { get; }
}

public sealed class ReceiveConversionResult
{
  public string? ResultId { get; set; }

  [JsonIgnore]
  public object? Result { get; }

  [JsonIgnore]
  public Exception? Error { get; }

  public string? ErrorMessage { get; set; }

  [JsonIgnore]
  public Base? Target { get; }

  public string? TargetId { get; set; }

  public string? TargetType { get; set; }
  public string? TargetAppId { get; set; }

  public bool IsSuccessful { get; set; }

  internal ReceiveConversionResult() { }

  public ReceiveConversionResult(Base target, object result, string resultId)
  {
    Target = target;
    TargetType = Target?.speckle_type.Split('.').Last();
    TargetId = Target?.id;
    TargetAppId = Target?.applicationId;
    Result = result;
    ResultId = resultId;
    IsSuccessful = Result is not null;
  }

  public ReceiveConversionResult(Base target, Exception error)
  {
    Target = target;
    TargetType = Target?.speckle_type.Split('.').Last();
    TargetId = Target?.id;
    TargetAppId = Target?.applicationId;
    Error = error;
    ErrorMessage = Error.Message;
    IsSuccessful = Result is not null;
  }

  public override string ToString() =>
    IsSuccessful
      ? $"Successfully converted {Target} to {Result}"
      : $"Failed to convert {Target}: {Error!.GetType()}: {Error!.Message}";
}
