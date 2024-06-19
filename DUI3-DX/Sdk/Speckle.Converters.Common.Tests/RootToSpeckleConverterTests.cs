using FluentAssertions;
using Moq;
using NUnit.Framework;
using Speckle.Core.Models;

namespace Speckle.Converters.Common.Tests;

public class RootToSpeckleConverterTests
{
  private readonly MockRepository _repository = new(MockBehavior.Strict);

  private readonly Mock<IRootConvertManager> _rootConvertManager;
  private readonly Mock<IProxyMapper> _proxyMapper;
  private readonly Mock<IRootElementProvider> _rootElementProvider;

  public RootToSpeckleConverterTests()
  {
    _rootConvertManager = _repository.Create<IRootConvertManager>();
    _proxyMapper = _repository.Create<IProxyMapper>();
    _rootElementProvider = _repository.Create<IRootElementProvider>();
  }

  [TearDown]
  public void Verify() => _repository.VerifyAll();

  [Test]
  public void Convert_BaseType()
  {
    try
    {
      Type baseType = new FakeType("baseType");
      Type hostType = new FakeType("hostType");

      object target = new();
      Type targetType = new FakeType("targetType");

      object wrappedTarget = new();
      Base converted = new();

      _rootConvertManager.Setup(x => x.GetTargetType(target)).Returns(targetType);
      _rootElementProvider.Setup(x => x.GetRootType()).Returns(baseType);
      _proxyMapper.Setup(x => x.GetHostTypeFromMappedType(baseType)).Returns(hostType);

      _proxyMapper.Setup(x => x.GetMappedTypeFromHostType(targetType)).Returns((Type?)null);
      _proxyMapper.Setup(x => x.GetMappedTypeFromProxyType(targetType)).Returns((Type?)null);

      _rootConvertManager.Setup(x => x.IsSubClass(baseType, targetType)).Returns(true);
      _proxyMapper.Setup(x => x.CreateProxy(baseType, target)).Returns(wrappedTarget);
      _rootConvertManager.Setup(x => x.Convert(baseType, wrappedTarget)).Returns(converted);

      var rootToSpeckleConverter = new RootToSpeckleConverter(
        _proxyMapper.Object,
        _rootConvertManager.Object,
        _rootElementProvider.Object
      );
      var testConverted = rootToSpeckleConverter.Convert(target);

      testConverted.Should().BeSameAs(converted);
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      throw;
    }
  }
}
