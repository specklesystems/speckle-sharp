using Speckle.Connectors.Utils;

namespace Speckle.Connectors.DUI.Models.Card;

public record ReceiveResult(bool Display, IReadOnlyList<ReceiveConversionResult> ReceiveConversionResults)
{
  public List<string> GetSuccessfulResultIds() =>
    ReceiveConversionResults.Where(x => x.IsSuccessful).Select(x => x.ResultId!).ToList();
}
