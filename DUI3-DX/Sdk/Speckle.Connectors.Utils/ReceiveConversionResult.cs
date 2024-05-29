using System.Diagnostics.CodeAnalysis;
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
  public string? ResultId { get; }

  [JsonIgnore]
  public object? Result { get; }

  [JsonIgnore]
  public Exception? Error { get; }

  public string? ErrorMessage => Error?.Message;

  [JsonIgnore]
  public Base? Target { get; }

  public string? TargetId => Target?.id;

  public string? TargetType => Target?.speckle_type.Split('.').Last();
  public string? TargetAppId => Target?.applicationId;

  [MemberNotNullWhen(true, nameof(Result))]
  [MemberNotNullWhen(true, nameof(ResultId))]
  [MemberNotNullWhen(false, nameof(Error))]
  public bool IsSuccessful => Result is not null;

  public ReceiveConversionResult() { }

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
