using System.Security.Cryptography;
using System.Text;

namespace Speckle.Models
{
  public static class Utilities
  {
    public static string md5(string input)
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
