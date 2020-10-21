using Autodesk.DesignScript.Runtime;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speckle.ConnectorDynamo.Functions
{
  public class Convert
  {
    public Base ToSpeckle([ArbitraryDimensionArrayImport] object data)
    {
      var converter = new BatchConverter();
      return converter.ConvertRecursivelyToSpeckle(data);
    }
  }
}
