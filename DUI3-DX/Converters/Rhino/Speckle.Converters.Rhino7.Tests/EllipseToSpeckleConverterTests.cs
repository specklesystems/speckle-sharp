using Moq;
using NUnit.Framework;
using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.Tests;

public class EllipseToSpeckleConverterTests
{
  private MockRepository _repository;

  private Mock<IConversionContextStack<RhinoDoc, UnitSystem>> _conversionContextStack;

  private Mock<ITypedConverter<RG.Plane, SOG.Plane>> _planeConverter;
  private Mock<ITypedConverter<RG.Box, SOG.Box>> _boxConverter;

  [SetUp]
  public void Setup()
  {
    _repository = new(MockBehavior.Strict);
    _conversionContextStack = _repository.Create<IConversionContextStack<RhinoDoc, UnitSystem>>();
    _planeConverter = _repository.Create<ITypedConverter<RG.Plane, SOG.Plane>>();
    _boxConverter = _repository.Create<ITypedConverter<RG.Box, SOG.Box>>();
  }

  [TearDown]
  public void Verify() => _repository.VerifyAll();
}
