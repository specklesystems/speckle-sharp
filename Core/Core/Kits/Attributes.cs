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

  [AttributeUsage(AttributeTargets.All)]
  public class SchemaVisibilityAttribute : Attribute
  {
    private Visibility _visibility;

    public virtual Visibility Visibility
    {
      get { return _visibility; }
    }

    public SchemaVisibilityAttribute(Visibility visibility)
    {
      _visibility = visibility;
    }
  }

  public enum Visibility
  {
    Hidden,
    Internal,
    Visible
  }
}
