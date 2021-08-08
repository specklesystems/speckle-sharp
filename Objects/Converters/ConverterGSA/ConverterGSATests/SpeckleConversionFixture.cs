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
  public abstract class SpeckleConversionFixture : IDisposable
  {
    public SpeckleConversionFixture()
    {
      var gsaModelMock = new Mock<IGSAModel>();
      gsaModelMock.SetupGet(x => x.GwaDelimiter).Returns('\t');
      Instance.GsaModel = gsaModelMock.Object;
    }

    public void Dispose()
    {
      Instance.GsaModel = null;
    }
  }
}
