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
        public BE.TeklaStructures.PolygonWelds PoylgonWeldsToSpeckle(PolygonWeld welds)
        {
            var speckleTeklaPolygonWeld = new BE.TeklaStructures.PolygonWelds();

            speckleTeklaPolygonWeld.sizeAbove = welds.SizeAbove;
            speckleTeklaPolygonWeld.sizeBelow = welds.SizeBelow;
            speckleTeklaPolygonWeld.lengthAbove = welds.LengthAbove;
            speckleTeklaPolygonWeld.lengthBelow = welds.LengthBelow;
            speckleTeklaPolygonWeld.pitchAbove = welds.PitchAbove;
            speckleTeklaPolygonWeld.pitchBelow = welds.PitchBelow;
            speckleTeklaPolygonWeld.typeAbove = (BE.TeklaStructures.TeklaWeldType)welds.TypeAbove;
            speckleTeklaPolygonWeld.typeAbove = (BE.TeklaStructures.TeklaWeldType)welds.TypeBelow;
            speckleTeklaPolygonWeld.intermittentType = (BE.TeklaStructures.TeklaWeldIntermittentType)welds.IntermittentType;

            speckleTeklaPolygonWeld.polyline = ToSpecklePolyline(welds.Polygon);

            GetAllUserProperties(speckleTeklaPolygonWeld, welds);

            var solid = welds.GetSolid();
            speckleTeklaPolygonWeld.displayValue = new List<Mesh> { GetMeshFromSolid(solid) };
            return speckleTeklaPolygonWeld;
        }
    }
}
