using System;
using System.Collections.Generic;
using System.Linq;
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
        typeof(String),
        typeof(Decimal),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid)
        }.Contains(type) ||
        Convert.GetTypeCode(type) != TypeCode.Object;
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
