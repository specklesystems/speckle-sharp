using System.Collections.Generic;
using Objects.Geometry;
using BE = Objects.BuiltElements;
using System.Linq;
using Tekla.Structures.Model;
using TSG = Tekla.Structures.Geometry3d;

namespace Objects.Converter.TeklaStructures;

public partial class ConverterTeklaStructures
{
  public void BoltsToNative(BE.TeklaStructures.Bolts bolts)
  {
    switch (bolts)
    {
      case BE.TeklaStructures.BoltsArray ba:
      {
        BoltArray teklaBoltArray = new();
        SetBoltGroupProperties(teklaBoltArray, ba);
        for (int i = 0; i < ba.xDistance.Count; i++)
        {
          teklaBoltArray.AddBoltDistX(ba.xDistance[i]);
        }
        for (int i = 0; i < ba.yDistance.Count; i++)
        {
          teklaBoltArray.AddBoltDistY(ba.yDistance[i]);
        }
        teklaBoltArray.Insert();
        return;
      }
      case BE.TeklaStructures.BoltsXY bx:
      {
        BoltXYList teklaBoltXY = new();
        SetBoltGroupProperties(teklaBoltXY, bx);
        for (int i = 0; i < bx.xPosition.Count; i++)
        {
          teklaBoltXY.AddBoltDistX(bx.xPosition[i]);
        }
        for (int i = 0; i < bx.yPosition.Count; i++)
        {
          teklaBoltXY.AddBoltDistY(bx.yPosition[i]);
        }
        teklaBoltXY.Insert();
        return;
      }
      case BE.TeklaStructures.BoltsCircle bc:
      {
        BoltCircle teklaBoltCircle = new();
        SetBoltGroupProperties(teklaBoltCircle, bc);
        teklaBoltCircle.NumberOfBolts = bc.boltCount;
        teklaBoltCircle.Diameter = bc.diameter;
        teklaBoltCircle.Insert();
        return;
      }
    }
  }

  public BE.TeklaStructures.Bolts BoltsToSpeckle(BoltGroup Bolts)
  {
    BE.TeklaStructures.Bolts speckleTeklaBolt;
    var units = GetUnitsFromModel();

    // Add specific Tekla necessary properties for the different types of bolts possible
    switch (Bolts)
    {
      case BoltArray ba:
        var speckleBoltArray = new BE.TeklaStructures.BoltsArray();

        speckleBoltArray.xDistance = new List<double>();
        speckleBoltArray.yDistance = new List<double>();
        for (int i = 0; i < ba.GetBoltDistXCount(); i++)
        {
          speckleBoltArray.xDistance.Add(ba.GetBoltDistX(i));
        }
        for (int i = 0; i < ba.GetBoltDistYCount(); i++)
        {
          speckleBoltArray.yDistance.Add(ba.GetBoltDistY(i));
        }
        speckleTeklaBolt = speckleBoltArray;
        break;
      case BoltXYList bxy:
        var speckleBoltXY = new BE.TeklaStructures.BoltsXY();
        speckleBoltXY.xPosition = new List<double>();
        speckleBoltXY.yPosition = new List<double>();
        for (int i = 0; i < bxy.GetBoltDistXCount(); i++)
        {
          speckleBoltXY.xPosition.Add(bxy.GetBoltDistX(i));
        }
        for (int i = 0; i < bxy.GetBoltDistYCount(); i++)
        {
          speckleBoltXY.yPosition.Add(bxy.GetBoltDistY(i));
        }
        speckleTeklaBolt = speckleBoltXY;
        break;
      case BoltCircle bc:
        var speckleBoltCircle = new BE.TeklaStructures.BoltsCircle();
        speckleBoltCircle.boltCount = (int)bc.NumberOfBolts;
        speckleBoltCircle.diameter = bc.Diameter;
        speckleTeklaBolt = speckleBoltCircle;
        break;
      default:
        speckleTeklaBolt = new BE.TeklaStructures.Bolts();
        break;
    }

    //Set common properties
    speckleTeklaBolt.boltSize = Bolts.BoltSize;
    speckleTeklaBolt.boltStandard = Bolts.BoltStandard;
    speckleTeklaBolt.cutLength = Bolts.CutLength;
    speckleTeklaBolt.length = Bolts.Length;
    speckleTeklaBolt.position = GetPositioning(Bolts.Position);

    // global bolt coordinates
    speckleTeklaBolt.coordinates = Bolts.BoltPositions
      .Cast<TSG.Point>()
      .Select(p => new Point(p.X, p.Y, p.Z, units))
      .ToList();

    // Add bolted parts necessary for insertion into Tekla
    speckleTeklaBolt.boltedPartsIds.Add(Bolts.PartToBeBolted.Identifier.GUID.ToString());
    speckleTeklaBolt.boltedPartsIds.Add(Bolts.PartToBoltTo.Identifier.GUID.ToString());
    if (Bolts.OtherPartsToBolt.Count > 0)
    {
      foreach (Part otherPart in Bolts.OtherPartsToBolt.Cast<Part>())
      {
        speckleTeklaBolt.boltedPartsIds.Add(otherPart.Identifier.GUID.ToString());
      }
    }
    GetAllUserProperties(speckleTeklaBolt, Bolts);

    var solid = Bolts.GetSolid();
    speckleTeklaBolt.displayValue = new List<Mesh> { GetMeshFromSolid(solid) };

    return speckleTeklaBolt;
  }

  public void SetBoltGroupProperties(BoltGroup boltGroup, BE.TeklaStructures.Bolts bolts)
  {
    boltGroup.PartToBeBolted =
      Model.SelectModelObject(new Tekla.Structures.Identifier(bolts.boltedPartsIds[0])) as Part;
    boltGroup.PartToBoltTo = Model.SelectModelObject(new Tekla.Structures.Identifier(bolts.boltedPartsIds[1])) as Part;
    if (bolts.boltedPartsIds.Count > 2)
    {
      for (int i = 2; i < bolts.boltedPartsIds.Count; i++)
      {
        boltGroup.AddOtherPartToBolt(
          Model.SelectModelObject(new Tekla.Structures.Identifier(bolts.boltedPartsIds[i])) as Part
        );
      }
    }
    boltGroup.FirstPosition = new TSG.Point(bolts.firstPosition.x, bolts.firstPosition.y, bolts.firstPosition.z);
    boltGroup.SecondPosition = new TSG.Point(bolts.secondPosition.x, bolts.secondPosition.y, bolts.secondPosition.z);
    boltGroup.BoltSize = bolts.boltSize;
    boltGroup.Tolerance = bolts.tolerance;
    boltGroup.BoltStandard = bolts.boltStandard;
    boltGroup.CutLength = bolts.cutLength;
    boltGroup.Position = SetPositioning(bolts.position);
  }
}
