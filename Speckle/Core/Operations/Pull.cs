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

namespace Speckle.Core
{
  public static partial class Operations
  {
    #region Pulling objects

    public static async Task<Base> Pull(string objectId, SqlLiteObjectTransport localTransport = null, Remote remote = null)
    {
      var (serializer, settings) = GetSerializerInstance();
      localTransport = localTransport != null ? localTransport : new SqlLiteObjectTransport();
      serializer.Transport = localTransport;

      try
      {
        // if the object is not present in the local transport, it means we need to get it (and its children!) from the remote. 
        var objString = localTransport.GetObject(objectId);
        return JsonConvert.DeserializeObject<Base>(objString, settings);
      }
      catch (Exception err)
      {

        var rem = new RemoteTransport("http://localhost:3000", "lol", "lol", 1000);
        rem.LocalTransport = localTransport;
        var res = await rem.GetObjectAndChildren(objectId);
        return JsonConvert.DeserializeObject<Base>(res, settings);
      }
    }

    #endregion
  }
}