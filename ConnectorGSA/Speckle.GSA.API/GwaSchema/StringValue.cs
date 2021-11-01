using System;

namespace Speckle.GSA.API.GwaSchema
{
  public class StringValue : Attribute
  {
    public string Value { get; protected set; }

    public StringValue(string v)
    {
      Value = v;
    }
  }
}
