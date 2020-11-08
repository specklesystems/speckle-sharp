using Autodesk.Revit.DB.Structure;
using Objects.Revit;
using DB = Autodesk.Revit.DB;
using Element = Objects.BuiltElements.Element;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public DB.Element BraceToNative(RevitBrace myBrace)
    {
      var myBeam = new RevitBeam()
      {
        type = myBrace.type,
        baseLine = myBrace.baseLine,
        level = myBrace.level
      };

      myBeam.family = myBrace.family;
      myBeam.parameters = myBrace.parameters;
      myBeam.typeParameters = myBrace.typeParameters;

      return FamilyInstanceToNative(myBeam, StructuralType.Brace);
    }

    private IRevitElement BraceToSpeckle(DB.FamilyInstance myFamily)
    {
      var myBeam = BeamToSpeckle(myFamily) as RevitBeam;

      var myBrace = new RevitBrace()
      {
        type = myBeam.type,
        baseLine = myBeam.baseLine,
        level = myBeam.level
      };

      myBrace.family = myBeam.family;
      myBrace.parameters = myBeam.parameters;
      myBrace.typeParameters = myBeam.typeParameters;

      return myBrace;
    }
  }
}