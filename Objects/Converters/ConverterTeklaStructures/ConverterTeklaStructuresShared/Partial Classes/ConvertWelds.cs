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
        public BE.TeklaStructures.Welds WeldsToSpeckle(Weld welds)
        {
            var speckleTeklaWeld = new BE.TeklaStructures.Welds();

            speckleTeklaWeld.sizeAbove = welds.SizeAbove;
            speckleTeklaWeld.sizeBelow = welds.SizeBelow;
            speckleTeklaWeld.lengthAbove = welds.LengthAbove;
            speckleTeklaWeld.lengthBelow = welds.LengthBelow;
            speckleTeklaWeld.pitchAbove = welds.PitchAbove;
            speckleTeklaWeld.pitchBelow = welds.PitchBelow;
            speckleTeklaWeld.angleAbove = welds.AngleAbove;
            speckleTeklaWeld.angleBelow = welds.AngleBelow;
            speckleTeklaWeld.typeAbove = (BE.TeklaStructures.TeklaWeldType)welds.TypeAbove;
            speckleTeklaWeld.typeBelow = (BE.TeklaStructures.TeklaWeldType)welds.TypeBelow;
            speckleTeklaWeld.intermittentType = (BE.TeklaStructures.TeklaWeldIntermittentType)welds.IntermittentType;

            GetAllUserProperties(speckleTeklaWeld, welds);

            var solid = welds.GetSolid();
            speckleTeklaWeld.displayMesh = GetMeshFromSolid(solid);
            return speckleTeklaWeld;
        }
    }
}
