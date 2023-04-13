using System.Security.Cryptography;
using System.Text;

namespace Speckle.Core.Helpers;

public static class Crypt
{
  public static string Hash(string input)
  {
    using (MD5 md5 = MD5.Create())
    {
      byte[] inputBytes = Encoding.ASCII.GetBytes(input.ToLowerInvariant());
      byte[] hashBytes = md5.ComputeHash(inputBytes);

      StringBuilder sb = new();
      for (int i = 0; i < hashBytes.Length; i++)
        sb.Append(hashBytes[i].ToString("X2"));
      return sb.ToString();
    }
  }
}
