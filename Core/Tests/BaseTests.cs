using System;
using NUnit.Framework;
using Speckle.Core.Models;

namespace TestsUnit
{
  [TestFixture]
  public class BaseTests
  {
    [Test(Description = "Checks if validation is performed in property names")]
    public void CanValidatePropNames()
    {
      dynamic @base = new Base();

      // Word chars are OK
      @base["something"] = "B";

      // Only single leading @ allowed
      @base["@something"] = "A";
      Assert.Throws<Exception>(() => { @base["@@@something"] = "Testing"; });

      // Invalid chars:  ./
      Assert.Throws<Exception>(() => { @base["some.thing"] = "Testing"; });
      Assert.Throws<Exception>(() => { @base["some/thing"] = "Testing"; });

      // Trying to change a class member value will throw exceptions.
      //Assert.Throws<Exception>(() => { @base["speckle_type"] = "Testing"; });
      //Assert.Throws<Exception>(() => { @base["id"] = "Testing"; });
    }
  }
}
