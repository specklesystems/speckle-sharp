using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using Speckle.Core.Logging;

namespace Speckle.Core.Models;

[SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
public static class Utilities
{
  public enum HashingFunctions
  {
    SHA256,
    MD5
  }

  public static int HashLength { get; } = 32;

  /// <summary>
  /// Wrapper method around hashing functions..
  /// </summary>
  /// <param name="input"></param>
  /// <returns></returns>
  public static string HashString(string input, HashingFunctions func = HashingFunctions.SHA256)
  {
    switch (func)
    {
      case HashingFunctions.SHA256:
        return Sha256(input).Substring(0, HashLength);

      case HashingFunctions.MD5:
      default:
        return Md5(input).Substring(0, HashLength);
    }
  }

  public static string HashFile(string filePath, HashingFunctions func = HashingFunctions.SHA256)
  {
    HashAlgorithm hashAlgorithm;
    if (func == HashingFunctions.MD5)
      hashAlgorithm = MD5.Create();
    else
      hashAlgorithm = SHA256.Create();

    using (var stream = File.OpenRead(filePath))
    {
      var hash = hashAlgorithm.ComputeHash(stream);
      hashAlgorithm.Dispose();
      return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant().Substring(0, HashLength);
    }
  }

  private static string Sha256(string input)
  {
    using MemoryStream ms = new();
    
    new BinaryFormatter().Serialize(ms, input);
    using SHA256 sha = SHA256.Create();
    
    var hash = sha.ComputeHash(ms.ToArray());
    StringBuilder sb = new();
    foreach (byte b in hash)
      sb.Append(b.ToString("X2"));

    return sb.ToString().ToLower();
  }
  
  private static string Md5(string input)
  {
    using MD5 md5 = MD5.Create();
    byte[] inputBytes = Encoding.ASCII.GetBytes(input.ToLowerInvariant());
    byte[] hashBytes = md5.ComputeHash(inputBytes);

    StringBuilder sb = new();
    for (int i = 0; i < hashBytes.Length; i++)
      sb.Append(hashBytes[i].ToString("X2"));
    return sb.ToString().ToLower();
  }

  public static bool IsSimpleType(this Type type)
  {
    return type.IsPrimitive
      || new[]
      {
        typeof(string),
        typeof(decimal),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid)
      }.Contains(type)
      || Convert.GetTypeCode(type) != TypeCode.Object;
  }

  /// <summary>
  /// Retrieves the simple type properties of an object
  /// </summary>
  /// <param name="o"></param>
  /// <param name="t"></param>
  /// <param name="getParentProps">Set to true to also retrieve simple props of direct parent type</param>
  /// <param name="ignore">Names of props to ignore</param>
  /// <returns></returns>
  public static Base GetApplicationProps(object o, Type t, bool getParentProps = false, IReadOnlyList<string> ignore = null)
  {
    var appProps = new Base();
    appProps["class"] = t.Name;

    try
    {
      // set primitive writeable props
      foreach (var propInfo in t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
      {
        if (ignore != null && ignore.Contains(propInfo.Name))
          continue;
        if (IsMeaningfulProp(propInfo, o, out object propValue))
          appProps[propInfo.Name] = propValue;
      }

      if (getParentProps)
        foreach (
          var propInfo in t.BaseType.GetProperties(
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public
          )
        )
        {
          if (ignore != null && ignore.Contains(propInfo.Name))
            continue;
          if (IsMeaningfulProp(propInfo, o, out object propValue))
            appProps[propInfo.Name] = propValue;
        }
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Warning(ex, "Failed to get application properties");
    }

    return appProps;
  }

  private static bool IsMeaningfulProp(PropertyInfo propInfo, object o, out object value)
  {
    value = propInfo.GetValue(o);
    if (propInfo.GetSetMethod() != null && value != null)
    {
      if (propInfo.PropertyType.IsPrimitive || propInfo.PropertyType == typeof(decimal))
        return true;
      if (propInfo.PropertyType == typeof(string) && !string.IsNullOrEmpty((string)value))
        return true;
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
    IEnumerable<string> names = propNames.ToList();
    if (o == null || names.Any())
      return;

    var typeProperties = t.GetProperties().ToList();
    typeProperties.AddRange(t.BaseType.GetProperties().ToList());
    foreach (var propInfo in typeProperties)
      if (propInfo.CanWrite && names.Contains(propInfo.Name))
      {
        var value = props[propInfo.Name];
        if (propInfo.PropertyType.BaseType.Name == "Enum")
          value = Enum.Parse(propInfo.PropertyType, (string)value);
        if (value != null)
          try
          {
            t.InvokeMember(
              propInfo.Name,
              BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty,
              Type.DefaultBinder,
              o,
              new[] { value }
            );
          }
          catch { }
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
      yield return list.GetRange(i, Math.Min(chunkSize, list.Count - i));
  }

  #region Deprecated Members
  [Obsolete("Use " + nameof(HashString), true)]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  public static string hashString(string input, HashingFunctions func = HashingFunctions.SHA256)
  {
    return HashString(input, func);
  }

  [Obsolete("Use " + nameof(HashFile), true)]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  public static string hashFile(string filePath, HashingFunctions func = HashingFunctions.SHA256)
  {
    return hashString(filePath, func);
  }

  #endregion
}
