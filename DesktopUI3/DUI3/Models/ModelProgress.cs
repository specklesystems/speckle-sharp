namespace DUI3.Models;

public class ModelProgress
{
  public string Id { get; set; }
  public string Status { get; set; }
  
  /// <summary>
  /// Progress value between 0 and 1 to calculate UI progress bar width.
  /// If it is null it will swooshing on UI.
  /// </summary>
  public double? Progress { get; set; }
}
