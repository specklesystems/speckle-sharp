using System.Diagnostics.CodeAnalysis;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Speckle.Connectors.Utils;

public record SendOperationResult(
  SendConversionResults ConversionResults,
  string RootObjId,
  IReadOnlyDictionary<string, ObjectReference> ConvertedReferences
);

public record SendConversionResults(IReadOnlyList<SendConversionResult> Results, Base Root)
{
  [JsonIgnore]
  public IEnumerable<SendConversionResult> SuccessfulResults => Results.Where(x => x.IsSuccessful);

  [JsonIgnore]
  public IEnumerable<SendConversionResult> FailedResults => Results.Where(x => !x.IsSuccessful);
}

public sealed class SendConversionResult
{
  public string? ResultId => Result?.id;
  public string? ResultAppId => Result?.applicationId;

  [JsonIgnore]
  public Base? Result { get; }
  public Exception? Error { get; }

  [JsonIgnore]
  public object Target { get; }

  public string TargetId { get; }

  [MemberNotNullWhen(true, nameof(Result))]
  [MemberNotNullWhen(true, nameof(ResultId))]
  [MemberNotNullWhen(false, nameof(Error))]
  public bool IsSuccessful => Result is not null;

  public SendConversionResult(object target, string targetId, Base result)
  {
    Target = target;
    TargetId = targetId;
    Result = result;
  }

  public SendConversionResult(object target, string targetId, Exception error)
  {
    Target = target;
    TargetId = targetId;
    Error = error;
  }

  public override string ToString() =>
    IsSuccessful
      ? $"Successfully converted {Target} to {Result}"
      : $"Failed to convert {Target}: {Error!.GetType()}: {Error!.Message}";
}
