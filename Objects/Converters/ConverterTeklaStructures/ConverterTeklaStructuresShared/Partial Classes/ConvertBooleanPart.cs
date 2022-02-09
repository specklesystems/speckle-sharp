using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using Speckle.Core.Models;
using BE = Objects.BuiltElements;
using Objects.BuiltElements.TeklaStructures;
using System.Linq;
using Tekla.Structures.Model;
using Tekla.Structures.Solid;
using System.Collections;
using StructuralUtilities.PolygonMesher;

namespace Objects.Converter.TeklaStructures
{
    public partial class ConverterTeklaStructures
    {
        public TeklaOpening BooleanPartToSpeckle(Tekla.Structures.Model.BooleanPart booleanPart)
        {
            TeklaOpening teklaOpening;
            if (booleanPart.OperativePart is ContourPlate)
            {
                var contourOpening = new TeklaContourOpening();
                contourOpening.applicationId = booleanPart.Identifier.GUID.ToString();
                contourOpening.cuttingPlate = AntiContourPlateToSpeckle(booleanPart.OperativePart as ContourPlate);
                contourOpening.cuttingPlate.displayMesh = null;
                contourOpening.openingType = TeklaOpeningTypeEnum.contour;
                (booleanPart.OperativePart as ContourPlate).Contour.CalculatePolygon(out Polygon polygon);
                contourOpening.outline = ToSpecklePolyline(polygon);
                contourOpening.thickness = double.Parse(new string((booleanPart.OperativePart as ContourPlate).Profile.ProfileString.Where(c => char.IsDigit(c)).ToArray()));
                teklaOpening = contourOpening;
            }
            else
            {
                var beamOpening = new TeklaBeamOpening();
                beamOpening.applicationId = booleanPart.Identifier.GUID.ToString();
                beamOpening.cuttingBeam = AntiBeamToSpeckle(booleanPart.OperativePart as Beam);
                teklaOpening = beamOpening;
            }
            return teklaOpening;
        }
    }
}
