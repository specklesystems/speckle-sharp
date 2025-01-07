using System.Linq;
using Objects.BuiltElements.TeklaStructures;
using Objects.Geometry;
using Tekla.Structures.Model;
using BE = Objects.BuiltElements;

namespace Objects.Converter.TeklaStructures;

public partial class ConverterTeklaStructures
{
  public void BooleanPartToNative(BE.Opening opening)
  {
    BooleanPart booleanPart = new();
    switch (opening)
    {
      case TeklaContourOpening contourOpening:
        {
          var contourPlate = new ContourPlate();
          contourPlate.Profile.ProfileString = contourOpening.cuttingPlate.profile.name;
          contourPlate.Material.MaterialString = contourOpening.cuttingPlate.material.name;
          contourPlate.Class = BooleanPart.BooleanOperativeClassName;
          contourPlate.Position = SetPositioning(contourOpening.cuttingPlate.position);
          for (int i = 0; i < contourOpening.cuttingPlate.contour.Count; i++)
          {
            TeklaContourPoint cp = contourOpening.cuttingPlate.contour[i];
            contourPlate.AddContourPoint(ToTeklaContourPoint(cp));
          }
          contourPlate.Insert();
          booleanPart.SetOperativePart(contourPlate);
          booleanPart.Father = Model.SelectModelObject(new Tekla.Structures.Identifier(contourOpening.openingHostId));
          booleanPart.Insert();
          contourPlate.Delete();
        }
        break;
      case TeklaBeamOpening beamOpening:
        {
          var beam = new Beam();
          var baseLine = beamOpening.cuttingBeam.baseLine as Line;
          beam.StartPoint = new Tekla.Structures.Geometry3d.Point(baseLine.start.x, baseLine.start.y, baseLine.start.z);
          beam.EndPoint = new Tekla.Structures.Geometry3d.Point(baseLine.start.x, baseLine.start.y, baseLine.start.z);

          beam.Profile.ProfileString = beamOpening.cuttingBeam.profile.name;
          beam.Material.MaterialString = beamOpening.cuttingBeam.material.name;
          beam.Class = BooleanPart.BooleanOperativeClassName;
          beam.Position = SetPositioning(beamOpening.cuttingBeam.position);
          beam.Insert();
          booleanPart.SetOperativePart(beam);
          booleanPart.Father = Model.SelectModelObject(new Tekla.Structures.Identifier(beamOpening.openingHostId));
          booleanPart.Insert();
          beam.Delete();
        }
        break;
    }
  }

  public TeklaOpening BooleanPartToSpeckle(Tekla.Structures.Model.BooleanPart booleanPart)
  {
    TeklaOpening teklaOpening;
    if (booleanPart.OperativePart is ContourPlate)
    {
      var contourOpening = new TeklaContourOpening();
      contourOpening.applicationId = booleanPart.Identifier.GUID.ToString();
      contourOpening.openingHostId = booleanPart.Father.Identifier.GUID.ToString();
      contourOpening.cuttingPlate = AntiContourPlateToSpeckle(booleanPart.OperativePart as ContourPlate);
      contourOpening.openingType = TeklaOpeningTypeEnum.contour;
      contourOpening.outline = ToSpecklePolycurve((booleanPart.OperativePart as ContourPlate).GetContourPolycurve());
      contourOpening.thickness = double.Parse(
        new string(
          (booleanPart.OperativePart as ContourPlate).Profile.ProfileString.Where(c => char.IsDigit(c)).ToArray()
        )
      );
      teklaOpening = contourOpening;
    }
    else
    {
      var beamOpening = new TeklaBeamOpening();
      beamOpening.applicationId = booleanPart.Identifier.GUID.ToString();
      beamOpening.openingHostId = booleanPart.Father.Identifier.GUID.ToString();
      beamOpening.openingType = TeklaOpeningTypeEnum.beam;
      beamOpening.cuttingBeam = AntiBeamToSpeckle(booleanPart.OperativePart as Beam);
      teklaOpening = beamOpening;
    }
    return teklaOpening;
  }
}
