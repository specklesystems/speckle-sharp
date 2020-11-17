using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements;
using Objects.Revit;
using DB = Autodesk.Revit.DB;
using Element = Objects.BuiltElements.Element;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public DB.Element BraceToNative(Brace speckleBrace)
    {
      //reuse logic in Beam class, at these are basically the same thing
      if (speckleBrace is RevitBrace rb)
      {
        var speckleBeam = new RevitBeam
        {
          baseLine = rb.baseLine,
          type = rb.type,
          level = rb.level,
          family = rb.family,
          parameters = rb.parameters,
          typeParameters = rb.typeParameters
        };

        return BeamToNative(speckleBeam, StructuralType.Brace);
      }
      else
      {
        var speckleBeam = new Beam();
        speckleBeam.baseLine = speckleBrace.baseLine;
        return BeamToNative(speckleBeam, StructuralType.Brace);
      }
    }

    private IRevitElement BraceToSpeckle(DB.FamilyInstance myFamily)
    {
      var myBeam = BeamToSpeckle(myFamily) as RevitBeam;

      var myBrace = new RevitBrace()
      {
        type = myBeam.type,
        baseLine = myBeam.baseLine,
        level = myBeam.level,
        family = myBeam.family,
        parameters = myBeam.parameters,
        typeParameters = myBeam.typeParameters
      };
      return myBrace;
    }
  }
}