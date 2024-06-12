using FluentAssertions;
using Moq;
using NUnit.Framework;
using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Services;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.Tests;

public class ModelCurveArrayToSpeckleConverterTests
{
  private readonly MockRepository _repository = new(MockBehavior.Strict);

  private readonly Mock<IConversionContextStack<IRevitDocument, IRevitForgeTypeId>> _revitConversionContextStack;
  private readonly Mock<IScalingServiceToSpeckle> _scalingServiceToSpeckle;
  private readonly Mock<ITypedConverter<IRevitCurve, ICurve>> _curveConverter;

  public ModelCurveArrayToSpeckleConverterTests()
  {
    _revitConversionContextStack = _repository.Create<IConversionContextStack<IRevitDocument, IRevitForgeTypeId>>();
    _scalingServiceToSpeckle = _repository.Create<IScalingServiceToSpeckle>();
    _curveConverter = _repository.Create<ITypedConverter<IRevitCurve, ICurve>>();
  }

  [TearDown]
  public void Verify() => _repository.VerifyAll();

  [Test]
  public void Convert_Empty()
  {
    var sut = new ModelCurveArrayToSpeckleConverter(
      _revitConversionContextStack.Object,
      _scalingServiceToSpeckle.Object,
      _curveConverter.Object
    );
    Assert.Throws<SpeckleConversionException>(() => sut.Convert(new List<IRevitModelCurve>()));
  }

  [Test]
  public void Convert()
  {
    var endpoint1 = _repository.Create<IRevitXYZ>();
    var geometry1 = _repository.Create<IRevitCurve>();
    var curve1 = _repository.Create<IRevitModelCurve>();
    curve1.Setup(x => x.GeometryCurve).Returns(geometry1.Object);
    geometry1.Setup(x => x.Length).Returns(2);
    geometry1.Setup(x => x.GetEndPoint(0)).Returns(endpoint1.Object);

    var endpoint2 = _repository.Create<IRevitXYZ>();
    var geometry2 = _repository.Create<IRevitCurve>();
    var curve2 = _repository.Create<IRevitModelCurve>();
    curve2.Setup(x => x.GeometryCurve).Returns(geometry2.Object);
    geometry2.Setup(x => x.Length).Returns(3);
    geometry2.Setup(x => x.GetEndPoint(1)).Returns(endpoint2.Object);

    var context = _repository.Create<IConversionContext<IRevitDocument>>();
    _revitConversionContextStack.Setup(x => x.Current).Returns(context.Object);

    var units = "units";
    context.Setup(x => x.SpeckleUnits).Returns(units);

    var scaleLength = 2.2;
    _scalingServiceToSpeckle.Setup(x => x.ScaleLength(2 + 3)).Returns(scaleLength);

    endpoint1.Setup(x => x.DistanceTo(endpoint2.Object)).Returns(4.4);

    _curveConverter.Setup(x => x.Convert(geometry1.Object)).Returns(_repository.Create<ICurve>().Object);
    _curveConverter.Setup(x => x.Convert(geometry2.Object)).Returns(_repository.Create<ICurve>().Object);

    var sut = new ModelCurveArrayToSpeckleConverter(
      _revitConversionContextStack.Object,
      _scalingServiceToSpeckle.Object,
      _curveConverter.Object
    );
    var polycurve = sut.Convert(new List<IRevitModelCurve>() { curve1.Object, curve2.Object });

    polycurve.units.Should().Be(units);
    polycurve.closed.Should().BeFalse();
    polycurve.length.Should().Be(scaleLength);
    polycurve.segments.Count.Should().Be(2);
  }
}
