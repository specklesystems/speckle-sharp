using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DUI3
{
  /// <summary>
  /// Meant as a testing ground for various scenarios.
  /// </summary>
  public class TestBinding : IBinding
  {
    public string Name { get; set; } = "testBinding";
    public IBridge Parent { get; set; }

    public string SayHi(string name, int count, bool sayHelloNotHi)
    {
      var baseGreeting = $"{(sayHelloNotHi ? "Hello" : "Hi")} {name}!";
      var finalGreeting = "";
      for (int i = 0; i < Math.Max(1, Math.Abs(count)); i++)
      {
        finalGreeting += baseGreeting + Environment.NewLine;
      }

      return finalGreeting;
    }

    public void ShouldThrow()
    {
      throw new Exception("I am supposed to throw.");
    }

    public void GoAway()
    {
      Debug.WriteLine("Okay, going away.");
    }

    public object GetComplexType()
    {
      return new { Id = GetHashCode().ToString(), count = GetHashCode() };
    }

    public void TriggerEvent(string eventName)
    {
      switch (eventName)
      {
        case "emptyTestEvent":
          Parent.SendToBrowser("emptyTestEvent");
          break;
        case "testEvent":
        default:
          Parent.SendToBrowser("testEvent", new
          {
            IsOk = true,
            Name = "foo",
            Count = 42
          });
          break;
      }
    }
  }
}
