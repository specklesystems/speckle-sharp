using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Objects;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Tests
{
  [TestFixture]
  public class GenericTests
  {
    public static IEnumerable AvailableTypesInKit()
    {
      // Get all types in the Objects assembly that inherit from Base
      return Assembly.GetAssembly(typeof(ObjectsKit))
        .GetTypes()
        .Where(t => typeof(Base).IsAssignableFrom(t));
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