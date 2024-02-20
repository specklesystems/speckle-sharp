namespace DUI3.Models.Card;

public class ModelCardNotification
{
  public string ModelCardId { get; set; }
  public string Text { get; set; }
  public string Level { get; set; }
  public int Timeout { get; set; }
  public bool Dismissible { get; set; } = true;
}
