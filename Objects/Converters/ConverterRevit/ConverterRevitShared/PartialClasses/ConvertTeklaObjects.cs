using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;
using Objects.Structural.Properties.Profiles;
using Objects.Structural.Materials;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject TeklaBeamToNative(BuiltElements.TeklaStructures.TeklaBeam teklaBeam, StructuralType structuralType = StructuralType.Beam)
    {
      var appObj = new ApplicationObject(teklaBeam.id, teklaBeam.speckle_type) { applicationId = teklaBeam.applicationId };

      RevitBeam revitBeam = new RevitBeam();
      //This only works for CSIC sections now for sure. Need to test on other sections
      revitBeam.type = teklaBeam.profile.name.Replace('X', 'x');
      revitBeam.baseLine = teklaBeam.baseLine;
      //Beam beam = new Beam(teklaBeam.baseLine);
      appObj = BeamToNative(revitBeam);
      //DB.FamilyInstance nativeRevitBeam = (DB.FamilyInstance)placeholders[0].NativeObject;
      return appObj;
    }
  }
}
