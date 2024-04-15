using NUnit.Framework;
using Speckle.Core.Helpers;

namespace Speckle.Core.Tests.Unit.Helpers;

[TestFixture]
[TestOf(nameof(BinaryFormatter))]
public class BinaryFormatterTests
{
  const string JSON = /*lang=json,strict*/ """{"totalChildrenCount":0,"applicationId":null,"speckle_type":"Base","string prop":"simple test case","numerical prop":123}""";
  const string WEIRDO = "ާ5D쬣ڍtr/_ţ:󺆌ᚤ䯆#ϻϥoÈȳ3\U0006e92cݽ\U00085ee7ꨖ$\U000c372e։v뾀Ƽچ\r\n썴ϐܔȜbG헙󳄶֚<ݝЃě\U000e6af0D\ud7a9ǦH\U000529adr\U0001ce21֔􉴨\U00019423!溉ł.\U000df40dʾښ7\r\nͭit\U0006cecaӊ󻱱l󻰨Ǎ䔚\u169dּ¶\U0004c9ff\U0008eab7ﮯ&\U000190a3囎ơЍ\U0001973a맇꾒\U00039ee8I`\U00041df1C@\U000dbeb1\r\nU赂͡󷧓¾蚉\U000d4636譄ʹ\U000924fdZ\U000aa470ုvv\U0009f2d8렾ΚD󱩧ƽ\u2bf9ꎞ\U000398f9ӏ\U000e0b14簮\U0001377f갑_N\r\nS34+\U000a19e2Սь̱峺ʏ͊寞XZ꼎亖ѱ[̝\u05ee\U0009b408庹\U00067996ޖߎ\U000633da䃔Ϲяꁫȿ";

  [Test]
  public void BasicBinaryFormatterParityTest() => CompareTechniques(JSON);

  [Test]
  public void WeirdBinaryFormatterParityTest() => CompareTechniques(WEIRDO);


  [Test]
  public void ShortStringBinaryFormatterParityTest() => CompareTechniques(new string('b', 5));

  [Test]
  public void LongStringBinaryFormatterParityTest() => CompareTechniques(new string('b', 500000));

  private static void CompareTechniques(string str)
  {
    using MemoryStream ms = new();

    BinaryFormatter.SerialiseString(ms, str);

    using MemoryStream ms2 = new();

#pragma warning disable SYSLIB0011 // Type or member is obsolete
    new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter().Serialize(ms2, str);
#pragma warning restore SYSLIB0011 // Type or member is obsolete

    Assert.That(ms.ToArray(), Is.EquivalentTo(ms2.ToArray()));
  }





}
