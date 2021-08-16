namespace Speckle.GSA.API.GwaSchema
{
  //The term "section component" here is a name applied to both the group as a whole as well as one member of the group, 
  //but the latter is shortened to SectionComp to distinguish them here
  public class SectionSteel : GsaSectionComponentBase
  {
    //The GWA specifies ref (i.e. record index) and name, but when a SECTION_COMP is inside a SECTION command, 
    //the ref is absent and name is blank (empty string) - so they'll be left out here
    public int? GradeIndex;
    public double? PlasElas;
    public double? NetGross;
    public double? Exposed;
    public double? Beta;
    public SectionSteelSectionType Type;
    public SectionSteelPlateType Plate;
    public bool Locked;


    public SectionSteel() : base()
    {
      Version = 2;
    }
  }

}
