#if ADVANCESTEEL
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Autodesk.AdvanceSteel.ConstructionTypes;
using Autodesk.AdvanceSteel.Geometry;
using Autodesk.AdvanceSteel.Modelling;
using Objects.BuiltElements;
using ASBeam = Autodesk.AdvanceSteel.Modelling.Beam;
using ASPoint3d = Autodesk.AdvanceSteel.Geometry.Point3d;

using static Autodesk.AdvanceSteel.DotNetRoots.Units.Unit;

namespace Objects.Converter.AutocadCivil
{
  public class BeamProperties : ASBaseProperties<ASBeam>, IASProperties
  {
    public override Dictionary<string, ASProperty> BuildedPropertyList()
    {
      Dictionary<string, ASProperty> dictionary = new Dictionary<string, ASProperty>();

      InsertProperty(dictionary, "profile section name", nameof(ASBeam.ProfSectionName));
      InsertProperty(dictionary, "profile section type", nameof(ASBeam.ProfSectionType));
      InsertProperty(dictionary, "systemline length", nameof(ASBeam.SysLength), eUnitType.kDistance);
      InsertProperty(dictionary, "deviation", nameof(ASBeam.Deviation));
      InsertProperty(dictionary, "shrink value", nameof(ASBeam.ShrinkValue));
      InsertProperty(dictionary, "angle (radians)", nameof(ASBeam.Angle));
      InsertProperty(dictionary, "profile name", nameof(ASBeam.ProfName));
      InsertProperty(dictionary, "run name", nameof(ASBeam.Runname));
      InsertProperty(dictionary, "length", nameof(ASBeam.GetLength), eUnitType.kDistance);
      InsertProperty(dictionary, "weight (per meter)", nameof(ASBeam.GetWeightPerMeter));
      InsertProperty(dictionary, "paint area", nameof(ASBeam.GetPaintArea), eUnitType.kArea);
      InsertProperty(dictionary, "is cross section mirrored", nameof(ASBeam.IsCrossSectionMirrored));
      InsertProperty(dictionary, "reference axis description", nameof(ASBeam.RefAxis));

      InsertCustomProperty(dictionary, "offsets", nameof(BeamProperties.GetOffsets), null, eUnitType.kDistance);
      InsertCustomProperty(dictionary, "start point", nameof(BeamProperties.GetPointAtStart), null);
      InsertCustomProperty(dictionary, "end point", nameof(BeamProperties.GetPointAtEnd), null);
      InsertCustomProperty(dictionary, "weight", nameof(BeamProperties.GetWeightExact), null, eUnitType.kWeight);
      //InsertCustomProperty(dictionary, "weight (exact)", nameof(BeamProperties.GetWeightExact), null, eUnitType.kWeight);
      //InsertCustomProperty(dictionary, "weight (fast)", nameof(BeamProperties.GetWeightFast), null, eUnitType.kWeight);
      InsertCustomProperty(dictionary, "profile type code", nameof(BeamProperties.GetProfileTypeCode), null);
      InsertCustomProperty(dictionary, "profile type", nameof(BeamProperties.GetProfileType), null);
      InsertCustomProperty(dictionary, "saw length", nameof(BeamProperties.GetSawLength), null);
      InsertCustomProperty(dictionary, "flange angle at start", nameof(BeamProperties.GetFlangeAngleAtStart), null);
      InsertCustomProperty(dictionary, "flange angle at end", nameof(BeamProperties.GetFlangeAngleAtEnd), null);
      InsertCustomProperty(dictionary, "web angle at start", nameof(BeamProperties.GetWebAngleAtStart), null);
      InsertCustomProperty(dictionary, "web angle at end", nameof(BeamProperties.GetWebAngleAtEnd), null);

      return dictionary;
    }

    private static ASPoint3d GetPointAtStart(ASBeam beam)
    {
      return beam.GetPointAtStart();
    }

    private static ASPoint3d GetPointAtEnd(ASBeam beam)
    {
      return beam.GetPointAtEnd();
    }

    private static Dictionary<string, double> GetOffsets(ASBeam beam)
    {
      Dictionary<string, double> dictionary = new Dictionary<string, double>
      {
        { "Y", beam.Offsets.x },
        { "Z", beam.Offsets.y }
      };

      return dictionary;
    }

    //private static double GetWeight(ASBeam beam)
    //{
    //  //1 yields the weight, 2 the exact weight
    //  return beam.GetWeight(1);
    //}

    private static double GetWeightExact(ASBeam beam)
    {
      //1 yields the weight, 2 the exact weight
      return beam.GetWeight(2);
    }

    //private static double GetWeightFast(ASBeam beam)
    //{
    //  //3 the fast weight
    //  return beam.GetWeight(3);
    //}

    private static string GetProfileTypeCode(ASBeam beam)
    {
      return beam.GetProfType().GetDSTVValues().GetProfileTypeString();
    }

    private static int GetProfileType(ASBeam beam)
    {
      return (int)beam.GetProfType().GetDSTVValues().DSTVType;
    }

    private static double GetSawLength(ASBeam beam)
    {
      GetSawInformation(beam, out var sawLength, out var flangeAngleAtStart, out var webAngleAtStart, out var flangeAngleAtEnd, out var webAngleAtEnd);
      return sawLength;
    }

    private static double GetFlangeAngleAtStart(ASBeam beam)
    {
      GetSawInformation(beam, out var sawLength, out var flangeAngleAtStart, out var webAngleAtStart, out var flangeAngleAtEnd, out var webAngleAtEnd);
      return DegreeToRadian(flangeAngleAtStart);
    }

    private static double GetWebAngleAtStart(ASBeam beam)
    {
      GetSawInformation(beam, out var sawLength, out var flangeAngleAtStart, out var webAngleAtStart, out var flangeAngleAtEnd, out var webAngleAtEnd);
      return DegreeToRadian(webAngleAtStart);
    }

    private static double GetFlangeAngleAtEnd(ASBeam beam)
    {
      GetSawInformation(beam, out var sawLength, out var flangeAngleAtStart, out var webAngleAtStart, out var flangeAngleAtEnd, out var webAngleAtEnd);
      return DegreeToRadian(flangeAngleAtEnd);
    }

    private static double GetWebAngleAtEnd(ASBeam beam)
    {
      GetSawInformation(beam, out var sawLength, out var flangeAngleAtStart, out var webAngleAtStart, out var flangeAngleAtEnd, out var webAngleAtEnd);
      return DegreeToRadian(webAngleAtEnd);
    }

    private static void GetSawInformation(ASBeam beam, out double sawLength, out double flangeAngleAtStart, out double webAngleAtStart, out double flangeAngleAtEnd, out double webAngleAtEnd)
    {
      int executed = beam.GetSawInformation(out sawLength, out flangeAngleAtStart, out webAngleAtStart, out flangeAngleAtEnd, out webAngleAtEnd);
      //if (executed <= 0)
      //{
      //  throw new System.Exception("No values were found for this steel Beam from Function");
      //}
    }

  }
}
#endif
