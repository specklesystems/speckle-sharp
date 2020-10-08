using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Core.Models
{
  public class Error
  {
    public string message { get; set; }
    public string details { get; set; }

    public Error()
    {

    }
    public Error(string message, string details)
    {
      this.message = message;
      this.details = details;
    }

  
  }
}
