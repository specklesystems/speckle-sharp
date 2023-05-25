using System;
using System.Collections.Generic;
using System.Text;

namespace TestGenerator
{
  internal static class TestTemplate
  {
    public const string StartCode = @"
using System.Threading.Tasks;
using Xunit;
using DB = Autodesk.Revit.DB;

namespace ConverterRevitTests
{
";
    public const string EndCode = @"
}
";
    public static string Create(string category, string fileName, string revitType, string assertFunc) => $@"
  public class {category}{fileName}Fixture : SpeckleConversionFixture
  {{
    public override string Category => ""{category}"";
    public override string TestName => ""{fileName}"";

    public {category}{fileName}Fixture() : base()
    {{
    }}
  }}

  public class {category}{fileName}Tests : SpeckleConversionTest, IClassFixture<{category}{fileName}Fixture>
  {{
    public {category}{fileName}Tests({category}{fileName}Fixture fixture) : base(fixture)
    {{
    }}

    [Fact]
    [Trait(""{category}"", ""{fileName}ToSpeckle"")]
    public async Task {category}{fileName}ToSpeckle()
    {{
      await NativeToSpeckle();
    }}

    [Fact]
    [Trait(""{category}"", ""{fileName}ToNative"")]
    public async Task {category}{fileName}ToNative()
    {{
      await SpeckleToNative<{revitType}>({assertFunc});
    }}

    [Fact]
    [Trait(""{category}"", ""{fileName}ToNativeUpdates"")]
    public async Task {category}{fileName}ToNativeUpdates()
    {{
      await SpeckleToNativeUpdates<{revitType}>({assertFunc});
    }}

    [Fact]
    [Trait(""{category}"", ""{fileName}Selection"")]
    public async Task {category}{fileName}SelectionToNative()
    {{
      await SelectionToNative<{revitType}>({assertFunc});
    }}
  }}
";
  }
}
