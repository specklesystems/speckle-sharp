#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Speckle.Core.Helpers;

public static class Crypt
{
  [Pure]
  public static string Sha256(string input, string? format = "x2", int start = 0, int end = 64)
  {
    using MemoryStream ms = new();

    new BinaryFormatter().Serialize(ms, input);

    using SHA256 sha = SHA256.Create();
    var hash = sha.ComputeHash(ms.ToArray());
    StringBuilder sb = new(64);
    foreach (byte b in hash)
    {
      sb.Append(b.ToString(format));
    }

    return sb.ToString(start, end);
  }

  [Pure]
  [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
  public static string Md5(string input, string? format = "x2", int start = 0, int end = 32)
  {
    using MD5 md5 = MD5.Create();
    byte[] inputBytes = Encoding.ASCII.GetBytes(input.ToLowerInvariant());
    byte[] hashBytes = md5.ComputeHash(inputBytes);

    StringBuilder sb = new(32);
    for (int i = 0; i < hashBytes.Length; i++)
    {
      sb.Append(hashBytes[i].ToString(format));
    }

    return sb.ToString(start, end);
  }

  [Obsolete("Use Md5(input, \"X2\") instead")]
  public static string Hash(string input) => Md5(input, "X2");
}
