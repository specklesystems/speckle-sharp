using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Speckle.Models;
using Speckle.Serialisation;
using Speckle.Transports;
using Speckle.Core;

namespace Speckle.Core
{
  public static partial class Operations
  {

    public static async Task<Base> Pull(string objectId, ITransport localTransport = null, Remote remote = null)
    {
      var (serializer, settings) = GetSerializerInstance();
      localTransport = localTransport != null ? localTransport : new SqlLiteObjectTransport();
      serializer.Transport = localTransport;

      var objString = localTransport.GetObject(objectId);

      if (objString == null)
      {
        var rem = new RemoteTransport("http://localhost:3000", "lol", "lol", 1000);
        rem.LocalTransport = localTransport;
        var res = await rem.GetObjectAndChildren(objectId);
        await localTransport.WriteComplete();
        return JsonConvert.DeserializeObject<Base>(res, settings);
      }

      return JsonConvert.DeserializeObject<Base>(objString, settings);
    }

  }
}