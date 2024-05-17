using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using System.Text;

namespace Speckle.Core.Helpers;

public static class Crypt
{
  /// <param name="input">the value to hash</param>
  /// <param name="format">NumericFormat</param>
  /// <param name="startIndex"></param>
  /// <param name="length"></param>
  /// <returns>the hash string</returns>
  /// <exception cref="FormatException"><paramref name="format"/> is not a recognised numeric format</exception>
  /// <exception cref="ArgumentOutOfRangeException"><inheritdoc cref="StringBuilder.ToString(int, int)"/></exception>
  [Pure]
  public static string Sha256(string input, string? format = "x2", int startIndex = 0, int length = 64)
  {
    var inputBytes = Encoding.UTF8.GetBytes(input);

    using var sha256 = SHA256.Create();
    byte[] hash = sha256.ComputeHash(inputBytes);

    StringBuilder sb = new(64);
    foreach (byte b in hash)
    {
      sb.Append(b.ToString(format));
    }

    return sb.ToString(startIndex, length);
  }

  /// <inheritdoc cref="Sha256"/>
  /// <remarks>MD5 is a broken cryptographic algorithm and should be used subject to review see CA5351</remarks>
  [Pure]
  [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
  public static string Md5(string input, string? format = "x2", int startIndex = 0, int length = 32)
  {
    using MD5 md5 = MD5.Create();
    byte[] inputBytes = Encoding.ASCII.GetBytes(input.ToLowerInvariant());
    byte[] hashBytes = md5.ComputeHash(inputBytes);

    StringBuilder sb = new(32);
    for (int i = 0; i < hashBytes.Length; i++)
    {
      sb.Append(hashBytes[i].ToString(format));
    }

    return sb.ToString(startIndex, length);
  }

  [Obsolete("Use Md5(input, \"X2\") instead")]
  public static string Hash(string input) => Md5(input, "X2");
}
