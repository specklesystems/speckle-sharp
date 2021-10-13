using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopUI2.ViewModels.DesignViewModels
{
  public class DesignReportViewModel
  {

    public string ConversionErrorsString { get; set; } = @"Could not convert chair to table
Type 'rocket' is not supported 🚀";

    public List<string> OperationErrors { get; set; } = new List<string>();
    public string OperationErrorsString { get; set; } = @"Something went wrong";
    public string ConversionLogString { get; set; } = @"This is a sample log
Some elements were created
Some other elements were not created
Bye Bye!";
  }
}
