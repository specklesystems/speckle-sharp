using NUnit.Framework;
using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.Rhino7.ToSpeckle.Raw;
using Speckle.Testing;

namespace Speckle.Converters.Rhino7.Tests;

public class EllipseToSpeckleConverterTests: MoqTest
{

  [Test]
  public void Convert_Test()
  {
    var conversionContextStack = Create<IConversionContextStack<RhinoDoc, UnitSystem>>();
    var planeConverter = Create<ITypedConverter<RG.Plane, SOG.Plane>>();
    var boxConverter = Create<ITypedConverter<RG.Box, SOG.Box>>();

    var x = new EllipseToSpeckleConverter(planeConverter.Object, boxConverter.Object, conversionContextStack.Object);
  }
}
