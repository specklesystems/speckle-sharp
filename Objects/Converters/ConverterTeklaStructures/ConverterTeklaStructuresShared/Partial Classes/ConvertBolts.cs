using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using Speckle.Core.Models;
using BE = Objects.BuiltElements;
using System.Linq;
using Tekla.Structures.Model;
using Tekla.Structures.Solid;
using System.Collections;
using StructuralUtilities.PolygonMesher;
using TSG = Tekla.Structures.Geometry3d;

namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures
  {

    public BE.TeklaStructures.Bolts BoltsToSpeckle(BoltGroup Bolts)
    {
      var speckleTeklaBolt= new BE.TeklaStructures.Bolts();
      var units = GetUnitsFromModel();
      speckleTeklaBolt.boltSize = Bolts.BoltSize;
      speckleTeklaBolt.boltStandard = Bolts.BoltStandard;
      speckleTeklaBolt.cutLength = Bolts.CutLength;
        speckleTeklaBolt.coordinates = Bolts.BoltPositions
            .Cast<TSG.Point>()
            .Select(p => new Point(p.X, p.Y, p.Z,units))
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
      speckleTeklaBolt.displayMesh = GetMeshFromSolid(solid);

      return speckleTeklaBolt;
    }
  }
}