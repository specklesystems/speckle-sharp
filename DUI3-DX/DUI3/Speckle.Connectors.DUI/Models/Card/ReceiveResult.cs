using Speckle.Core.Models;

namespace Speckle.Connectors.DUI.Models.Card;

public record ReceiveResult(IReadOnlyList<ReceiveConversionResult> Results, bool Display);
