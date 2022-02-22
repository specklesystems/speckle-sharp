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
using Objects.Structural.ETABS.Geometry;
using Objects.Structural.ETABS.Analysis;
using System.Linq;
using ETABSv1;

namespace Objects.Converter.ETABS
{
  public partial class ConverterETABS
  {
    public object ModelToNative(Model model)
    {
      if(model.specs != null) { ModelInfoToNative(model.specs); }

      if (model.materials != null)
      {
        foreach (Material material in model.materials)
        {
          MaterialToNative(material);
        }
      }

      if (model.properties != null)
      {
        foreach (var property in model.properties)
        {
          if (property is GSAProperty1D)
          {
            break;
          }
          else if (property is ETABSSpringProperty){
            SpringPropertyToNative((ETABSSpringProperty)property);
          }
          else if (property is ETABSLinearSpring){
            LinearSpringPropertyToNative((ETABSLinearSpring)property);
          }
          else if(property is ETABSAreaSpring){
            AreaSpringPropertyToNative((ETABSAreaSpring)property);
          }
          else if (property is ETABSLinkProperty)
          {
            LinkPropertyToNative((ETABSLinkProperty)property);
          }
          else if (property is ETABSTendonProperty){
            break;
          }
          else if (property is  ETABSProperty2D)
          {
            Property2DToNative((ETABSProperty2D)property);
          }
          else if (property is ETABSDiaphragm){
            diaphragmToNative((ETABSDiaphragm)property);
          }
          else if (property is Property1D)
          {
            Property1DToNative((Property1D)property);
          }
        }
      }

      if (model.elements != null)
      {
        foreach (var element in model.elements)
        { 

          if (element is Element1D && !(element is ETABSTendon))
          {
            var ETABSelement = (Element1D)element;
            if(ETABSelement.type == ElementType1D.Link){
              LinkToNative((ETABSElement1D)(element));
            }
            else{
              FrameToNative((Element1D)element);
            }
          }

          else if (element is Element2D)
          {
            AreaToNative((Element2D)element);
          }
          else if (element is ETABSStories){
            StoriesToNative((ETABSStories)element);
          }
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
          if (load is LoadGravity)
          {
            LoadPatternToNative((LoadGravity)load);
          }
        }
        foreach (var load in model.loads)
        {
          if (load is LoadFace)
          {
            LoadFaceToNative((LoadFace)load);
          }
          else if (load is ETABSWindLoadingFace)
          {
            LoadFaceToNative((ETABSWindLoadingFace)load);
          }
          else if (load is LoadBeam)
          {
            var loading1D = (LoadBeam)load;
            if (loading1D.loadType == BeamLoadType.Uniform)
            {
              LoadUniformFrameToSpeckle(loading1D);
            }
            else if (loading1D.loadType == BeamLoadType.Point)
            {
              LoadPointFrameToSpeckle(loading1D);
            }
          }

        }
      }

      return "Finished";
    }
    Base ModelElementsCountToSpeckle()
    {
      var ElementsCount = new Base();
      int count = SpeckleModel.elements.Count();
      count += SpeckleModel.nodes.Count();
      ElementsCount.applicationId = count.ToString();
      return ElementsCount;
    }
    public Model ModelToSpeckle()
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

      //var stories = StoriesToSpeckle();
      ////Should stories belong here ? not sure 
      //model.elements.Add(stories);


      //Properties are sent by default whether you want them to be sent or not. Easier this way to manage information about the model
      Model.PropFrame.GetNameList(ref number, ref properties1D);
      properties1D.ToList();
      foreach (string property1D in properties1D)
      {
        var speckleProperty1D = Property1DToSpeckle(property1D);
        model.properties.Add(speckleProperty1D);
      }

      string[] springPointProperties = { };
      Model.PropPointSpring.GetNameList(ref number, ref springPointProperties);
      springPointProperties.ToList();
      foreach(string propertySpring in springPointProperties){
        var specklePropertyPointSpring = SpringPropertyToSpeckle(propertySpring);
        model.properties.Add(specklePropertyPointSpring);
      }

      string[] springLineProperties = { };
      Model.PropLineSpring.GetNameList(ref number, ref springLineProperties);
      springLineProperties.ToList();
      foreach (string propertyLine in springLineProperties)
      {
        var specklePropertyLineSpring = LinearSpringToSpeckle(propertyLine);
        model.properties.Add(specklePropertyLineSpring);
      }

      string[] springAreaProperties = { };
      Model.PropAreaSpring.GetNameList(ref number, ref springAreaProperties);
      springAreaProperties.ToList();
      foreach (string propertyArea in springAreaProperties)
      {
        var specklePropertyAreaSpring = AreaSpringToSpeckle(propertyArea);
        model.properties.Add(specklePropertyAreaSpring);
      }
      string[] LinkProperties = { };
      Model.PropLink.GetNameList(ref number, ref LinkProperties);
      LinkProperties.ToList();
      foreach(string propertyLink in LinkProperties){
        var specklePropertyLink = LinkPropertyToSpeckle(propertyLink);
        model.properties.Add(specklePropertyLink);
      }

      string[] TendonProperties = { };
      Model.PropTendon.GetNameList(ref number, ref TendonProperties);
      TendonProperties.ToList();
      foreach (string propertyTendon in TendonProperties)
      {
        var specklePropertyTendon = TendonPropToSpeckle(propertyTendon);
        model.properties.Add(specklePropertyTendon);
      }

      string[] DiaphragmProperties = { };
      Model.Diaphragm.GetNameList(ref number, ref DiaphragmProperties);
      DiaphragmProperties.ToList();
      foreach (string propertyDiaphragm in DiaphragmProperties)
      {
        var specklePropertyDiaphragm =diaphragmToSpeckle(propertyDiaphragm);
        model.properties.Add(specklePropertyDiaphragm);
      }

      string[] properties2D = { };
      Model.PropArea.GetNameList(ref number, ref properties2D);
      properties2D.ToList();
      foreach (string property in properties2D)
      {
        var speckleProperty2D = FloorPropertyToSpeckle(property);
        if (speckleProperty2D != null)
        {
          model.properties.Add(speckleProperty2D);
        }
        else
        {
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
