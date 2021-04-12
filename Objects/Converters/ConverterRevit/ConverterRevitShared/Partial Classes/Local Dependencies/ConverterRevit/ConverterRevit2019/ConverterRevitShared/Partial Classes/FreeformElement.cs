using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DB = Autodesk.Revit.DB;
using ConverterRevitShared.Revit;
using Objects.Geometry;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Objects.Converter.Revit
{
    public partial class ConverterRevit
    {
        public ApplicationPlaceholderObject FreeformElementToNative(
            Objects.BuiltElements.Revit.FreeformElement freeformElement)
        {
            // 1. Convert the freeformElement geometry to native
            var solids = new List<DB.Solid>();
            var brep = freeformElement.baseGeometry as Brep;
            try
            {
                var solid = BrepToNative(freeformElement.baseGeometry as Brep);
                solids.Add(solid);
            }
            catch (Exception e)
            {
                ConversionErrors.Add(new SpeckleException($"Could not convert BREP {freeformElement.id} to native, falling back to mesh representation."));
                var mesh = MeshToNative(brep.displayMesh, DB.TessellatedShapeBuilderTarget.Solid)
                    .Select(m => m as DB.Solid);
                solids.AddRange(mesh);
                return null;
            }

            var tempPath = CreateFreeformElementFamily(solids, freeformElement.id);
            Doc.LoadFamily(tempPath, new FamilyLoadOption(), out var fam);
            var symbol = Doc.GetElement(fam.GetFamilySymbolIds().First()) as DB.FamilySymbol;
            symbol.Activate();

            var freeform = Doc.Create.NewFamilyInstance(DB.XYZ.Zero, symbol, DB.Structure.StructuralType.NonStructural);

            SetInstanceParameters(freeform, freeformElement);
            return new ApplicationPlaceholderObject
            {
                applicationId = freeformElement.applicationId, ApplicationGeneratedId = freeform.UniqueId,
                NativeObject = freeform
            };
        }

        public ApplicationPlaceholderObject FreeformElementToNative(Brep brep)
        {
            var solids = new List<DB.Solid>();
            try
            {
                var solid = BrepToNative(brep);
                solids.Append(solid);
            }
            catch (Exception e)
            {
                var mesh = MeshToNative(brep.displayMesh, DB.TessellatedShapeBuilderTarget.Solid)
                    .Select(m => m as DB.Solid);
                solids.AddRange(mesh);
                //throw new SpeckleException("Could not convert brep to native");
            }

            var tempPath = CreateFreeformElementFamily(solids, brep.id);
            DB.Family fam;
            Doc.LoadFamily(tempPath, new FamilyLoadOption(), out fam);
            var symbol = Doc.GetElement(fam.GetFamilySymbolIds().First()) as DB.FamilySymbol;
            symbol.Activate();
            

            var freeform = Doc.Create.NewFamilyInstance(DB.XYZ.Zero, symbol, DB.Structure.StructuralType.NonStructural);
            
            SetInstanceParameters(freeform, brep);
            return new ApplicationPlaceholderObject
            {
                applicationId = brep.applicationId, ApplicationGeneratedId = freeform.UniqueId, NativeObject = freeform
            };
        }

        private string CreateFreeformElementFamily(List<DB.Solid> solids, string name)
        {
            // FreeformElements can only be created in a family context.
            // so we create a temporary family to hold it.
            
            var famPath = Path.Combine(Doc.Application.FamilyTemplatePath, @"English\Metric Generic Model.rft");
            if (!File.Exists(famPath))
            {
                throw new Exception($"Could not find file Metric Generic Model.rft - {famPath}");
            }
            
            var famDoc = Doc.Application.NewFamilyDocument(famPath);
            using (DB.Transaction t = new DB.Transaction(famDoc, "Create Freeform Elements"))
            {
                t.Start();
                    
                solids.ForEach(s => DB.FreeFormElement.Create(famDoc, s));

                t.Commit();
            }

            var famName = "SpeckleFreeform_" + name;
            string tempFamilyPath = Path.Combine(Path.GetTempPath(), famName + ".rfa");
            var so = new DB.SaveAsOptions();
            so.OverwriteExistingFile = true;
            famDoc.SaveAs(tempFamilyPath, so);
            famDoc.Close();

            return tempFamilyPath;
        }
    }
}