namespace Speckle.Connectors.DUI.Models.Card;

public class ModelCardProgress
{
  public string ModelCardId { get; set; }
  public string Status { get; set; }

  /// <summary>
  /// Progress value between 0 and 1 to calculate UI progress bar width.
  /// If it is null it will swooshing on UI.
  /// </summary>
  public double? Progress { get; set; }
}
