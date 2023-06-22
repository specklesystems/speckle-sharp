using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.CSharp.RuntimeBinder;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Linq;
using Speckle.Newtonsoft.Json.Serialization;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace Speckle.Core.Serialisation;

internal static class SerializationUtilities
{
  #region Value handling

  internal static object HandleValue(
    JToken value,
    JsonSerializer serializer,
    CancellationToken CancellationToken,
    JsonProperty jsonProperty = null,
    string TypeDiscriminator = "speckle_type"
  )
  {
    if (CancellationToken.IsCancellationRequested)
      return null; // Check for cancellation

    if (value is JValue)
    {
      if (jsonProperty != null)
        return value.ToObject(jsonProperty.PropertyType);
      return ((JValue)value).Value;
    }

    // Lists
    if (value is JArray)
    {
      if (CancellationToken.IsCancellationRequested)
        return null; // Check for cancellation

      if (jsonProperty != null && jsonProperty.PropertyType.GetConstructor(Type.EmptyTypes) != null)
      {
        var arr = Activator.CreateInstance(jsonProperty.PropertyType);

        var addMethod = arr.GetType().GetMethod("Add");
        var hasGenericType = jsonProperty.PropertyType.GenericTypeArguments.Count() != 0;

        foreach (var val in (JArray)value)
        {
          if (CancellationToken.IsCancellationRequested)
            return null; // Check for cancellation

          if (val == null)
            continue;

          var item = HandleValue(val, serializer, CancellationToken);

          if (item is DataChunk chunk)
          {
            foreach (var dataItem in chunk.data)
              if (hasGenericType && !jsonProperty.PropertyType.GenericTypeArguments[0].IsInterface)
              {
                if (jsonProperty.PropertyType.GenericTypeArguments[0].IsAssignableFrom(dataItem.GetType()))
                  addMethod.Invoke(arr, new[] { dataItem });
                else
                  addMethod.Invoke(
                    arr,
                    new[] { Convert.ChangeType(dataItem, jsonProperty.PropertyType.GenericTypeArguments[0]) }
                  );
              }
              else
              {
                addMethod.Invoke(arr, new[] { dataItem });
              }
          }
          else if (hasGenericType && !jsonProperty.PropertyType.GenericTypeArguments[0].IsInterface)
          {
            if (jsonProperty.PropertyType.GenericTypeArguments[0].IsAssignableFrom(item.GetType()))
              addMethod.Invoke(arr, new[] { item });
            else
              addMethod.Invoke(
                arr,
                new[] { Convert.ChangeType(item, jsonProperty.PropertyType.GenericTypeArguments[0]) }
              );
          }
          else
          {
            addMethod.Invoke(arr, new[] { item });
          }
        }
        return arr;
      }

      if (jsonProperty != null)
      {
        if (CancellationToken.IsCancellationRequested)
          return null; // Check for cancellation

        var arr = Activator.CreateInstance(typeof(List<>).MakeGenericType(jsonProperty.PropertyType.GetElementType()));

        foreach (var val in (JArray)value)
        {
          if (CancellationToken.IsCancellationRequested)
            return null; // Check for cancellation

          if (val == null)
            continue;

          var item = HandleValue(val, serializer, CancellationToken);
          if (item is DataChunk chunk)
          {
            foreach (var dataItem in chunk.data)
              if (!jsonProperty.PropertyType.GetElementType().IsInterface)
                ((IList)arr).Add(Convert.ChangeType(dataItem, jsonProperty.PropertyType.GetElementType()));
              else
                ((IList)arr).Add(dataItem);
          }
          else
          {
            if (!jsonProperty.PropertyType.GetElementType().IsInterface)
              ((IList)arr).Add(Convert.ChangeType(item, jsonProperty.PropertyType.GetElementType()));
            else
              ((IList)arr).Add(item);
          }
        }
        var actualArr = Array.CreateInstance(jsonProperty.PropertyType.GetElementType(), ((IList)arr).Count);
        ((IList)arr).CopyTo(actualArr, 0);
        return actualArr;
      }
      else
      {
        if (CancellationToken.IsCancellationRequested)
          return null; // Check for cancellation

        var arr = new List<object>();
        foreach (var val in (JArray)value)
        {
          if (CancellationToken.IsCancellationRequested)
            return null; // Check for cancellation

          if (val == null)
            continue;

          var item = HandleValue(val, serializer, CancellationToken);

          if (item is DataChunk chunk)
            arr.AddRange(chunk.data);
          else
            arr.Add(item);
        }
        return arr;
      }
    }

    if (CancellationToken.IsCancellationRequested)
      return null; // Check for cancellation

    if (value is JObject)
    {
      if (((JObject)value).Property(TypeDiscriminator) != null)
        return value.ToObject<Base>(serializer);

      var dict =
        jsonProperty != null ? Activator.CreateInstance(jsonProperty.PropertyType) : new Dictionary<string, object>();
      foreach (var prop in (JObject)value)
      {
        if (CancellationToken.IsCancellationRequested)
          return null; // Check for cancellation

        object key = prop.Key;
        if (jsonProperty != null)
          key = Convert.ChangeType(prop.Key, jsonProperty.PropertyType.GetGenericArguments()[0]);
        ((IDictionary)dict)[key] = HandleValue(prop.Value, serializer, CancellationToken);
      }
      return dict;
    }
    return null;
  }

  #endregion

  #region Getting Types

  private static Dictionary<string, Type> cachedTypes = new();

  private static Dictionary<string, Dictionary<string, PropertyInfo>> typeProperties = new();

  private static Dictionary<string, List<MethodInfo>> onDeserializedCallbacks = new();

  internal static Type GetType(string objFullType)
  {
    lock (cachedTypes)
    {
      if (cachedTypes.ContainsKey(objFullType))
        return cachedTypes[objFullType];

      var type = GetAtomicType(objFullType);
      cachedTypes[objFullType] = type;
      return type;
    }
  }

  internal static Type GetAtomicType(string objFullType)
  {
    var objectTypes = objFullType.Split(':').Reverse();
    foreach (var typeName in objectTypes)
    {
      //TODO: rather than getting the type from the first loaded kit that has it, maybe
      //we get it from a specific Kit
      var type = KitManager.Types.FirstOrDefault(tp => tp.FullName == typeName);
      if (type != null)
        return type;

      //To allow for backwards compatibility saving deserialization target types.
      //We also check a ".Deprecated" prefixed namespace
      string deprecatedTypeName = GetDeprecatedTypeName(typeName);

      var deprecatedType = KitManager.Types.FirstOrDefault(tp => tp.FullName == deprecatedTypeName);
      if (deprecatedType != null)
        return deprecatedType;
    }

    return typeof(Base);
  }

  internal static string GetDeprecatedTypeName(string typeName, string deprecatedSubstring = "Deprecated.")
  {
    int lastDotIndex = typeName.LastIndexOf('.');
    return typeName.Insert(lastDotIndex + 1, deprecatedSubstring);
  }

  internal static Dictionary<string, PropertyInfo> GetTypePropeties(string objFullType)
  {
    lock (typeProperties)
    {
      if (!typeProperties.ContainsKey(objFullType))
      {
        Dictionary<string, PropertyInfo> ret = new();
        Type type = GetType(objFullType);
        PropertyInfo[] properties = type.GetProperties();
        foreach (PropertyInfo prop in properties)
          ret[prop.Name.ToLower()] = prop;
        typeProperties[objFullType] = ret;
      }
      return typeProperties[objFullType];
    }
  }

  internal static List<MethodInfo> GetOnDeserializedCallbacks(string objFullType)
  {
    // return new List<MethodInfo>();
    lock (onDeserializedCallbacks)
    {
      // System.Runtime.Serialization.Ca
      if (!onDeserializedCallbacks.ContainsKey(objFullType))
      {
        List<MethodInfo> ret = new();
        Type type = GetType(objFullType);
        MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (MethodInfo method in methods)
        {
          List<OnDeserializedAttribute> onDeserializedAttributes = method
            .GetCustomAttributes<OnDeserializedAttribute>(true)
            .ToList();
          if (onDeserializedAttributes.Count > 0)
            ret.Add(method);
        }
        onDeserializedCallbacks[objFullType] = ret;
      }
      return onDeserializedCallbacks[objFullType];
    }
  }

  internal static Type GetSytemOrSpeckleType(string typeName)
  {
    var systemType = Type.GetType(typeName);
    if (systemType != null)
      return systemType;
    return GetAtomicType(typeName);
  }

  /// <summary>
  /// Flushes kit's (discriminator, type) cache. Useful if you're dynamically loading more kits at runtime, that provide better coverage of what you're deserialising, and it's now somehow poisoned because the higher level types were not originally available.
  /// </summary>
  public static void FlushCachedTypes()
  {
    cachedTypes = new Dictionary<string, Type>();
  }

  #endregion

  #region Abstract Handling

  private static Dictionary<string, Type> cachedAbstractTypes = new();

  internal static object HandleAbstractOriginalValue(
    JToken jToken,
    string assemblyQualifiedName,
    JsonSerializer serializer
  )
  {
    if (cachedAbstractTypes.ContainsKey(assemblyQualifiedName))
      return jToken.ToObject(cachedAbstractTypes[assemblyQualifiedName]);

    var pieces = assemblyQualifiedName.Split(',').Select(s => s.Trim()).ToArray();

    var myAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(ass => ass.GetName().Name == pieces[1]);
    if (myAssembly == null)
      throw new SpeckleException("Could not load abstract object's assembly.");

    var myType = myAssembly.GetType(pieces[0]);
    if (myType == null)
      throw new SpeckleException("Could not load abstract object's assembly.");

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

  private static readonly Dictionary<string, CallSite<Func<CallSite, object, object, object>>> setters = new();

  public static void SetValue(string propertyName, object target, object value)
  {
    lock (setters)
    {
      CallSite<Func<CallSite, object, object, object>> site;

      lock (setters)
        if (!setters.TryGetValue(propertyName, out site))
        {
          var binder = Binder.SetMember(
            CSharpBinderFlags.None,
            propertyName,
            typeof(CallSiteCache),
            new List<CSharpArgumentInfo>
            {
              CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
              CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
            }
          );
          setters[propertyName] = site = CallSite<Func<CallSite, object, object, object>>.Create(binder);
        }

      site.Target(site, target, value);
    }
  }
}
