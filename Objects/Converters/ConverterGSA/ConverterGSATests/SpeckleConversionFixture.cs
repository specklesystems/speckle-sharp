using System;
using Speckle.GSA.API;
using Speckle.Core.Kits;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Linq;

namespace ConverterGSATests
{
  public abstract class SpeckleConversionFixture : IDisposable
  {
    protected ISpeckleConverter converter;
    protected GsaModelMock gsaModelMock = new GsaModelMock();
    protected int? highestNodeIndex = null;
    protected List<List<double>> nodeCoords = new List<List<double>>();

    public SpeckleConversionFixture()
    {
      /*  For possible future use
      var gsaModelMock = new Mock<IGSAModel>();
      gsaModelMock.SetupGet(x => x.GwaDelimiter).Returns('\t');
      */
      converter = new ConverterGSA.ConverterGSA();
      Instance.GsaModel = gsaModelMock;
      ((GsaProxyMockForConverterTests)Instance.GsaModel.Proxy).NodeAtFn = (double x, double y, double z) =>
      {
        var newCoords = new List<double> { Math.Round(x, 6), Math.Round(y, 6), Math.Round(z, 6) };
        for (int i = 0; i < nodeCoords.Count(); i++)
        {
          if (newCoords.SequenceEqual(nodeCoords[i]))
          {
            return (i +  1);   //GSA records are base-1
          }
        }
        highestNodeIndex = (highestNodeIndex == null) ? 0 : highestNodeIndex.Value + 1;
        nodeCoords.Add(newCoords);
        return (highestNodeIndex.Value + 1);  //GSA records are base-1
      };
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
