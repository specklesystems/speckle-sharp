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
      speckleTeklaBolt.BoltSize = Bolts.BoltSize;
      speckleTeklaBolt.BoltStandard = Bolts.BoltStandard;
      speckleTeklaBolt.CutLength = Bolts.CutLength;
        speckleTeklaBolt.Coordinates = Bolts.BoltPositions
            .Cast<TSG.Point>()
            .Select(p => new Point(p.X, p.Y, p.Z,units))
            .ToList();
      
      var solid = Bolts.GetSolid();
      speckleTeklaBolt.displayMesh = GetMeshFromSolid(solid);


      return speckleTeklaBolt;
    }
  }
}