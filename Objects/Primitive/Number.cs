using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Primitive
{
  public class Number : Base
  {
    public double? value { get; set; }

    public Number() { }

    public Number(double value)
    {
      this.value = value;
    }

    public static implicit operator double?(Number n)
    {
      return n.value;
    }

    public static implicit operator Number(double n)
    {
      return new Number(n);
    }
  }
}
