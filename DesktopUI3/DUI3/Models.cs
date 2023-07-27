using System;
using System.Collections.Generic;
using System.Text;

namespace DUI3
{
  public class DocumentInfo
  {
    public string Location { get; set; }
    public string Name { get; set; }

    public string Id { get; set; }
  }

  public class DocumentState
  {
    public List<ModelCard> Models { get; set; } = new List<ModelCard>();
  }

  public class ModelCard // CARD
  {
    public string Id { get; set; }
    public string ModelId { get; set; }
    public string ProjectId { get; set; }
    public string ServerUrl { get; set; }
    public string LastLocalUpdate { get; set; }
  }
  
  public class Sender: ModelCard
  {
    public string Send()
    {
      // var converted = 
      // ParentBinding.Parent.SendToBrowser(event);
      return "YOLO";
    }
  }

  public class Receiver: ModelCard
  {
    // TODO 
  }
}
