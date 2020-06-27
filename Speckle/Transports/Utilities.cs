using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speckle.Transports
{
  public static class Utilities
  {
    /// <summary>
    /// Compresses the string.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns></returns>
    public static string CompressString(string text)
    {
      //return text;
      byte[] buffer = Encoding.UTF8.GetBytes(text);
      var memoryStream = new MemoryStream();
      using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
      {
        gZipStream.Write(buffer, 0, buffer.Length);
      }

      memoryStream.Position = 0;

      var compressedData = new byte[memoryStream.Length];
      memoryStream.Read(compressedData, 0, compressedData.Length);

      var gZipBuffer = new byte[compressedData.Length + 4];
      Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
      Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
      return Convert.ToBase64String(gZipBuffer);
    }

    /// <summary>
    /// Decompresses the string.
    /// </summary>
    /// <param name="compressedText">The compressed text.</param>
    /// <returns></returns>
    public static string DecompressString(string compressedText)
    {
      //return compressedText;
      byte[] gZipBuffer = Convert.FromBase64String(compressedText);
      using (var memoryStream = new MemoryStream())
      {
        int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
        memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

        var buffer = new byte[dataLength];

        memoryStream.Position = 0;
        using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
        {
          gZipStream.Read(buffer, 0, buffer.Length);
        }

        return Encoding.UTF8.GetString(buffer);
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

    public static async Task WaitUntil(Func<bool> condition, int frequency = 25, int timeout = -1)
    {
      var waitTask = Task.Run(async () =>
      {
        while (!condition()) await Task.Delay(frequency);
      });

      if (waitTask != await Task.WhenAny(waitTask,
              Task.Delay(timeout)))
        throw new TimeoutException();
    }

  }
}
