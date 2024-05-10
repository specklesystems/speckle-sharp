namespace Speckle.Connectors.DUI.Models.Card.SendFilter;

public interface ISendFilter
{
  public string Name { get; set; }
  public string Summary { get; set; }
  public bool IsDefault { get; set; }

  /// <summary>
  /// Gets the ids of the objects targeted by the filter from the host application.
  /// </summary>
  /// <returns></returns>
  public List<string> GetObjectIds();

  /// <summary>
  /// Checks whether any of the targeted objects are affected by changes from the host application.
  /// </summary>
  /// <param name="changedObjectIds"></param>
  /// <returns></returns>
  public bool CheckExpiry(string[] changedObjectIds);
}
