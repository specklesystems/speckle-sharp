using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Speckle.Core.Kits;

namespace Tests
{
  [TestFixture]
  public class GenericTests
  {
    public static IEnumerable AvailableTypesInKit()
    {
      return KitManager.GetDefaultKit().Types;
    }
    
    [Test(Description = "Checks that all objects inside the Default Kit have empty constructors.")]
    [TestCaseSource(nameof(AvailableTypesInKit))]
    public void ObjectHasEmptyConstructor(Type t)
    {
      var constructor = t.GetConstructor(Type.EmptyTypes);
      Assert.That(constructor, Is.Not.Null);
    }
  }
}