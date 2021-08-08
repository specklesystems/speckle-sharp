using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Speckle.GSA.API;

namespace ConverterGSATests
{
  public class SchemaTest : SpeckleConversionFixture
  {
    public SchemaTest() : base() { }

    [Fact]
    public void Test1()
    {
      Assert.Equal('\t', Instance.GsaModel.GwaDelimiter);
    }
  }
}
