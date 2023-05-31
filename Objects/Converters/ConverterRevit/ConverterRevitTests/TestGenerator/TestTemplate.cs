using System;
using System.Collections.Generic;
using System.Text;

namespace TestGenerator
{
  internal static class TestTemplate
  {
    public const string StartNamespace = @"
using System.Threading.Tasks;
using Xunit;
using DB = Autodesk.Revit.DB;

namespace ConverterRevitTests
{
";
    public const string EndNamespace = @"
}
";
    public static string CreateFixture(string category, string fileName) => $@"
  public class {category}{fileName}Fixture : SpeckleConversionFixture
  {{
    public override string Category => ""{category}"";
    public override string TestName => ""{fileName}"";

    public {category}{fileName}Fixture() : base()
    {{
    }}
  }}
";

    public static string InitTest(string category, string fileName) => $@"
  public class {category}{fileName}Tests : SpeckleConversionTest, IClassFixture<{category}{fileName}Fixture>
  {{
    public {category}{fileName}Tests({category}{fileName}Fixture fixture) : base(fixture)
    {{
    }}
";

    public static string CreateToSpeckleTest(string category, string fileName) => $@"
    [Fact]
    [Trait(""{category}"", ""{fileName}ToSpeckle"")]
    public async Task {category}{fileName}ToSpeckle()
    {{
      await NativeToSpeckle();
    }}
";

    public static string CreateToNativeTest(string category, string fileName, string revitType, string syncAssertFunc, string asyncAssertFunc) => $@"
    [Fact]
    [Trait(""{category}"", ""{fileName}ToNative"")]
    public async Task {category}{fileName}ToNative()
    {{
      await SpeckleToNative<{revitType}>({syncAssertFunc}, {asyncAssertFunc});
    }}
";

    public static string CreateUpdateTest(string category, string fileName, string revitType, string syncAssertFunc, string asyncAssertFunc) => $@"
    [Fact]
    [Trait(""{category}"", ""{fileName}ToNativeUpdates"")]
    public async Task {category}{fileName}ToNativeUpdates()
    {{
      await SpeckleToNativeUpdates<{revitType}>({syncAssertFunc}, {asyncAssertFunc});
    }}
";

    public static string CreateSelectionTest(string category, string fileName, string revitType, string syncAssertFunc, string asyncAssertFunc) => $@"
    [Fact]
    [Trait(""{category}"", ""{fileName}Selection"")]
    public async Task {category}{fileName}SelectionToNative()
    {{
      await SelectionToNative<{revitType}>({syncAssertFunc}, {asyncAssertFunc});
    }}
";

    public const string EndClass = @"
  }
";
  }
}
