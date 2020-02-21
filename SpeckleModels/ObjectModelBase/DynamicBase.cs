using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace Speckle.Models
{

  /// <summary>
  /// Base class implementing a bunch of nice dynamic object methods. Do not use directly unless you know what you're doing :)
  /// </summary>
  public class DynamicBase : DynamicObject, IDynamicMetaObjectProvider
  {
    private Dictionary<string, object> properties = new Dictionary<string, object>();

    public DynamicBase()
    {

    }

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
      return (properties.TryGetValue(binder.Name, out result));
    }

    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
      properties[binder.Name] = value;
      return true;
    }

    public object this[string key]
    {
      get
      {
        if (properties.ContainsKey(key))
          return properties[key];
        try
        {
          return GetType().GetProperty(key).GetValue(this);
        }
        catch
        {
          throw;
        }
      }
      set
      {
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
        catch
        {
          throw;
        }
      }
    }

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
    /// Gets the names of the defined class properties
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetInstanceMembers()
    {
      var names = new List<string>();
      var pinfos = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
      foreach (var pinfo in pinfos) names.Add(pinfo.Name);

      names.Remove("Item"); // TODO: investigate why we get Item out?
      return names;
    }

    /// <summary>
    /// Gets the dynamically added properties
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetDynamicMembers()
    {
      foreach (var kvp in properties)
        yield return kvp.Key;
    }
  }

}
