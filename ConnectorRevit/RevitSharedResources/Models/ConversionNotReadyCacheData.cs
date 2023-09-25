using System;
using System.Collections.Generic;
using System.Text;

namespace RevitSharedResources.Models
{
  public struct ConversionNotReadyCacheData
  {
    public ConversionNotReadyCacheData(int numTimesCaught) 
    { 
      NumberOfTimesCaught = numTimesCaught;
    }
    public int NumberOfTimesCaught;
  }
}
