using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace Speckle.Core.Models
{

  /// <summary>
  /// Base class implementing a bunch of nice dynamic object methods, like adding and removing props dynamically. Makes c# feel like json.
  /// <para>Orginally adapted from Rick Strahl 🤘</para>
  /// <para>https://weblog.west-wind.com/posts/2012/feb/08/creating-a-dynamic-extensible-c-expando-object</para>
  /// </summary>
  public class DynamicBase : DynamicObject, IDynamicMetaObjectProvider
  {
    /// <summary>
    /// The actual property bag, where dynamically added props are stored.
    /// </summary>
    private Dictionary<string, object> properties = new Dictionary<string, object>();

    public DynamicBase()
    {

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
      return (properties.TryGetValue(binder.Name, out result));
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

    public bool IsPropNameValid(string name, out string reason)
    {
      // Regex rules
      // Rule for multiple leading @.
      var manyLeadingAtChars = new Regex(@"^@{2,}");
      // Rule for invalid chars.
      var invalidChars = new Regex(@"[\.\/]");
      // Existing members
      var members = GetInstanceMembersNames();

      // TODO: Check for detached/non-detached duplicate names? i.e: '@something' vs 'something'
      // TODO: Instance members will not be overwritten, this may cause issues.
      var checks = new List<(bool, string)>
      {
        (!(string.IsNullOrEmpty(name) || name == "@"), "Found empty prop name"),
        // Checks for multiple leading @
        (!manyLeadingAtChars.IsMatch(name), "Only one leading '@' char is allowed. This signals the property value should be detached."),
        // Checks for invalid chars
        (!invalidChars.IsMatch(name), $"Prop with name '{name}' contains invalid characters. The following characters are not allowed: ./"),
        // Checks if you are trying to change a member property
        //(!members.Contains(name), "Modifying the value of instance member properties is not allowed.")
      };

      var r = "";
      // Prop name is valid if none of the checks are true
      var isValid = checks.TrueForAll(v =>
      {
        if (!v.Item1) r = v.Item2;
        return v.Item1;
      });

      reason = r;
      return isValid;
    }

    /// <summary>
    /// Sets and gets properties using the key accessor pattern. E.g.:
    /// <para><pre>((dynamic)myObject)["superProperty"] = 42;</pre></para>
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public object this[string key]
    {
      get
      {
        if (properties.ContainsKey(key))
          return properties[key];

        var prop = GetType().GetProperty(key);

        if (prop == null)
          return null;

        return prop.GetValue(this);
      }
      set
      {
        if (!IsPropNameValid(key, out string reason)) throw new SpeckleException("Invalid prop name: " + reason);

        if (properties.ContainsKey(key))
        {
          properties[key] = value;
          return;
        }

        var prop = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault(p => p.Name == key);

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
          throw new SpeckleException(ex.Message, ex);
        }
      }
    }

    /// <summary>
    /// Gets all of the property names on this class, dynamic or not.
    /// </summary>
    /// <returns></returns>
    public override IEnumerable<string> GetDynamicMemberNames()
    {
      var names = new List<string>();
      foreach (var kvp in properties) names.Add(kvp.Key);

      var pinfos = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
      foreach (var pinfo in pinfos) names.Add(pinfo.Name);

      names.Remove("Item"); // TODO: investigate why we get Item out?
      return names;
    }

    /// <summary>
    /// Gets the names of the defined class properties (typed).
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetInstanceMembersNames()
    {
      var names = new List<string>();
      var pinfos = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
      foreach (var pinfo in pinfos) names.Add(pinfo.Name);

      names.Remove("Item"); // TODO: investigate why we get Item out?
      return names;
    }

    /// <summary>
    /// Gets the defined (typed) properties of this object.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<PropertyInfo> GetInstanceMembers()
    {
      var names = new List<PropertyInfo>();
      var pinfos = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

      foreach (var pinfo in pinfos)
        if (pinfo.Name != "Item") names.Add(pinfo);

      return names;
    }

    /// <summary>
    /// Gets the names of the typed and dynamic properties that don't have a [SchemaIgnore] attribute.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetMemberNames()
    {
      var names = new List<string>();
      foreach (var kvp in properties) names.Add(kvp.Key);

      var pinfos = GetType()
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(x => x.GetCustomAttribute(typeof(SchemaIgnore)) == null
                    && x.GetCustomAttribute(typeof(ObsoleteAttribute)) == null
                    && x.Name != "Item");
      foreach (var pinfo in pinfos) names.Add(pinfo.Name);

      return names;
    }

    /// <summary>
    ///  Gets the typed and dynamic properties that don't have a [SchemaIgnore] attribute.
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, object> GetMembers()
    {
      //typed members
      var dic = new Dictionary<string, object>();
      var pinfos = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(x => x.GetCustomAttribute(typeof(SchemaIgnore)) == null && x.GetCustomAttribute(typeof(ObsoleteAttribute)) == null && x.Name != "Item");
      foreach (var pi in pinfos)
        dic.Add(pi.Name, pi.GetValue(this));
      //dynamic members
      foreach (var kvp in properties)
        dic.Add(kvp.Key, kvp.Value);
      return dic;
    }

    /// <summary>
    /// Gets the dynamically added property names only.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetDynamicMembers()
    {
      foreach (var kvp in properties)
        yield return kvp.Key;
    }

    /// <summary>
    /// Currently we assume having only 1 property means we auto-wrap it around a DynamicBase, therefore is a wrapper. This might change in the future.
    /// </summary>
    public bool IsWrapper() => properties.Count == 1;

  }

}
