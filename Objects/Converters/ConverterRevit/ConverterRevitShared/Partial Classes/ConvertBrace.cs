using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System.Collections.Generic;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public List<ApplicationObject> BraceToNative(Brace speckleBrace)
    {
      //reuse ConversionLog.Addic in Beam class, at these are basically the same thing
      if (speckleBrace is RevitBrace rb)
      {
        var speckleBeam = new RevitBeam
        {
          baseLine = rb.baseLine,
          type = rb.type,
          level = rb.level,
          family = rb.family,
          parameters = rb.parameters,
          applicationId = rb.applicationId,
        };

        return BeamToNative(speckleBeam, StructuralType.Brace);
      }
      else
      {
        var speckleBeam = new Beam();
        speckleBeam.baseLine = speckleBrace.baseLine;
        speckleBeam.applicationId = speckleBrace.applicationId;
        return BeamToNative(speckleBeam, StructuralType.Brace);
      }
    }

    private Base BraceToSpeckle(DB.FamilyInstance myFamily)
    {
      var myBeam = BeamToSpeckle(myFamily) as RevitBeam;

      var myBrace = new RevitBrace()
      {
        type = myBeam.type,
        baseLine = myBeam.baseLine,
        level = myBeam.level,
        family = myBeam.family,
        parameters = myBeam.parameters
      };
      return myBrace;
    }
  }
}