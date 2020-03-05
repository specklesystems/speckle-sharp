using System.Security.Cryptography;
using System.Text;

namespace Speckle.Models
{
  public static class Utilities
  {
    public enum HashingFuctions
    {
      SHA256, MD5
    }

    /// <summary>
    /// Wrapper method around hashing functions. Defaults to md5.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string hashString(string input, HashingFuctions func = HashingFuctions.MD5)
    {
      switch (func)
      {
        case HashingFuctions.SHA256:
          return Utilities.sha256(input);

        case HashingFuctions.MD5:
        default:
          return Utilities.md5(input);
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
            sb.Append(b.ToString("X2"));
          return sb.ToString().ToLower();
        }
      }
    }

    static string md5(string input)
    {
      using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
      {
        new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter().Serialize(ms, input);
        using (MD5 md5 = MD5.Create())
        {
          var hash = md5.ComputeHash(ms.ToArray());
          StringBuilder sb = new StringBuilder();
          foreach (byte b in hash)
            sb.Append(b.ToString("X2"));
          return sb.ToString().ToLower();
        }
      }
    }
  }
}
