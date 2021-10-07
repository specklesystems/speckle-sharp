using System;
using System.Collections.Generic;
using Objects.Structural.Analysis;
using Objects.Geometry;
using Objects.Structural.Properties;
using Objects.Structural.Geometry;
using Speckle.Core.Models;
using ETABSv1;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        Model ModelToSpeckle()
        {
            var model = new Model();
            model.specs = ModelInfoToSpeckle();

            model.elements = new List<Base> { };

            //model.elements.Add();


           

            return model;
        }
    }
}
