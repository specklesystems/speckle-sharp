using FluentAssertions;
using Moq;
using NUnit.Framework;
using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;
using Speckle.Converters.RevitShared.ToSpeckle;

namespace Speckle.Converters.Revit2023.Tests;

public class XyzConversionToPointTests
{
  private MockRepository _repository;

  private Mock<IRevitConversionContextStack> _revitConversionContextStack;
  private Mock<IScalingServiceToSpeckle> _scalingServiceToSpeckle;

  [SetUp]
  public void Setup()
  {
    _repository = new(MockBehavior.Strict);
    _revitConversionContextStack = _repository.Create<IRevitConversionContextStack>();
    _scalingServiceToSpeckle = _repository.Create<IScalingServiceToSpeckle>();
  }

  [TearDown]
  public void Verify() => _repository.VerifyAll();

  [Test]
  public void Convert_Point()
  {
    var x = 3.1;
    var y = 3.2;
    var z = 3.3;
    var xScaled = 4.1;
    var yScaled = 4.2;
    var zScaled = 4.3;
    var xyz = _repository.Create<DB.XYZ>();
    xyz.Setup(x => x.X).Returns(x);
    xyz.Setup(x => x.Y).Returns(y);
    xyz.Setup(x => x.Z).Returns(z);

    var units = "units";
    var conversionContext = _repository.Create<IConversionContext<DB.Document>>();
    conversionContext.Setup(x => x.SpeckleUnits).Returns(units);

    _scalingServiceToSpeckle.Setup(a => a.ScaleLength(x)).Returns(xScaled);
    _scalingServiceToSpeckle.Setup(a => a.ScaleLength(y)).Returns(yScaled);
    _scalingServiceToSpeckle.Setup(a => a.ScaleLength(z)).Returns(zScaled);

    _revitConversionContextStack.Setup(x => x.Current).Returns(conversionContext.Object);

    var converter = new XyzConversionToPoint(_scalingServiceToSpeckle.Object, _revitConversionContextStack.Object);
    var point = converter.Convert(xyz.Object);

    point.x.Should().Be(xScaled);
    point.y.Should().Be(yScaled);
    point.z.Should().Be(zScaled);
    point.units.Should().Be(units);
  }
}
