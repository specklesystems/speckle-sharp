using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Organization;

namespace Objects.BuiltElements.Revit
{
  public class ProjectInfo : BIMModelInfo
  {
    public string author { get; set; }
    public string issueDate { get; set; }
    public string organizationDescription { get; set; }
    public string organizationName { get; set; }

  }

}
