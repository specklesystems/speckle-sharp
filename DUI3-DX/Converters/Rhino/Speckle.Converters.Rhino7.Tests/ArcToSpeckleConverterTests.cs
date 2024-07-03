using FluentAssertions;
using Moq;
using NUnit.Framework;
using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.Rhino7.ToSpeckle.Raw;
using Speckle.Testing;

namespace Speckle.Converters.Rhino7.Tests;

public class ArcToSpeckleConverterTests : MoqTest
{
  [Test]
  public void Convert_ShouldConvertArcCorrectly()
  {
    // Arrange
    var radius = 1.1d;
    var mockPointConverter = Create<ITypedConverter<RG.Point3d, SOG.Point>>();
    var mockPlaneConverter = Create<ITypedConverter<RG.Plane, SOG.Plane>>();
    var mockBoxConverter = Create<ITypedConverter<RG.Box, SOG.Box>>();
    var mockContextStack = Create<IConversionContextStack<RhinoDoc, UnitSystem>>();
    var factory = Create<IBoxFactory>();


    var context = Create<IConversionContext<RhinoDoc>>();
    context.Setup(x => x.SpeckleUnits).Returns("units");
    mockContextStack.Setup(cs => cs.Current).Returns(context.Object);

    var targetArc = Create<RG.Arc>();
    var targetPlane = Create<RG.Plane>();
    var targetBox = Create<RG.Box>();
    var point3d = Create<RG.Point3d>();
    var boundbox = Create<RG.BoundingBox>();

    targetArc.Setup(x => x.Plane).Returns(targetPlane.Object);
    targetArc.Setup(x => x.Radius).Returns(radius);
    targetArc.Setup(x => x.StartAngle).Returns(radius);
    targetArc.Setup(x => x.EndAngle).Returns(radius);
    targetArc.Setup(x => x.Angle).Returns(radius);
    targetArc.Setup(x => x.Length).Returns(radius);
    targetArc.Setup(x => x.StartPoint).Returns(point3d.Object);
    targetArc.Setup(x => x.MidPoint).Returns(point3d.Object);
    targetArc.Setup(x => x.EndPoint).Returns(point3d.Object);
    targetArc.Setup(x => x.BoundingBox()).Returns(boundbox.Object);
    factory.Setup(x => x.Create(boundbox.Object)).Returns(targetBox.Object);
    
    mockPlaneConverter.Setup(pc => pc.Convert(targetPlane.Object)).Returns(new SOG.Plane());
    mockPointConverter.Setup(pc => pc.Convert(It.IsAny<RG.Point3d>())).Returns(new SOG.Point());
    mockBoxConverter.Setup(bc => bc.Convert(targetBox.Object)).Returns(new SOG.Box());

    var converter = new ArcToSpeckleConverter(
      mockPointConverter.Object,
      mockPlaneConverter.Object,
      mockBoxConverter.Object,
      mockContextStack.Object,
      factory.Object
    );

    // Act
    var result = converter.Convert(targetArc.Object);

    // Assert
    result.Should().NotBeNull();
  }
}
