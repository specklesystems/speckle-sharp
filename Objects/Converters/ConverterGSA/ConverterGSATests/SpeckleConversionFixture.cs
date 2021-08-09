using System;
using Speckle.GSA.API;
using Speckle.Core.Kits;

namespace ConverterGSATests
{
  public abstract class SpeckleConversionFixture : IDisposable
  {
    protected ISpeckleConverter converter;
    protected GsaModelMock gsaModelMock = new GsaModelMock();

    public SpeckleConversionFixture()
    {
      /*  For possible future use
      var gsaModelMock = new Mock<IGSAModel>();
      gsaModelMock.SetupGet(x => x.GwaDelimiter).Returns('\t');
      */
      converter = new ConverterGSA.ConverterGSA();
      Instance.GsaModel = gsaModelMock;
    }

    public void Dispose()
    {
      Instance.GsaModel = null;
    }
  }
}
