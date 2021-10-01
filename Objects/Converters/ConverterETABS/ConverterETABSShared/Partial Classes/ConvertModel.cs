using System;
using Objects.Structural.Analysis;
using Objects.Geometry;
using ETABSv1;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        Model ModelToSpeckle()
        {
            var model = new Model();
            model.specs = ModelInfoToSpeckle();
            return model;
        }
    }
}
