using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Core.Kits
{
  [AttributeUsage(AttributeTargets.Class)]
  public class SchemaBuilderAttribute : Attribute
  {
    private string _description;

    public virtual string Description
    {
      get { return _description; }
    }

    public SchemaBuilderAttribute(string description)
    {
      _description = description;

    }
  }

  [AttributeUsage(AttributeTargets.All)]
  public class SchemaBuilderIgnoreAttribute : Attribute
  {
  }
}
