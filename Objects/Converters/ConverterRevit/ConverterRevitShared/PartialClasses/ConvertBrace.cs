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
    public ApplicationObject BraceToNative(Brace speckleBrace)
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

    private Base BraceToSpeckle(DB.FamilyInstance myFamily, out List<string> notes)
    {
      notes = new List<string>();
      var myBeam = BeamToSpeckle(myFamily, out notes) as RevitBeam;

      var myBrace = new RevitBrace()
      {
        applicationId = myBeam.applicationId,
        type = myBeam.type,
        baseLine = myBeam.baseLine,
        level = myBeam.level,
        family = myBeam.family,
        parameters = myBeam.parameters,
        displayValue = myBeam.displayValue,
      };

      var dynamicProps = myBeam.GetMembers(DynamicBaseMemberType.Dynamic);  
      
      foreach (var dp in dynamicProps)
        myBrace[dp.Key] = dp.Value;

      return myBrace;
    }
  }
}
