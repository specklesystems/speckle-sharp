using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;

namespace Objects.Organization
{
  public class ModelInfo :Base
  {

  public string description { get; set; }
  public string ProjectOwner { get; set; }
  public string ProjectName { get; set; }
  public string ProjectDescription { get; set; }
  public string units { get; set; } // this should be set universally so we aren't sending it one per every object. It's a valid assumption that if they're sending it from Revit, it should all come through with that unit. 

  }
}
