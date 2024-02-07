namespace Objects.BuiltElements.TeklaStructures;

public class TeklaOpening : Opening
{
  public string openingHostId { get; set; }
  public TeklaOpeningTypeEnum openingType { get; set; }
}

public class TeklaContourOpening : TeklaOpening
{
  public TeklaContourPlate cuttingPlate { get; set; }
  public double thickness { get; set; }
}

public class TeklaBeamOpening : TeklaOpening
{
  public TeklaBeam cuttingBeam { get; set; }
}
