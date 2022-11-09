using DesktopUI2.ViewModels.MappingTool;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DesktopUI2
{
  public class DummyMappingsBindings : MappingsBindings
  {
    public override void SetMappings(string schema, string viewModel)
    {
      throw new NotImplementedException();
    }

    public override List<Type> GetSelectionSchemas()
    {
      var type = typeof(ISchema);
      return Assembly.GetExecutingAssembly().GetTypes().Where(p => type.IsAssignableFrom(p)).ToList();

    }

    public DummyMappingsBindings()
    {

    }
  }
}
