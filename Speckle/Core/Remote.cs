using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Speckle.Models;
using Speckle.Serialisation;
using Speckle.Transports;

namespace Speckle.Core
{

  public class Remote
  {
    public string ServerUrl { get; set; }

    public string StreamId { get; set; }

    public string Email { get; set; }

    public string ApiToken { get; set; }

    public Remote() { }
  }

}
