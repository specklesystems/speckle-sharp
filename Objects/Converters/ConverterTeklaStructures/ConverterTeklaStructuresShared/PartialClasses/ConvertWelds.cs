using System.Collections.Generic;
using Objects.Geometry;
using BE = Objects.BuiltElements;
using Tekla.Structures.Model;

namespace Objects.Converter.TeklaStructures;

public partial class ConverterTeklaStructures
{
  public void WeldsToNative(BE.TeklaStructures.Welds welds)
  {
    if (welds is BE.TeklaStructures.PolygonWelds)
    {
      var polygonWeld = welds as BE.TeklaStructures.PolygonWelds;
      PolygonWeld teklaPolyWeld = new();
      SetWeldProperties(teklaPolyWeld, polygonWeld);
      teklaPolyWeld.MainObject = Model.SelectModelObject(new Tekla.Structures.Identifier(polygonWeld.mainObjectId));
      teklaPolyWeld.SecondaryObject = Model.SelectModelObject(
        new Tekla.Structures.Identifier(polygonWeld.secondaryObjectId)
      );

      var polyPoints = polygonWeld.polyline.GetPoints();
      for (int i = 0; i < polyPoints.Count; i++)
      {
        teklaPolyWeld.Polygon.Points.Add(polyPoints[i]);
      }
      teklaPolyWeld.Insert();
    }
    else
    {
      Weld teklaWeld = new();
      SetWeldProperties(teklaWeld, welds);
      teklaWeld.MainObject = Model.SelectModelObject(new Tekla.Structures.Identifier(welds.mainObjectId));
      teklaWeld.SecondaryObject = Model.SelectModelObject(new Tekla.Structures.Identifier(welds.secondaryObjectId));
      teklaWeld.Insert();
    }
  }

  public BE.TeklaStructures.Welds WeldsToSpeckle(Weld welds)
  {
    var speckleTeklaWeld = new BE.TeklaStructures.Welds();

    GetWeldProperties(welds, speckleTeklaWeld);

    speckleTeklaWeld.mainObjectId = welds.MainObject.Identifier.GUID.ToString();
    speckleTeklaWeld.secondaryObjectId = welds.SecondaryObject.Identifier.GUID.ToString();

    GetAllUserProperties(speckleTeklaWeld, welds);

    var solid = welds.GetSolid();
    speckleTeklaWeld.displayValue = new List<Mesh> { GetMeshFromSolid(solid) };
    return speckleTeklaWeld;
  }

  public void GetWeldProperties(BaseWeld baseWeld, BE.TeklaStructures.Welds speckleWeld)
  {
    speckleWeld.sizeAbove = baseWeld.SizeAbove;
    speckleWeld.sizeBelow = baseWeld.SizeBelow;
    speckleWeld.lengthAbove = baseWeld.LengthAbove;
    speckleWeld.lengthBelow = baseWeld.LengthBelow;
    speckleWeld.pitchAbove = baseWeld.PitchAbove;
    speckleWeld.pitchBelow = baseWeld.PitchBelow;
    speckleWeld.angleAbove = baseWeld.AngleAbove;
    speckleWeld.angleBelow = baseWeld.AngleBelow;
    speckleWeld.typeAbove = (BE.TeklaStructures.TeklaWeldType)baseWeld.TypeAbove;
    speckleWeld.typeBelow = (BE.TeklaStructures.TeklaWeldType)baseWeld.TypeBelow;
    speckleWeld.intermittentType = (BE.TeklaStructures.TeklaWeldIntermittentType)baseWeld.IntermittentType;
  }

  public void SetWeldProperties(BaseWeld baseWeld, BE.TeklaStructures.Welds speckleWeld)
  {
    baseWeld.SizeAbove = speckleWeld.sizeAbove;
    baseWeld.SizeBelow = speckleWeld.sizeBelow;
    baseWeld.LengthAbove = speckleWeld.lengthAbove;
    baseWeld.LengthBelow = speckleWeld.lengthBelow;
    baseWeld.PitchAbove = speckleWeld.pitchAbove;
    baseWeld.PitchBelow = speckleWeld.pitchBelow;
    baseWeld.AngleAbove = speckleWeld.angleAbove;
    baseWeld.AngleBelow = speckleWeld.angleBelow;
    baseWeld.TypeAbove = (BaseWeld.WeldTypeEnum)speckleWeld.typeAbove;
    baseWeld.TypeBelow = (BaseWeld.WeldTypeEnum)speckleWeld.typeBelow;
    baseWeld.IntermittentType = (BaseWeld.WeldIntermittentTypeEnum)speckleWeld.intermittentType;
  }
}
