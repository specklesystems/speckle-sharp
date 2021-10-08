using System;
using System.Collections.Generic;
using Objects.Structural.Analysis;
using Objects.Geometry;
using Objects.Structural.Properties;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;
using Speckle.Core.Models;
using System.Linq;
using ETABSv1;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        object ModelToNative(Model model)
        {
            foreach(Material material in model.materials)
            {
                MaterialToNative(material);
            }
            foreach(Property1D property1D in model.properties)
            {
                Property1DToNative(property1D);
            }
            foreach(var element in model.elements)
            {
                if (element.Equals(typeof(Element1D)))
                {
                    FrameToNative((Element1D)element);
                }
                else
                {
                    AreaToNative((Element2D)element);
                }
            }
            return "Finished";
        }
        Model ModelToSpeckle()
        {
            var model = new Model();
            model.specs = ModelInfoToSpeckle();
            model.nodes = new List<Base> { };
            model.materials = new List<Base> { };
            model.elements = new List<Base> { };
            model.properties = new List<Base> { };
            int number = 0;
            string[] properties1D = { };
            Model.PropFrame.GetNameList(ref number, ref properties1D);
            properties1D.ToList();
            foreach( string property1D in properties1D)
            {
                var speckleProperty1D  = Property1DToSpeckle(property1D);
                model.properties.Add(speckleProperty1D);
            }

            // need to rewirte Property2DToSpeckle to encapsulate this better by just name 
            //string[] properties2D = { };
            //Model.PropArea.GetNameList(ref number, ref properties2D);
            //properties2D.ToList();
            //foreach(string property2D in properties2D)
            //{
            //    var speckleProperty2D = Property2DToSpeckle(property2D,property2D);
            //    model.properties.Add(speckleProperty2D);
            //}

            string[] materials = { };
            Model.PropMaterial.GetNameList(ref number, ref materials);
            foreach (string material in materials)
            {
                var speckleMaterial = MaterialToSpeckle(material);
                model.materials.Add(speckleMaterial);
            }


           

            return model;
        }
    }
}
