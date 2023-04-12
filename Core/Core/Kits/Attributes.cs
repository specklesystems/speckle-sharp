using System;

namespace Speckle.Core.Kits;

[AttributeUsage(AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
public class SchemaInfo : Attribute
{
  private string _category;
  private string _description;
  private string _name;
  private string _subcategory;

  public SchemaInfo(string name, string description)
    : this(name, description, null, null) { }

  public SchemaInfo(string name, string description, string category, string subcategory)
  {
    _name = name;
    _description = description;
    _category = category;
    _subcategory = subcategory;
  }

  public virtual string Subcategory => _subcategory;

  public virtual string Category => _category;

  public virtual string Description => _description;

  public virtual string Name => _name;
}

[AttributeUsage(AttributeTargets.Constructor)]
public class SchemaDeprecated : Attribute {}

[AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public class SchemaParamInfo : Attribute
{
  private string _description;

  public SchemaParamInfo(string description)
  {
    _description = description;
  }

  public virtual string Description => _description;
}

/// <summary>
/// Used to indicate which is the main input parameter of the schema builder component. Schema info will be attached to this object.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class SchemaMainParam : Attribute
{
  public SchemaMainParam() { }
}

// TODO: this could be nuked, as it's only used to hide props on Base,
// which we might want to expose anyways...
/// <summary>
/// Used to ignore properties from expand objects etc
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SchemaIgnore : Attribute {}

[AttributeUsage(AttributeTargets.Method)]
public class SchemaComputedAttribute : Attribute
{
  public SchemaComputedAttribute(string name)
  {
    Name = name;
  }

  public virtual string Name { get; }
}