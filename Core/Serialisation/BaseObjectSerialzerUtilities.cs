using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.Core.Serialisation
{
  internal static class SerializationUtilities
  {
    #region Getting Types

    private static Dictionary<string, Type> cachedTypes = new Dictionary<string, Type>();

    internal static Type GetType(string objFullType)
    {
      var objectTypes = objFullType.Split(':').Reverse();

      if (cachedTypes.ContainsKey(objectTypes.First()))
        return cachedTypes[objectTypes.First()];

      foreach (var typeName in objectTypes)
      {
        //TODO: rather than getting the type from the first loaded kit that has it, maybe 
        //we get it from a specific Kit
        var type = KitManager.Types.FirstOrDefault(tp => tp.FullName == typeName);
        if (type != null)
        {
          cachedTypes[typeName] = type;
          return type;
        }
      }

      return typeof(Base);
    }

    /// <summary>
    /// Flushes kit's (discriminator, type) cache. Useful if you're dynamically loading more kits at runtime, that provide better coverage of what you're deserialising, and it's now somehow poisoned because the higher level types were not originally available.
    /// </summary>
    public static void FlushCachedTypes()
    {
      cachedTypes = new Dictionary<string, Type>();
    }

    #endregion

    #region Value handling

    internal static object HandleValue(JToken value, Newtonsoft.Json.JsonSerializer serializer, JsonProperty jsonProperty = null, string TypeDiscriminator = "speckle_type")
    {
      if (value is JValue)
      {
        if (jsonProperty != null) return value.ToObject(jsonProperty.PropertyType);
        else return ((JValue)value).Value;
      }

      if (value is JArray)
      {
        if (jsonProperty != null && jsonProperty.PropertyType.GetConstructor(Type.EmptyTypes) != null)
        {
          var arr = Activator.CreateInstance(jsonProperty.PropertyType);

          var addMethod = arr.GetType().GetMethod("Add");
          var hasGenericType = jsonProperty.PropertyType.GenericTypeArguments.Count() != 0;

          foreach (var val in ((JArray)value))
          {
            if (val == null) continue;
            if (hasGenericType && !jsonProperty.PropertyType.GenericTypeArguments[0].IsInterface)
            {
              addMethod.Invoke(arr, new object[] { Convert.ChangeType(HandleValue(val, serializer), jsonProperty.PropertyType.GenericTypeArguments[0]) });
            }
            else
            {
              addMethod.Invoke(arr, new object[] { HandleValue(val, serializer) });
            }
          }
          return arr;
        }
        else if (jsonProperty != null)
        {
          var arr = Activator.CreateInstance(typeof(List<>).MakeGenericType(jsonProperty.PropertyType.GetElementType()));
          var actualArr = Array.CreateInstance(jsonProperty.PropertyType.GetElementType(), ((JArray)value).Count);

          foreach (var val in ((JArray)value))
          {
            if (val == null) continue;
            if (!jsonProperty.PropertyType.GetElementType().IsInterface)
              ((IList)arr).Add(Convert.ChangeType(HandleValue(val, serializer), jsonProperty.PropertyType.GetElementType()));
            else
              ((IList)arr).Add(HandleValue(val, serializer));
          }

          ((IList)arr).CopyTo(actualArr, 0);
          return actualArr;
        }
        else
        {
          var arr = new List<object>();
          foreach (var val in ((JArray)value))
          {
            if (val == null) continue;
            arr.Add(HandleValue(val, serializer));
          }
          return arr;
        }
      }

      if (value is JObject)
      {
        if (((JObject)value).Property(TypeDiscriminator) != null)
        {
          return value.ToObject<Base>(serializer);
        }

        var dict = jsonProperty != null ? Activator.CreateInstance(jsonProperty.PropertyType) : new Dictionary<string, object>();
        foreach (var prop in ((JObject)value))
        {
          object key = prop.Key;
          if (jsonProperty != null)
            key = Convert.ChangeType(prop.Key, jsonProperty.PropertyType.GetGenericArguments()[0]);
          ((IDictionary)dict)[key] = HandleValue(prop.Value, serializer);
        }
        return dict;
      }
      return null;
    }

    #endregion

    #region Abstract Handling

    private static Dictionary<string, Type> cachedAbstractTypes = new Dictionary<string, Type>();

    internal static object HandleAbstractOriginalValue(JToken jToken, string assemblyQualifiedName, Newtonsoft.Json.JsonSerializer serializer)
    {
      if (cachedAbstractTypes.ContainsKey(assemblyQualifiedName))
        return jToken.ToObject(cachedAbstractTypes[assemblyQualifiedName]);

      var pieces = assemblyQualifiedName.Split(',').Select(s => s.Trim()).ToArray();

      var myAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(ass => ass.GetName().Name == pieces[1]);
      if (myAssembly == null)
        Log.CaptureAndThrow(new SpeckleException("Could not load abstract object's assembly."), level: Sentry.Protocol.SentryLevel.Warning);

      var myType = myAssembly.GetType(pieces[0]);
      if (myType == null)
        Log.CaptureAndThrow(new SpeckleException("Could not load abstract object's assembly."), level: Sentry.Protocol.SentryLevel.Warning);

      cachedAbstractTypes[assemblyQualifiedName] = myType;

      return jToken.ToObject(myType);
    }

    #endregion
  }

  internal static class CallSiteCache
  {
    // Adapted from the answer to 
    // https://stackoverflow.com/questions/12057516/c-sharp-dynamicobject-dynamic-properties
    // by jbtule, https://stackoverflow.com/users/637783/jbtule
    // And also
    // https://github.com/mgravell/fast-member/blob/master/FastMember/CallSiteCache.cs
    // by Marc Gravell, https://github.com/mgravell

    private static readonly Dictionary<string, CallSite<Func<CallSite, object, object, object>>> setters
      = new Dictionary<string, CallSite<Func<CallSite, object, object, object>>>();

    public static void SetValue(string propertyName, object target, object value)
    {
      CallSite<Func<CallSite, object, object, object>> site;

      lock (setters)
      {
        if (!setters.TryGetValue(propertyName, out site))
        {
          var binder = Microsoft.CSharp.RuntimeBinder.Binder.SetMember(CSharpBinderFlags.None,
               propertyName, typeof(CallSiteCache),
               new List<CSharpArgumentInfo>{
                   CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                   CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)});
          setters[propertyName] = site = CallSite<Func<CallSite, object, object, object>>.Create(binder);
        }
      }

      site.Target(site, target, value);
    }
  }
}
