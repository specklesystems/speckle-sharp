using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Core.Kits
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
  public class SchemaDescriptionAttribute : Attribute
  {
    private string _description;

    public virtual string Description
    {
      get { return _description; }
    }

    public SchemaDescriptionAttribute(string description)
    {
      _description = description;

    }
  }

  [AttributeUsage(AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
  public class SchemaInfo : Attribute
  {
    private string _description;
    private string _name;

    public virtual string Description
    {
      get { return _description; }
    }

    public virtual string Name
    {
      get { return _name; }
    }

    public SchemaInfo(string name, string description)
    {
      _name = name;
      _description = description;
    }
  }

  [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
  public class SchemaParamInfo : Attribute
  {
    private string _description;

    public virtual string Description
    {
      get { return _description; }
    }

    public SchemaParamInfo(string description)
    {
      _description = description;
    }
  }

  /// <summary>
  /// Used to ignore classes or properties from schema builder, expand objects etc
  /// </summary>
  [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
  public class SchemaIgnoreAttribute : Attribute
  {


  }

  /// <summary>
  /// Used to ignore classes or properties from schema builder, expand objects etc
  /// </summary>
  [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
  public class SchemaOptionalAttribute : Attribute
  {


  }


}
