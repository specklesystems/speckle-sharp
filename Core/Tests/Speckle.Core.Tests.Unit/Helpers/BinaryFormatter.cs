using System.Security.Cryptography;
using NUnit.Framework;
using Speckle.Core.Helpers;

namespace Speckle.Core.Tests.Unit.Helpers;

[TestFixture]
[TestOf(nameof(BinaryFormatter))]
public class BinaryFormatterTests
{
  [Test]
  public void ZeroLengthStringParityCheck() => CompareTechniques(new string('b', 0));

  /// <summary>
  /// Refer to https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/10b218f5-9b2b-4947-b4b7-07725a2c8127 for the specifics of this requirement.
  /// </summary>
  [Test]
  public void SingleByteLengthStorageParityCheck() => CompareTechniques(new string('b', 127));

  /// <summary>
  /// Refer to https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/10b218f5-9b2b-4947-b4b7-07725a2c8127 for the specifics of this requirement.
  /// </summary>
  [Test]
  public void TwoByteLengthStorageParityCheck() => CompareTechniques(new string('b', 128));

  /// <summary>
  /// Refer to https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/10b218f5-9b2b-4947-b4b7-07725a2c8127 for the specifics of this requirement.
  /// </summary>
  [Test]
  public void ThreeByteLengthStorageParityCheck() => CompareTechniques(new string('b', 16384));

  /// <summary>
  /// Refer to https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/10b218f5-9b2b-4947-b4b7-07725a2c8127 for the specifics of this requirement.
  /// </summary>
  [Test]
  public void FourByteLengthStorageParityCheck() => CompareTechniques(new string('b', 2097152));

  /// <summary>
  /// Refer to https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/10b218f5-9b2b-4947-b4b7-07725a2c8127 for the specifics of this requirement.
  /// </summary>
  [Test]
  public void FiveByteLengthStorageParityCheck() => CompareTechniques(new string('b', 268435456));

  /// <summary>
  /// Assets that null behaviour is the same between both techniques.
  /// </summary>
  [Test]
  public void NullStringParityCheck()
  {
    using (MemoryStream ms = new())
    {
      Assert.Throws<ArgumentNullException>(
        () => BinaryFormatter.SerialiseString(ms, null)
      );
    }

    using (MemoryStream ms2 = new())
    {
#pragma warning disable SYSLIB0011 // Type or member is obsolete
      Assert.Throws<ArgumentNullException>(
        () => new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter().Serialize(ms2, null)
      );
#pragma warning restore SYSLIB0011 // Type or member is obsolete
    };
  }

  private static void CompareTechniques(string str)
  {
    byte[] ourHash;
    byte[] msHash;

    using (MemoryStream ms = new())
    {

      BinaryFormatter.SerialiseString(ms, str);

      using SHA256 sha = SHA256.Create();
#pragma warning disable CA1850 // Prefer static 'HashData' method over 'ComputeHash'  <- note that this probably indicates this should be changed in Helpers/Crypt.cs
      ourHash = sha.ComputeHash(ms.ToArray());
#pragma warning restore CA1850 // Prefer static 'HashData' method over 'ComputeHash'

    }

    using (MemoryStream ms2 = new())
    {

#pragma warning disable SYSLIB0011 // Type or member is obsolete
      new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter().Serialize(ms2, str);
#pragma warning restore SYSLIB0011 // Type or member is obsolete

      using SHA256 sha = SHA256.Create();
#pragma warning disable CA1850 // Prefer static 'HashData' method over 'ComputeHash'  <- note that this probably indicates this should be changed in Helpers/Crypt.cs
      msHash = sha.ComputeHash(ms2.ToArray());
#pragma warning restore CA1850 // Prefer static 'HashData' method over 'ComputeHash'
    };

    Assert.That(ourHash, Is.EquivalentTo(msHash));
  }





}
