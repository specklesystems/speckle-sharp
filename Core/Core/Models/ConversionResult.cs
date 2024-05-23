using System;

namespace Speckle.Core.Models;

public sealed class ConversionResult
{
  public string? ResultId { get; }
  public object? Result { get; }
  public Exception? Error { get; }
  public object Target { get; }

  //[MemberNotNullWhen(true, nameof(Result))]
  //[MemberNotNullWhen(true, nameof(ResultId))]
  //[MemberNotNullWhen(false, nameof(Error))]
  public bool IsSuccessful => Result is not null;

  public ConversionResult(object target, object result, string resultId)
  {
    Target = target;
    Result = result;
    ResultId = resultId;
  }

  public ConversionResult(object target, Exception error)
  {
    Target = target;
    Error = error;
  }

  public override string ToString() =>
    IsSuccessful
      ? $"Successfully converted {Target} to {Result}"
      : $"Failed to convert {Target}: {Error!.GetType()}: {Error!.Message}";
}
