using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Core.Kits
{

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

  // TODO: this could be nuked, as it's only used to hide props on Base, 
  // which we might want to expose anyways...
  /// <summary>
  /// Used to ignore properties from expand objects etc
  /// </summary>
  [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
  public class SchemaIgnore : Attribute
  {
  }
}
