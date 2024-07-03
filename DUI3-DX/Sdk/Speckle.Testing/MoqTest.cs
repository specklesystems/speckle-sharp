using Moq;
using NUnit.Framework;

namespace Speckle.Testing;

public abstract class MoqTest
{
  [SetUp]
  public void Setup() => Repository = new(MockBehavior.Strict);

  [TearDown]
  public void Verify() => Repository.VerifyAll();

  protected MockRepository Repository { get; private set; } = new(MockBehavior.Strict);

  protected Mock<T> Create<T>()
    where T : class => Repository.Create<T>();
}
