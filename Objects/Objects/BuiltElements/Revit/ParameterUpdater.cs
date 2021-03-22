using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements.Revit
{
  public class ParameterUpdater : Base
  {
    public string revitId { get; set; }
    public List<Parameter> parameters { get; set; }


    [SchemaInfo("ParameterUpdater", "Updates parameters on a Revit element by id")]
    public ParameterUpdater([SchemaParamInfo("A Revit ElementId or UniqueId")] string id, List<Parameter> parameters)
    {
      this.revitId = id;
      this.parameters = parameters;
    }

    public ParameterUpdater()
    {

    }
  }
}
