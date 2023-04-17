using System;

namespace Speckle.Core.Kits;

[AttributeUsage(AttributeTargets.Constructor)]
public sealed class SchemaInfo : Attribute
{
  public SchemaInfo(string name, string description)
    : this(name, description, null, null) { }

  public SchemaInfo(string name, string description, string category, string subcategory)
  {
    Name = name;
    Description = description;
    Category = category;
    Subcategory = subcategory;
  }

  public string Subcategory { get; }

  public string Category { get; }

  public string Description { get; }

  public string Name { get; }
}

[AttributeUsage(AttributeTargets.Constructor)]
public sealed class SchemaDeprecated : Attribute { }

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class SchemaParamInfo : Attribute
{
  public SchemaParamInfo(string description)
  {
    Description = description;
  }

  public string Description { get; }
}

/// <summary>
/// Used to indicate which is the main input parameter of the schema builder component. Schema info will be attached to this object.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class SchemaMainParam : Attribute { }

// TODO: this could be nuked, as it's only used to hide props on Base,
// which we might want to expose anyways...
/// <summary>
/// Used to ignore properties from expand objects etc
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SchemaIgnore : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public sealed class SchemaComputedAttribute : Attribute
{
  public SchemaComputedAttribute(string name)
  {
    Name = name;
  }

  public string Name { get; }
}
