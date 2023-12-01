namespace DUI3.Models.Card;

public class ModelCardNotification
{
  public string Id { get; set; }
  public string ModelCardId { get; set; }
  public string Text { get; set; }
  public string Level { get; set; }
  public ModelCardNotificationAction Action { get; set; }
  public int Timeout { get; set; }
  public bool Visible { get; set; }
}

public class ModelCardNotificationAction
{
  public string Url { get; set; }
  public string Name { get; set; }
}
