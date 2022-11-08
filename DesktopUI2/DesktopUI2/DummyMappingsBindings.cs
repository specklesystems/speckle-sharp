using DesktopUI2.ViewModels.MappingTool;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopUI2
{
  public class DummyMappingsBindings : MappingsBindings
  {
    public override void SetMappings(string schema)
    {
      throw new NotImplementedException();
    }

    public override List<Type> GetSelectionSchemas()
    {
      return new List<Type> { };
    }

    public DummyMappingsBindings()
    {

    }
  }
}
