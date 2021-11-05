using System;
using System.Collections.Generic;
using Objects.Structural.Analysis;
using Objects.Geometry;
using Objects.Structural.Properties;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.ETABS.Properties;
using Objects.Structural.GSA.Properties;
using Objects.Structural.GSA.Geometry;
using Objects.Structural.Loading;
using Objects.Structural.ETABS.Loading;
using Speckle.Core.Models;
using System.Linq;
using ETABSv1;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        object ModelToNative(Model model)
        {
            var Element1D = new Element1D();
            var Property1D = new Property1D();
            var Loading1D = new LoadBeam();
            var Loading2D = new LoadFace();
            var LoadingNode = new LoadNode();
            var LoadGravity = new LoadGravity();
            var Loading2DWind = new ETABSWindLoadingFace();
            var GSAProperty = new GSAProperty1D();
            var GSAElement1D = new GSAMember1D();
            if(model.materials != null)
            {
                foreach (Material material in model.materials)
                {
                    MaterialToNative(material);
                }
            }

            if(model.properties != null)
            {
                foreach (var property in model.properties)
                {
                    if (property.GetType().ToString() == Property1D.GetType().ToString())
                    {
                        Property1DToNative((Property1D)property);
                    }
                    else if (property.GetType().Equals(GSAProperty.GetType()))
                    {
                        break;
                    }
                    else
                    {
                        Property2DToNative((ETABSProperty2D)property);
                    }
                }
            }

            if( model.elements != null)
            {
                foreach (var element in model.elements)
                {

                    if (element.GetType().ToString() == Element1D.GetType().ToString())
                    {
                        FrameToNative((Element1D)element);
                    }
                    else if (element.GetType().Equals(GSAElement1D.GetType()))
                    {
                        FrameToNative((Element1D)element);
                    }
                    else
                    {
                        AreaToNative((Element2D)element);
                    }
                }
            }

            if(model.nodes != null)
            {
                foreach (Node node in model.nodes)
                {
                    PointToNative(node);
                }
            }

            if (model.nodes != null)
            {
                foreach (Node node in model.nodes)
                {
                    PointToNative(node);
                }
            }

            if (model.loads != null)
            {
                foreach (var load in model.loads)
                {
                    //import loadpatterns in first
                    if (load.GetType().Equals(LoadGravity.GetType()))
                    {
                        LoadPatternToNative((LoadGravity)load);
                    }
                }
                foreach (var load in model.loads)
                {
                    if (load.GetType().Equals(Loading2D.GetType())) 
                    {
                        LoadFaceToNative((LoadFace)load);
                    }
                    else if(load.GetType().Equals(Loading2DWind.GetType()))
                    {
                        LoadFaceToNative((ETABSWindLoadingFace)load);
                    }
                    else if (load.GetType().Equals(Loading1D.GetType()))
                    {
                        var loading1D = (LoadBeam)load;
                        if(loading1D.loadType == BeamLoadType.Uniform)
                        {
                            LoadUniformFrameToSpeckle(loading1D);
                        }
                        else if(loading1D.loadType == BeamLoadType.Point)
                        {
                            LoadPointFrameToSpeckle(loading1D);
                        }
                    }
                    
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
            model.restraints = new List<Base> { };
            model.loads = new List<Base> { };
            int number = 0;
            string[] properties1D = { };
            Model.PropFrame.GetNameList(ref number, ref properties1D);
            properties1D.ToList();
            foreach (string property1D in properties1D)
            {
                var speckleProperty1D = Property1DToSpeckle(property1D);
                model.properties.Add(speckleProperty1D);
            }

            string[] properties2D = { };
            Model.PropArea.GetNameList(ref number, ref properties2D);
            properties2D.ToList();
            foreach(string property in properties2D)
            {
                var speckleProperty2D = FloorPropertyToSpeckle(property);
                if (speckleProperty2D != null)
                {
                    model.properties.Add(speckleProperty2D);
                }
                else {
                    model.properties.Add(WallPropertyToSpeckle(property));
                }
            }


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
