using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Speckle.Core.Models
{
  public static class Utilities
  {

    public static int HashLength { get; } = 32;

    public enum HashingFuctions
    {
      SHA256, MD5
    }

    /// <summary>
    /// Wrapper method around hashing functions. Defaults to md5.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string hashString(string input, HashingFuctions func = HashingFuctions.SHA256)
    {
      switch (func)
      {
        case HashingFuctions.SHA256:
          return Utilities.sha256(input).Substring(0, HashLength);

        case HashingFuctions.MD5:
        default:
          return Utilities.md5(input).Substring(0, HashLength);

      }
    }

    static string sha256(string input)
    {
      using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
      {
        new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter().Serialize(ms, input);
        using (SHA256 sha = SHA256.Create())
        {
          var hash = sha.ComputeHash(ms.ToArray());
          StringBuilder sb = new StringBuilder();
          foreach (byte b in hash)
          {
            sb.Append(b.ToString("X2"));
          }

          return sb.ToString().ToLower();
        }
      }
    }

    static string md5(string input)
    {
      using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
      {
        byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input.ToLowerInvariant());
        byte[] hashBytes = md5.ComputeHash(inputBytes);

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
        {
          sb.Append(hashBytes[i].ToString("X2"));
        }
        return sb.ToString().ToLower();
      }
    }

    public static bool IsSimpleType(this Type type)
    {
      return
        type.IsPrimitive ||
        new Type[] {
        typeof(string),
        typeof(decimal),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid)
        }.Contains(type) ||
        Convert.GetTypeCode(type) != TypeCode.Object;
    }

    /// <summary>
    /// Retrieves the simple type properties of an object
    /// </summary>
    /// <param name="o"></param>
    /// <param name="t"></param>
    /// <param name="getParentProps">Set to true to also retrieve simple props of direct parent type</param>
    /// <param name="ignore">Names of props to ignore</param>
    /// <returns></returns>
    public static Base GetApplicationProps(object o, Type t, bool getParentProps = false, List<string> ignore = null)
    {
      var appProps = new Base();
      appProps["class"] = t.Name;

      // set primitive writeable props 
      foreach (var propInfo in t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
      {
        if (ignore != null && ignore.Contains(propInfo.Name)) continue;
        if (IsMeaningfulProp(propInfo, o, out object propValue))
          appProps[propInfo.Name] = propValue;
      }
      if (getParentProps)
      {
        foreach (var propInfo in t.BaseType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
        {
          if (ignore != null && ignore.Contains(propInfo.Name)) continue;
          if (IsMeaningfulProp(propInfo, o, out object propValue))
            appProps[propInfo.Name] = propValue;
        }
      }

      return appProps;
    }
    private static bool IsMeaningfulProp(PropertyInfo propInfo, object o, out object value)
    {
      value = propInfo.GetValue(o);
      if (propInfo.GetSetMethod() != null && value != null)
      {
        if (propInfo.PropertyType.IsPrimitive || propInfo.PropertyType == typeof(decimal)) return true;
        if (propInfo.PropertyType == typeof(string) && !string.IsNullOrEmpty((string)value)) return true;
        if (propInfo.PropertyType.BaseType.Name == "Enum") // for some reason "IsEnum" prop returns false
        {
          value = value.ToString();
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Sets the properties of an object with the properties of a base object
    /// </summary>
    /// <param name="o"></param>
    /// <param name="t"></param>
    /// <param name="props">The base class object representing application props</param>
    public static void SetApplicationProps(object o, Type t, Base props)
    {
      var propNames = props.GetDynamicMembers();
      if (o == null || propNames.Count() == 0)
        return;

      var typeProperties = t.GetProperties().ToList();
      typeProperties.AddRange(t.BaseType.GetProperties().ToList());
      foreach (var propInfo in typeProperties)
      {
        if (propInfo.CanWrite && propNames.Contains(propInfo.Name))
        {
          var value = props[propInfo.Name];
          if (propInfo.PropertyType.BaseType.Name == "Enum")
            value = Enum.Parse(propInfo.PropertyType, (string)value);
          if (value != null)
          {
            try
            {
              t.InvokeMember(propInfo.Name,
              BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty,
              Type.DefaultBinder, o, new object[] { value });
            }
            catch { }
          }
        }
      }
    }

    /// <summary>
    /// Chunks a list into pieces.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="chunkSize"></param>
    /// <returns></returns>
    public static IEnumerable<List<T>> SplitList<T>(List<T> list, int chunkSize = 50)
    {
      for (int i = 0; i < list.Count; i += chunkSize)
      {
        yield return list.GetRange(i, Math.Min(chunkSize, list.Count - i));
      }
    }

  }

}
