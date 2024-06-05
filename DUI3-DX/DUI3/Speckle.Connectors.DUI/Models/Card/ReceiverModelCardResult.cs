namespace Speckle.Connectors.DUI.Models.Card;

public class ReceiverModelCardResult
{
  public string? ModelCardId { get; set; }
  public List<string> BakedObjectIds { get; set; } = new();
}
