using System.Collections;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Raw;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.Revit2023.Tests;

public class ModelCurveArrayToSpeckleConverterTests
{
  private MockRepository _repository;

  private Mock<IRevitConversionContextStack> _revitConversionContextStack;
  private Mock<IScalingServiceToSpeckle> _scalingServiceToSpeckle;
  private Mock<ITypedConverter<DB.Curve, ICurve>> _curveConverter;

  [SetUp]
  public void Start()
  {
    _repository = new(MockBehavior.Strict);
    _revitConversionContextStack = _repository.Create<IRevitConversionContextStack>();
    _scalingServiceToSpeckle = _repository.Create<IScalingServiceToSpeckle>();
    _curveConverter = _repository.Create<ITypedConverter<DB.Curve, ICurve>>();
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
    var array = _repository.Create<DB.ModelCurveArray>();
    array.Setup(x => x.GetEnumerator()).Returns(Enumerable.Empty<object>().GetEnumerator());
    Assert.Throws<SpeckleConversionException>(() => sut.Convert(array.Object));
  }

  [Test]
  public void Convert()
  {
    var endpoint1 = _repository.Create<DB.XYZ>();
    var geometry1 = _repository.Create<DB.Curve>();
    var curve1 = _repository.Create<DB.ModelCurve>();
    curve1.Setup(x => x.GeometryCurve).Returns(geometry1.Object);
    geometry1.Setup(x => x.Length).Returns(2);
    geometry1.Setup(x => x.GetEndPoint(0)).Returns(endpoint1.Object);

    var endpoint2 = _repository.Create<DB.XYZ>();
    var geometry2 = _repository.Create<DB.Curve>();
    var curve2 = _repository.Create<DB.ModelCurve>();
    curve2.Setup(x => x.GeometryCurve).Returns(geometry2.Object);
    geometry2.Setup(x => x.Length).Returns(3);
    geometry2.Setup(x => x.GetEndPoint(1)).Returns(endpoint2.Object);

    var context = _repository.Create<IConversionContext<DB.Document>>();
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
    var array = _repository.Create<DB.ModelCurveArray>();

    array
      .Setup(x => x.GetEnumerator())
      .Returns(new List<DB.ModelCurve> { curve1.Object, curve2.Object }.GetEnumerator());
    var polycurve = sut.Convert(array.Object);

    polycurve.units.Should().Be(units);
    polycurve.closed.Should().BeFalse();
    polycurve.length.Should().Be(scaleLength);
    polycurve.segments.Count.Should().Be(2);
  }
}
