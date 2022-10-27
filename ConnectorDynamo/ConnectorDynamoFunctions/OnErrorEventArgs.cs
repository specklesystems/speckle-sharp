using System;
using Autodesk.DesignScript.Runtime;

namespace Speckle.ConnectorDynamo
{
  [IsVisibleInDynamoLibrary(false)]
  public class OnErrorEventArgs
  {
    public Exception Error;

    public OnErrorEventArgs(Exception error)
    {
      Error = error;
    }
  }
}
