using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace Speckle.Core.Models;

/// <summary>
/// Base class implementing a bunch of nice dynamic object methods, like adding and removing props dynamically. Makes c# feel like json.
/// <para>Orginally adapted from Rick Strahl 🤘</para>
/// <para>https://weblog.west-wind.com/posts/2012/feb/08/creating-a-dynamic-extensible-c-expando-object</para>
/// </summary>
public class DynamicBase : DynamicObject, IDynamicMetaObjectProvider
{
  /// <summary>
  /// Default <see cref="DynamicBaseMemberType"/> value for <see cref="GetMembers"/>
  /// </summary>
  public const DynamicBaseMemberType DefaultIncludeMembers =
    DynamicBaseMemberType.Instance | DynamicBaseMemberType.Dynamic;

  private static Dictionary<Type, List<PropertyInfo>> propInfoCache = new();

  /// <summary>
  /// The actual property bag, where dynamically added props are stored.
  /// </summary>
  private Dictionary<string, object> properties = new();

  /// <summary>
  /// Sets and gets properties using the key accessor pattern. E.g.:
  /// <para><pre>((dynamic)myObject)["superProperty"] = 42;</pre></para>
  /// </summary>
  /// <param name="key"></param>
  /// <returns></returns>
  [IgnoreTheItem]
  public object this[string key]
  {
    get
    {
      if (properties.ContainsKey(key))
        return properties[key];

      PopulatePropInfoCache(GetType());
      var prop = propInfoCache[GetType()].FirstOrDefault(p => p.Name == key);

      if (prop == null)
        return null;

      return prop.GetValue(this);
    }
    set
    {
      if (!IsPropNameValid(key, out string reason))
        throw new InvalidPropNameException(key, reason);

      if (properties.ContainsKey(key))
      {
        properties[key] = value;
        return;
      }

      PopulatePropInfoCache(GetType());
      var prop = propInfoCache[GetType()].FirstOrDefault(p => p.Name == key);

      if (prop == null)
      {
        properties[key] = value;
        return;
      }
      try
      {
        prop.SetValue(this, value);
      }
      catch (Exception ex)
      {
        throw new SpeckleException($"Failed to set value for {GetType().Name}.{prop.Name}", ex);
      }
    }
  }

  /// <summary>
  /// Gets properties via the dot syntax.
  /// <para><pre>((dynamic)myObject).superProperty;</pre></para>
  /// </summary>
  /// <param name="binder"></param>
  /// <param name="result"></param>
  /// <returns></returns>
  public override bool TryGetMember(GetMemberBinder binder, out object result)
  {
    return properties.TryGetValue(binder.Name, out result);
  }

  /// <summary>
  /// Sets properties via the dot syntax.
  /// <para><pre>((dynamic)myObject).superProperty = something;</pre></para>
  /// </summary>
  /// <param name="binder"></param>
  /// <param name="value"></param>
  /// <returns></returns>
  public override bool TrySetMember(SetMemberBinder binder, object value)
  {
    var valid = IsPropNameValid(binder.Name, out _);
    if (valid)
      properties[binder.Name] = value;
    return valid;
  }

  private static readonly HashSet<char> DisallowedPropNameChars = new() { '.', '/' };

  public bool IsPropNameValid(string name, out string reason)
  {
    if (string.IsNullOrEmpty(name) || name == "@")
    {
      reason = "Found empty prop name";
      return false;
    }

    if (name.StartsWith("@@"))
    {
      reason = "Only one leading '@' char is allowed. This signals the property value should be detached.";
      return false;
    }

    foreach (char c in name)
    {
      if (DisallowedPropNameChars.Contains(c))
      {
        reason = $"Prop with name '{name}' contains invalid characters. The following characters are not allowed: ./";
        return false;
      }
    }

    reason = "";
    return true;
  }

  private static void PopulatePropInfoCache(Type type)
  {
    if (!propInfoCache.ContainsKey(type))
      propInfoCache[type] = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(p => !p.IsDefined(typeof(IgnoreTheItemAttribute), true))
        .ToList();
  }

  /// <summary>
  /// Gets all of the property names on this class, dynamic or not.
  /// </summary> <returns></returns>
  [Obsolete("Use `GetMembers(DynamicBaseMemberType.All).Keys` instead")]
  public override IEnumerable<string> GetDynamicMemberNames()
  {
    PopulatePropInfoCache(GetType());
    var pinfos = propInfoCache[GetType()];

    var names = new List<string>(properties.Count + pinfos.Count);
    foreach (var pinfo in pinfos)
      names.Add(pinfo.Name);
    foreach (var kvp in properties)
      names.Add(kvp.Key);

    return names;
  }

  /// <summary>
  /// Gets the names of the defined class properties (typed).
  /// </summary>
  /// <returns></returns>
  [Obsolete("Use GetMembers(DynamicBaseMemberType.InstanceAll).Keys instead")]
  public IEnumerable<string> GetInstanceMembersNames()
  {
    return GetInstanceMembersNames(GetType());
  }

  public static IEnumerable<string> GetInstanceMembersNames(Type t)
  {
    PopulatePropInfoCache(t);
    var pinfos = propInfoCache[t];

    var names = new List<string>(pinfos.Count);
    foreach (var pinfo in pinfos)
      names.Add(pinfo.Name);

    return names;
  }

  /// <summary>
  /// Gets the defined (typed) properties of this object.
  /// </summary>
  /// <returns></returns>
  public IEnumerable<PropertyInfo> GetInstanceMembers()
  {
    return GetInstanceMembers(GetType());
  }

  public static IEnumerable<PropertyInfo> GetInstanceMembers(Type t)
  {
    PopulatePropInfoCache(t);
    var pinfos = propInfoCache[t];

    var names = new List<PropertyInfo>(pinfos.Count);

    foreach (var pinfo in pinfos)
      if (pinfo.Name != "Item")
        names.Add(pinfo);

    return names;
  }

  /// <summary>
  /// Gets the names of the typed and dynamic properties that don't have a [SchemaIgnore] attribute.
  /// </summary>
  /// <returns></returns>
  [Obsolete("Use GetMembers().Keys instead")]
  public IEnumerable<string> GetMemberNames()
  {
    return GetMembers().Keys;
  }

  /// <summary>
  ///  Gets the typed and dynamic properties.
  /// </summary>
  /// <param name="includeMembers">Specifies which members should be included in the resulting dictionary. Can be concatenated with "|"</param>
  /// <returns>A dictionary containing the key's and values of the object.</returns>
  public Dictionary<string, object> GetMembers(DynamicBaseMemberType includeMembers = DefaultIncludeMembers)
  {
    // Initialize an empty dict
    var dic = new Dictionary<string, object>();

    // Add dynamic members
    if (includeMembers.HasFlag(DynamicBaseMemberType.Dynamic))
      dic = new Dictionary<string, object>(properties);

    if (includeMembers.HasFlag(DynamicBaseMemberType.Instance))
    {
      PopulatePropInfoCache(GetType());
      var pinfos = propInfoCache[GetType()].Where(x =>
      {
        var hasIgnored = x.IsDefined(typeof(SchemaIgnore), true);
        var hasObsolete = x.IsDefined(typeof(ObsoleteAttribute), true);

        // If obsolete is false and prop has obsolete attr
        // OR
        // If schemaIgnored is true and prop has schemaIgnore attr
        return !(
          !includeMembers.HasFlag(DynamicBaseMemberType.SchemaIgnored) && hasIgnored
          || !includeMembers.HasFlag(DynamicBaseMemberType.Obsolete) && hasObsolete
        );
      });
      foreach (var pi in pinfos)
        if (!dic.ContainsKey(pi.Name)) //todo This is a TEMP FIX FOR #1969, and should be reverted after a proper fix is made!
          dic.Add(pi.Name, pi.GetValue(this));
    }

    if (includeMembers.HasFlag(DynamicBaseMemberType.SchemaComputed))
      GetType()
        .GetMethods()
        .Where(e => e.IsDefined(typeof(SchemaComputedAttribute)) && !e.IsDefined(typeof(ObsoleteAttribute)))
        .ToList()
        .ForEach(e =>
        {
          var attr = e.GetCustomAttribute<SchemaComputedAttribute>();
          try
          {
            dic[attr.Name] = e.Invoke(this, null);
          }
          catch (Exception ex)
          {
            SpeckleLog.Logger.Warning(ex, "Failed to get computed member: {name}", attr.Name);
            dic[attr.Name] = null;
          }
        });

    return dic;
  }

  /// <summary>
  /// Gets the dynamically added property names only.
  /// </summary>
  /// <returns></returns>
  [Obsolete("Use GetMembers(DynamicBaseMemberType.Dynamic).Keys instead")]
  public IEnumerable<string> GetDynamicMembers()
  {
    return properties.Keys;
  }
}

/// <summary>
/// This attribute is used internally to hide the this[key]{get; set;} property from inner reflection on members.
/// For more info see this discussion: https://speckle.community/t/why-do-i-keep-forgetting-base-objects-cant-use-item-as-a-dynamic-member/3246/5
/// </summary>
internal sealed class IgnoreTheItemAttribute : Attribute { }
