using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Tests.Unit;

[TestFixture]
public class GenericTests
{
  public static IEnumerable<Type> AvailableTypesInKit()
  {
    // Get all types in the Objects assembly that inherit from Base
    return Assembly.GetAssembly(typeof(ObjectsKit)).GetTypes().Where(t => typeof(Base).IsAssignableFrom(t));
  }

  public static IEnumerable<Type> NonAbstractTypesInKit()
  {
    return AvailableTypesInKit().Where(t => !t.IsAbstract);
  }

  [
    Test(Description = "Checks that all objects inside the Default Kit have empty constructors."),
    TestCaseSource(nameof(NonAbstractTypesInKit))
  ]
  public void ObjectHasEmptyConstructor(Type t)
  {
    var constructor = t.GetConstructor(Type.EmptyTypes);
    Assert.That(constructor, Is.Not.Null);
  }

  [
    Test(
      Description = "Checks that all methods with the 'SchemaComputed' attribute inside the Default Kit have no parameters."
    ),
    TestCaseSource(nameof(AvailableTypesInKit))
  ]
  public void SchemaComputedMethod_CanBeCalledWithNoParameters(Type t)
  {
    t.GetMethods()
      .Where(m => m.IsDefined(typeof(SchemaComputedAttribute)))
      .ToList()
      .ForEach(m =>
      {
        // Check if all parameters are optional.
        // This allows for other methods to be used as long as they can be called empty.
        // But also covers the basic case of having no parameters in the first place.
        Assert.That(m.GetParameters().All(p => p.IsOptional), Is.True);
      });
  }
}
