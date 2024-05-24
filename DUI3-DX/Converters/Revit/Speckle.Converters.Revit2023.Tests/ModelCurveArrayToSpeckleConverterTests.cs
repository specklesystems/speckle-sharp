using FakeItEasy;
using NUnit.Framework;
using Objects;
using Speckle.Converters.Common.Objects;
using Speckle.Revit2023.Interfaces;

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
  public void Convert()
  {
    var target = A.Fake<IRevitModelCurveCollection>(x => x.Strict());
    var sut = new ModelCurveArrayToSpeckleConverter(
      _revitConversionContextStack,
      _scalingServiceToSpeckle,
      _curveConverter
    );
    sut.Convert(target);
  }
}
