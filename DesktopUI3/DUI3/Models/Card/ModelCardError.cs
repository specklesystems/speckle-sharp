using System;

namespace DUI3.Models.Card;

public class ModelCardError
{
  public string ModelCardId { get; set; }
  public Exception Error { get; set; }
}
