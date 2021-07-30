using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements.Revit
{
  public class ParameterUpdater : Base
  {
    public string elementId { get; set; }
    public Base parameters { get; set; }


    [SchemaInfo("ParameterUpdater", "Updates parameters on a Revit element by id", "Revit", "Families")]
    public ParameterUpdater([SchemaParamInfo("A Revit ElementId or UniqueId")] string id, List<Parameter> parameters)
    {
      this.elementId = id;
      this.parameters = parameters.ToBase();
    }

    public ParameterUpdater()
    {

    }
  }
}
