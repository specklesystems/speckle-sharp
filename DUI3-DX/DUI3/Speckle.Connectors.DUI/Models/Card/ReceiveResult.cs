using Speckle.Core.Models;

namespace Speckle.Connectors.DUI.Models.Card;

public record ReceiveResult(IReadOnlyList<ReceiveConversionResult> Results, bool Display)
{
  public List<string> GetSuccessfulResultIds()
  {
    return Results.Where(x => x.IsSuccessful).Select(x => x.ResultId!).ToList();
  }
}
