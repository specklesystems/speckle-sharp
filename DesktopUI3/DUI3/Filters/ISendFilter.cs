using System.Collections.Generic;

namespace DUI3.Filters;

public interface ISendFilter
{
  public string Name { get; set; }
  public string Summary { get; set; } 
  public List<string> GetObjectIds();
  public ExpiredStatus GetExpiryStatus(string[] changedObjectIds);
}

public interface ISelectionFilter : ISendFilter
{
  public List<string> ObjectIds { get; set; }
}

public interface ICategoryFilter : ISendFilter
{
  // TODO
}

public class ExpiredStatus
{
  public bool IsExpired { get; set; }
  public string Summary { get; set; }
}
