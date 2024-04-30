namespace Speckle.Connectors.DUI.Models.Card;

public class ReceiverModelCard : ModelCard
{
  public string ProjectName { get; set; }
  public string ModelName { get; set; }
  public string SelectedVersionId { get; set; }
  public string LatestVersionId { get; set; }
  public bool HasDismissedUpdateWarning { get; set; }
}
