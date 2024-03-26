using NUnit.Framework;
using Speckle.Core.Models.Extensions;

namespace Speckle.Core.Tests.Unit.Models.Extensions;

[TestFixture]
[TestOf(typeof(BaseExtensions))]
public class ExceptionTests
{
  [Test]
  public void CanPrintAllInnerExceptions()
  {
    var ex = new Exception("Some error");
    var exMsg = ex.ToFormattedString();
    Assert.That(exMsg, Is.Not.Null);

    var ex2 = new Exception("One or more errors occurred", ex);
    var ex2Msg = ex2.ToFormattedString();
    Assert.That(ex2Msg, Is.Not.Null);

    var ex3 = new AggregateException("One or more errors occurred", ex2);
    var ex3Msg = ex3.ToFormattedString();

    Assert.That(ex3Msg, Is.Not.Null);
  }
}
