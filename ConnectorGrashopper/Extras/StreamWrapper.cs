using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConnectorGrashopper.Extras
{
  public class StreamWrapper
  {
    public string StreamId { get; set; }
    public string AccountId { get; set; }
    public string ServerUrl { get; set; }

    public override string ToString()
    {
      return $"Id: {StreamId} @ {ServerUrl}";
    }
  }
}
