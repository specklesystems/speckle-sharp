using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;

namespace Speckle.Core.Models;

public static class Utilities
{
  public enum HashingFunctions
  {
    SHA256,
    MD5
  }

  public static int HashLength => 32;

  /// <summary>
  /// Wrapper method around hashing functions..
  /// </summary>
  /// <param name="input"></param>
  /// <returns></returns>
  [Pure]
  public static string HashString(string input, HashingFunctions func = HashingFunctions.SHA256)
  {
    return func switch
    {
      HashingFunctions.SHA256 => Crypt.Sha256(input, length: HashLength),
      HashingFunctions.MD5 => Crypt.Md5(input, length: HashLength),
      _ => throw new ArgumentOutOfRangeException(nameof(func), func, "Unrecognised value"),
    };
  }

  [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
  public static string HashFile(string filePath, HashingFunctions func = HashingFunctions.SHA256)
  {
    using HashAlgorithm hashAlgorithm = func == HashingFunctions.MD5 ? MD5.Create() : SHA256.Create();

    using var stream = File.OpenRead(filePath);

    var hash = hashAlgorithm.ComputeHash(stream);
    return BitConverter.ToString(hash, 0, HashLength).Replace("-", "").ToLowerInvariant();
  }

  [Pure]
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
  public static Base GetApplicationProps(
    object o,
    Type t,
    bool getParentProps = false,
    IReadOnlyList<string>? ignore = null
  )
  {
    var appProps = new Base();
    appProps["class"] = t.Name;

    try
    {
      // set primitive writeable props
      foreach (var propInfo in t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
      {
        if (ignore != null && ignore.Contains(propInfo.Name))
        {
          continue;
        }

        if (IsMeaningfulProp(propInfo, o, out object? propValue))
        {
          appProps[propInfo.Name] = propValue;
        }
      }

      if (getParentProps)
      {
        foreach (
          var propInfo in t.BaseType.GetProperties(
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public
          )
        )
        {
          if (ignore != null && ignore.Contains(propInfo.Name))
          {
            continue;
          }

          if (IsMeaningfulProp(propInfo, o, out object? propValue))
          {
            appProps[propInfo.Name] = propValue;
          }
        }
      }
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Warning(ex, "Failed to get application properties");
    }

    return appProps;
  }

  private static bool IsMeaningfulProp(PropertyInfo propInfo, object o, out object? value)
  {
    value = propInfo.GetValue(o);
    if (propInfo.GetSetMethod() != null && value != null)
    {
      if (propInfo.PropertyType.IsPrimitive || propInfo.PropertyType == typeof(decimal))
      {
        return true;
      }

      if (propInfo.PropertyType == typeof(string) && !string.IsNullOrEmpty((string)value))
      {
        return true;
      }

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
  [Obsolete("Unused")]
  public static void SetApplicationProps(object o, Type t, Base props)
  {
    var propNames = props.GetDynamicMembers();
    IEnumerable<string> names = propNames.ToList();
    if (o == null || names.Any())
    {
      return;
    }

    var typeProperties = t.GetProperties().ToList();
    typeProperties.AddRange(t.BaseType.GetProperties().ToList());
    foreach (var propInfo in typeProperties)
    {
      if (propInfo.CanWrite && names.Contains(propInfo.Name))
      {
        var value = props[propInfo.Name];
        if (propInfo.PropertyType.BaseType.Name == "Enum")
        {
          value = Enum.Parse(propInfo.PropertyType, (string)value);
        }

        if (value != null)
        {
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
          catch (Exception ex) when (!ex.IsFatal()) { }
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
  [Obsolete("Unused")]
  public static IEnumerable<List<T>> SplitList<T>(List<T> list, int chunkSize = 50)
  {
    for (int i = 0; i < list.Count; i += chunkSize)
    {
      yield return list.GetRange(i, Math.Min(chunkSize, list.Count - i));
    }
  }
}
