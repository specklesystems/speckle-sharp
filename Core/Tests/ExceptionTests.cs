using NUnit.Framework;
using Speckle.Core.Models.Extensions;

namespace Tests;

[TestFixture]
public class ExceptionTests
{
  [Test]
  public void CanPrintAllInnerExceptions()
  {
    var ex = new Exception("Some error");
    var exMsg = ex.ToFormattedString();

    var ex2 = new Exception("One or more errors occurred", ex);
    var ex2Msg = ex2.ToFormattedString();

    var ex3 = new AggregateException("One or more errors occurred", ex2);
    var ex3Msg = ex3.ToFormattedString();

    Assert.NotNull(ex3Msg);
  }
}
