using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using System.Text;
using Element = Objects.Element;
using Objects;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public DB.Element BraceToNative(Brace myBrace)
    {
      var myBeam = new Beam()
      {
        type = myBrace.type,
        baseGeometry = myBrace.baseGeometry,
        level = myBrace.level
      };

      myBeam["family"] = myBrace["family"];
      myBeam["parameters"] = myBrace["parameters"];
      myBeam["typeParameters"] = myBrace["typeParameters"];

      return FamilyInstanceToNative(myBeam, StructuralType.Brace);
    }

    private Element BraceToSpeckle(FamilyInstance myFamily)
    {
      var myBeam = BeamToSpeckle(myFamily) as Beam;

      var myBrace = new Brace()
      {
        type = myBeam.type,
        baseGeometry = myBeam.baseGeometry,
        level = myBeam.level
      };

      myBrace["family"] = myBeam["family"];
      myBrace["parameters"] = myBeam["parameters"];
      myBrace["typeParameters"] = myBeam["typeParameters"];

      return myBrace;
    }



  }
}
