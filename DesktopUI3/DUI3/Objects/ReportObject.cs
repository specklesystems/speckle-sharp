namespace DUI3.Objects;

public enum ConversionResult
{
  Success,
  Failed
}

// Information the UI needs to report objects
public class ReportObject<T>
{
  public SpeckleHostObject<T> SpeckleHostObject { get; }
  public ConversionResult ConversionResult { get; }
}
