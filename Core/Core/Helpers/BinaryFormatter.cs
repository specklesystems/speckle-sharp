using System;
using System.IO;
using System.Text;

namespace Speckle.Core.Helpers;

/// <summary>
/// Mimics the behaviour of System.Runtime.Serialization.Formatters.Binary.BinaryFormatter for compatibility with Dotnet 8
/// </summary>
public static class BinaryFormatter
{
  /// <summary>
  /// Leading bytes on a BinaryFormatter as defined here: 
  /// https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/a7e578d3-400a-4249-9424-7529d10d1b3c 
  /// and here: https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/eb503ca5-e1f6-4271-a7ee-c4ca38d07996
  /// </summary>
  private static readonly byte[] s_prefix = new byte[] { 
      0,                   // RecordTypeEnum (always 0)
      1,   0,   0,   0,    // Root ID, Id of the incoming string (see below)
    255, 255, 255, 255,    // Header ID
      1,   0,   0,   0,    // Major Version
      0,   0,   0,   0,    // Minor Version
      6,                   // RecordTypeEnum for string - must be 6
      1,   0,   0,   0     // Id of the upcoming string, 1 as there is no other string.
  };
  /// <summary>
  /// Indicates end of file
  /// </summary>
  private const byte SUFFIX = 11;

  /// <summary>
  /// Mimics the behaviour of System.Runtime.Serialization.Formatters.Binary.BinaryFormatter().Serialize() when applied to a single string record.
  /// </summary>
  /// <param name="ms">Memory stream to append to</param>
  /// <param name="value">String to serialise</param>
  public static void SerialiseString(MemoryStream ms, string value)
  {
    ms.Write(s_prefix,0, s_prefix.Length);

    var utf8bytes = Encoding.UTF8.GetBytes(value);
    var encodedLength = EncodeLength(utf8bytes.Length);
    ms.Write(encodedLength, 0,encodedLength.Length);

    ms.Write(utf8bytes, 0, utf8bytes.Length);
    ms.WriteByte(SUFFIX);
  }


  /// <summary>
  /// Length encoding to bytes as defined here: https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/10b218f5-9b2b-4947-b4b7-07725a2c8127
  /// </summary>
  /// <param name="length">Integer to encode into a length</param>
  /// <returns></returns>
  /// <exception cref="ArgumentException"></exception>
  private static byte[] EncodeLength(int length)
  {
    if (length is < 0 or > 2147483647)
    {
      throw new ArgumentException("Length must be between 0 and 2147483647 inclusive.");
    }

    byte[] encodedBytes;

    if (length <= 127)
    {
      // Single byte encoding
      encodedBytes = new byte[1];
      encodedBytes[0] = (byte)length;
    }
    else if (length <= 16383)
    {
      // Two byte encoding
      encodedBytes = new byte[2];
      encodedBytes[0] = (byte)((length & 0x7F) | 0x80);
      encodedBytes[1] = (byte)(length >> 7);
    }
    else if (length <= 2097151)
    {
      // Three byte encoding
      encodedBytes = new byte[3];
      encodedBytes[0] = (byte)((length & 0x7F) | 0x80);
      encodedBytes[1] = (byte)(((length >> 7) & 0x7F) | 0x80);
      encodedBytes[2] = (byte)(length >> 14);
    }
    else if (length <= 268435455)
    {
      // Four byte encoding
      encodedBytes = new byte[4];
      encodedBytes[0] = (byte)((length & 0x7F) | 0x80);
      encodedBytes[1] = (byte)(((length >> 7) & 0x7F) | 0x80);
      encodedBytes[2] = (byte)(((length >> 14) & 0x7F) | 0x80);
      encodedBytes[3] = (byte)(length >> 21);
    }
    else
    {
      // Five byte encoding
      encodedBytes = new byte[5];
      encodedBytes[0] = (byte)((length & 0x7F) | 0x80);
      encodedBytes[1] = (byte)(((length >> 7) & 0x7F) | 0x80);
      encodedBytes[2] = (byte)(((length >> 14) & 0x7F) | 0x80);
      encodedBytes[3] = (byte)(((length >> 21) & 0x7F) | 0x80);
      encodedBytes[4] = (byte)(length >> 28);
    }

    return encodedBytes;
  }
}
