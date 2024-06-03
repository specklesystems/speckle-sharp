using FakeItEasy;
using NUnit.Framework;
using Objects;
using Objects.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Revit2023.Interfaces;

namespace Speckle.Converters.Revit2023.Tests;

public class LocationConversionToSpeckleTests
{
  private class EmptyLocation : IRevitLocation { }

  private readonly ITypedConverter<IRevitCurve, ICurve> _curveConverter = A.Fake<ITypedConverter<IRevitCurve, ICurve>>(
    x => x.Strict()
  );
  private readonly ITypedConverter<IRevitXYZ, Point> _xyzConverter = A.Fake<ITypedConverter<IRevitXYZ, Point>>(
    x => x.Strict()
  );

  [Test]
  public void Convert_Empty()
  {
    var sut = new LocationConversionToSpeckle(_curveConverter, _xyzConverter);
    Assert.Throws<SpeckleConversionException>(() => sut.Convert(new EmptyLocation()));
  }

  [Test]
  public void Convert_Point()
  {
    var sut = new LocationConversionToSpeckle(_curveConverter, _xyzConverter);
    Assert.Throws<SpeckleConversionException>(() => sut.Convert(new EmptyLocation()));
  }
}
