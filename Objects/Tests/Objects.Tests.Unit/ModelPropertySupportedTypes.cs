using System;
using System.Collections.Generic;
using System.DoubleNumerics;
using System.Drawing;
using System.Linq;
using NUnit.Framework;
using Speckle.Core.Models;
using Speckle.Core.Serialisation;
using Speckle.Newtonsoft.Json;

namespace Objects.Tests.Unit;

/// <summary>
/// Tests that all Base object models in the kit have properties that are an allowed type
/// This test is not exhaustive, there are plenty of generic arg combinations that will pass this test,
/// but still not work / are not defined behaviour. This test will just catch many types that definitely won't work
/// </summary>
public class ModelPropertySupportedTypes
{
  /// <summary>
  /// Set of types that we support in Base objects
  /// If it's not in the list, or is commented out, it's not supported by our serializer!
  /// </summary>
  /// <remarks>
  /// If you're tempted to add to this list, please ensure both our serializer and deserializer support properties of this type
  /// Check the <see cref="Speckle.Core.Serialisation.SerializationUtilities.ValueConverter"/>
  /// Check the <see cref="BaseObjectSerializerV2"/>
  /// (or is an interface where all concrete types are supported)
  /// You should also consider adding a test in SerializerNonBreakingChanges
  /// </remarks>
  private readonly HashSet<Type> _allowedTypes =
    new()
    {
      typeof(Boolean),
      typeof(Byte),
      typeof(UInt32),
      typeof(UInt64),
      typeof(Int16),
      typeof(Int32),
      typeof(Int64),
      //typeof(Half),
      typeof(Single),
      typeof(Double),
      typeof(Char),
      typeof(string),
      typeof(DateTime),
      typeof(Guid),
      typeof(Color),
      typeof(List<>),
      typeof(Nullable<>),
      typeof(IList<>),
      typeof(IReadOnlyList<>),
      typeof(Dictionary<,>),
      //typeof(IDictionary<,>),
      //typeof(IReadOnlyDictionary<,>),
      typeof(ICurve),
      typeof(Object),
      typeof(Matrix4x4),
    };

  [Test]
  [TestCaseSource(typeof(GenericTests), nameof(GenericTests.AvailableTypesInKit))]
  public void TestObjects(Type t)
  {
    var members = DynamicBase.GetInstanceMembers(t).Where(p => !p.IsDefined(typeof(JsonIgnoreAttribute), true));

    foreach (var prop in members)
    {
      if (prop.PropertyType.IsAssignableTo(typeof(Base)))
        continue;
      if (prop.PropertyType.IsEnum)
        continue;
      if (prop.PropertyType.IsSZArray)
        continue;

      Type propType = prop.PropertyType;
      Type typeDef = propType.IsGenericType ? propType.GetGenericTypeDefinition() : propType;
      Assert.That(_allowedTypes, Does.Contain(typeDef), $"{typeDef} was not in allowedTypes");
    }
  }
}
