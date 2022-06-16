using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
    public partial class ConverterRevit
    {
        #region MaterialQuantity
        /// <summary>
        /// Gets the quantitiy of a material in one element
        /// </summary>
        /// <param name="element"></param>
        /// <param name="material"></param>
        /// <returns></returns>
        public Objects.Other.MaterialQuantity MaterialQuantityToSpeckle(DB.Element element, DB.Material material)
        {
            if (material == null || element == null) return null;
            //Create VolumeParam
            Objects.BuiltElements.Revit.Parameter volume = new Objects.BuiltElements.Revit.Parameter()
            {
                name = "Volume",
                value = RevitVersionHelper.ConvertCubiceMetresFromInternalUnits(element.GetMaterialVolume(material.Id)),
                applicationUnitType = "autodesk.unit.unit:cubicMeters-1.0.1",
                applicationUnit = "autodesk.spec.aec:volume-2.0.0",
                applicationInternalName = null,
                isShared = false,
                isReadOnly = false,
                isTypeParameter = true,
                units = null
            };

            Objects.BuiltElements.Revit.Parameter area = new Objects.BuiltElements.Revit.Parameter()
            {
                name = "Area",
                applicationUnitType = "autodesk.unit.unit:squareMeters-1.0.1",
                applicationUnit = "autodesk.spec.aec:area-2.0.0",
                applicationInternalName = null,
                isShared = false,
                isReadOnly = false,
                isTypeParameter = true,
                value = RevitVersionHelper.ConvertSquareMetresFromInternalUnits(element.GetMaterialArea(material.Id, false)),
                units = null
            };


            var speckleMaterial = ConvertAndCacheMaterial(material.Id, material.Document);
            return new Objects.Other.MaterialQuantity(speckleMaterial, volume, area);
        }
        

        #endregion


        #region MaterialQuantities
            public IEnumerable<Objects.Other.MaterialQuantity> MaterialQuantitiesToSpeckle(DB.Element element)
        {
            var matIDs = element.GetMaterialIds(false);
            if (matIDs == null || matIDs.Count() == 0)
            {
                return null;
            }
            var materials = matIDs.Select(material => element.Document.GetElement(material) as DB.Material);
            return MaterialQuantitiesToSpeckle(element, materials);
        }
        public IEnumerable<Objects.Other.MaterialQuantity> MaterialQuantitiesToSpeckle(DB.Element element, IEnumerable<DB.Material> materials)
        {
            if (materials == null || materials.Count() == 0) return null;
            List<Objects.Other.MaterialQuantity> quantities = new List<Objects.Other.MaterialQuantity>();

            foreach (var material in materials)
            {
                quantities.Add(MaterialQuantityToSpeckle(element, material));
            }
            return quantities;
        }


        #endregion
    }

}
