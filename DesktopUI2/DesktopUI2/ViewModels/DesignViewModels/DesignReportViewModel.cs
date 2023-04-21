namespace DesktopUI2.ViewModels.DesignViewModels;

public class DesignReportViewModel
{
  public DesignReport Report { get; set; } = new();

  public class DesignReport
  {
    public string ConversionErrorsString { get; set; } =
      @"Could not convert chair to table
Test
Test
Test
Test
Test
Test
Test
Test
Test
Test
Test
Test
Test
Test
Test
Test
Test
Test
Test
Test
Test
Test
Test
Test
Test
Type 'rocket' is not supported 🚀";

    public int OperationErrorsCount { get; set; }

    public int ConversionErrorsCount { get; set; }
    public string OperationErrorsString { get; set; } = @"Something went wrong";

    public string ConversionLogString { get; set; } =
      @"This is a sample log
Some elements were created
Some other elements were not created
Some other elements were updated
Bye Bye!";
  }
}
