using System;
using System.Collections.Generic;
using System.Text;
using Objects.Structural.Properties.Profiles;
using Speckle.Core.Models;

namespace Objects.BuiltElements.AdvanceSteel
{
  public class AdsSectionProfile : Base
  {
    public string ProfSectionType { get; set; }

    public string ProfSectionName { get; set; }
    
    public AdsSectionProfileDB SectionProfileDB { get; set; }
  }
}
