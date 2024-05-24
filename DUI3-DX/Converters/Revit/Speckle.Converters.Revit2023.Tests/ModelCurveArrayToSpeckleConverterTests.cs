using FakeItEasy;
using NUnit.Framework;
using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Revit2023.Interfaces;
using Shouldly;

namespace Speckle.Converters.Revit2023.Tests;

public class ModelCurveArrayToSpeckleConverterTests
{
  private readonly IRevitConversionContextStack _revitConversionContextStack = A.Fake<IRevitConversionContextStack>(
    x => x.Strict()
  );
  private readonly IScalingServiceToSpeckle _scalingServiceToSpeckle = A.Fake<IScalingServiceToSpeckle>(
    x => x.Strict()
  );
  private readonly ITypedConverter<IRevitCurve, ICurve> _curveConverter = A.Fake<ITypedConverter<IRevitCurve, ICurve>>(
    x => x.Strict()
  );

  [Test]
  public void Convert_Empty()
  {
    var sut = new ModelCurveArrayToSpeckleConverter(
      _revitConversionContextStack,
      _scalingServiceToSpeckle,
      _curveConverter
    );
    Assert.Throws<SpeckleConversionException>(() => sut.Convert(new List<IRevitModelCurve>()));
  }

  [Test]
  public void Convert()
  {
    var endpoint1 = A.Fake<IRevitXYZ>(x => x.Strict());
    var geometry1 = A.Fake<IRevitCurve>();
    var curve1 = A.Fake<IRevitModelCurve>(x => x.Strict());
    A.CallTo(() => curve1.GeometryCurve).Returns(geometry1);
    A.CallTo(() => geometry1.Length).Returns(2);
    A.CallTo(() => geometry1.GetEndPoint(0)).Returns(endpoint1);

    var endpoint2 = A.Fake<IRevitXYZ>(x => x.Strict());
    var geometry2 = A.Fake<IRevitCurve>();
    var curve2 = A.Fake<IRevitModelCurve>(x => x.Strict());
    A.CallTo(() => curve2.GeometryCurve).Returns(geometry2);
    A.CallTo(() => geometry2.Length).Returns(3);
    A.CallTo(() => geometry2.GetEndPoint(1)).Returns(endpoint2);

    var context = A.Fake<IConversionContext<IRevitDocument>>();
    A.CallTo(() => _revitConversionContextStack.Current).Returns(context);

    var units = "units";
    A.CallTo(() => context.SpeckleUnits).Returns(units);

    var scaleLength = 2.2;
    A.CallTo(() => _scalingServiceToSpeckle.ScaleLength(2 + 3)).Returns(scaleLength);

    A.CallTo(() => endpoint1.DistanceTo(endpoint2)).Returns(4.4);

    A.CallTo(() => _curveConverter.Convert(geometry1)).Returns(A.Fake<ICurve>());
    A.CallTo(() => _curveConverter.Convert(geometry2)).Returns(A.Fake<ICurve>());

    var sut = new ModelCurveArrayToSpeckleConverter(
      _revitConversionContextStack,
      _scalingServiceToSpeckle,
      _curveConverter
    );
    var polycurve = sut.Convert(new List<IRevitModelCurve>() { curve1, curve2 });

    polycurve.units.ShouldBe(units);
    polycurve.closed.ShouldBeFalse();
    polycurve.length.ShouldBe(scaleLength);
    polycurve.segments.Count.ShouldBe(2);
  }
}
