using System;
using Speckle.GSA.API;
using Speckle.Core.Kits;
using System.Linq.Expressions;

namespace ConverterGSATests
{
  public abstract class SpeckleConversionFixture : IDisposable
  {
    protected ISpeckleConverter converter;
    protected GsaModelMock gsaModelMock = new GsaModelMock();
    protected int highestNodeIndex = 0;

    public SpeckleConversionFixture()
    {
      /*  For possible future use
      var gsaModelMock = new Mock<IGSAModel>();
      gsaModelMock.SetupGet(x => x.GwaDelimiter).Returns('\t');
      */
      converter = new ConverterGSA.ConverterGSA();
      Instance.GsaModel = gsaModelMock;
      ((GsaProxyMockForConverterTests)Instance.GsaModel.Proxy).NodeAtFn = (double x, double y, double z) => (++highestNodeIndex);
    }

    public void Dispose()
    {
      Instance.GsaModel = null;
    }

    //https://stackoverflow.com/questions/17048752/how-to-get-a-property-name-from-the-property-of-a-class-instance/17049349#17049349
    public static string GetPropertyName<T, P>(Expression<Func<T, P>> propertyDelegate)
    {
      var expression = (MemberExpression)propertyDelegate.Body;
      return expression.Member.Name;
    }
  }
}
