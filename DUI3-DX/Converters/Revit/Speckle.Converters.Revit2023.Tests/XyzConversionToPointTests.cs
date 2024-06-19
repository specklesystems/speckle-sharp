using FluentAssertions;
using Moq;
using NUnit.Framework;
using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Services;
using Speckle.Converters.RevitShared.ToSpeckle;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.Tests;

public class XyzConversionToPointTests
{
  private readonly MockRepository _repository = new(MockBehavior.Strict);

  private readonly Mock<IConversionContextStack<IRevitDocument, IRevitForgeTypeId>> _revitConversionContextStack;
  private readonly Mock<IScalingServiceToSpeckle> _scalingServiceToSpeckle;

  public XyzConversionToPointTests()
  {
    _revitConversionContextStack = _repository.Create<IConversionContextStack<IRevitDocument, IRevitForgeTypeId>>();
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
    var xyz = _repository.Create<IRevitXYZ>();
    xyz.Setup(x => x.X).Returns(x);
    xyz.Setup(x => x.Y).Returns(y);
    xyz.Setup(x => x.Z).Returns(z);

    var units = "units";
    var conversionContext = _repository.Create<IConversionContext<IRevitDocument>>();
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
