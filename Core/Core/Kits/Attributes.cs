using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Core.Kits
{
  [AttributeUsage(AttributeTargets.Class)]
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

  /// <summary>
  /// Used to ignore classes or properties from schema builder, expand objects etc
  /// </summary>
  [AttributeUsage(AttributeTargets.All)]
  public class SchemaIgnoreAttribute : Attribute
  {


  }


}
