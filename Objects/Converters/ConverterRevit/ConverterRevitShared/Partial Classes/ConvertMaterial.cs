using Autodesk.Revit.DB.Structure;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
    public partial class ConverterRevit
    {
        public Objects.Other.Material MaterialToSpeckle(DB.Material revitmaterial)
        {

            var speckleMaterial = new Objects.Other.Revit.RevitMaterial(revitmaterial.Name, revitmaterial.MaterialCategory, revitmaterial.MaterialClass, revitmaterial.Shininess,
                revitmaterial.Smoothness, revitmaterial.Transparency);


            GetAllRevitParamsAndIds(speckleMaterial, revitmaterial);

            Report.Log($"Converted Material{revitmaterial.Id}");
            return speckleMaterial;
        }

      

        private Objects.Other.Material ConvertAndCacheMaterial(DB.ElementId id, DB.Document doc)
        {
            var material = doc.GetElement(id) as DB.Material;

            if (material == null) return null;
            if (!Materials.ContainsKey(material.Name))
            {
                Materials[material.Name] = MaterialToSpeckle(material);
            }
            return Materials[material.Name] as Objects.Other.Material;
        }
    }

}
