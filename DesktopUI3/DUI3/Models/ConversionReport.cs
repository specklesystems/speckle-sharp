using System.Collections.Generic;

namespace DUI3.Models;

public class ConversionReport
{
  /// <summary>
  /// An overall summary.
  /// </summary>
  public string Summary { get; set; }

  /// <summary>
  /// Specific reports for sets of objects (e.g., failed conversions, conversion fallbacks, etc.
  /// </summary>
  public List<ReportItem> Items { get; set; }

  public (ReportItem success, ReportItem warning, ReportItem danger) InitializeSuccessWarningDangerReport()
  {
    var success = new ReportItem() { Level = NotificationLevel.Success, Message = "Successful conversions" };
    var warning = new ReportItem() { Level = NotificationLevel.Warning, Message = "Partially successful conversions" };
    var danger = new ReportItem() { Level = NotificationLevel.Danger, Message = "Failed conversions" };

    Items.Add(success);
    Items.Add(warning);
    Items.Add(danger);
    return (success, warning, danger);
  }
}

public class ReportItem
{
  /// <summary>
  /// A short message.
  /// </summary>
  public string Message { get; set; }

  /// <summary>
  /// Warning, Info, Success, Danger - etc. Use the NotificationLevel class.
  /// </summary>
  public string Level { get; set; }

  /// <summary>
  /// The affected objects.
  /// </summary>
  public List<string> ObjectIds { get; set; } = new();
}
