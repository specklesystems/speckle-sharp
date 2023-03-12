using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;

namespace Objects.BuiltElements.AdvanceSteel
{
  public class AdvanceSteelObject : Base
  {
    public AdvanceSteelObject()
    {
        
    }

    private string __typeAdvanceSteel;

    public override string speckle_type
    {
      get
      {
        if (__typeAdvanceSteel == null)
        {
          __typeAdvanceSteel = this.GetType().Name;
        }
        return __typeAdvanceSteel;
      }
    }

    public Dictionary<int, string> UserAttributes { get; set; }
  }
}
