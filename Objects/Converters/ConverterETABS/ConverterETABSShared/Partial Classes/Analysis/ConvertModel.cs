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
          if (property is Property1D)
          {
            Property1DToNative((Property1D)property);
          }
          else if (property is GSAProperty1D)
          {
            break;
          }
          else if (property is ETABSLinkProperty){
            LinkPropertyToNative((ETABSLinkProperty)property);
          }
          else if(property is ETABSAreaSpring){
            AreaSpringPropertyToNative((ETABSAreaSpring)property);
          }
          else 
          {
            Property2DToNative((ETABSProperty2D)property);
          }
        }
      }

      if (model.elements != null)
      {
        foreach (var element in model.elements)
        {

          if (element.GetType().ToString() == Element1D.GetType().ToString())
          {
            FrameToNative((ETABSElement1D)element);
          }
          else if (element.GetType().Equals(GSAElement1D.GetType()))
          {
            FrameToNative((ETABSElement1D)element);
          }
          else
          {
            AreaToNative((ETABSElement2D)element);
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
          else if (load.GetType().Equals(Loading2DWind.GetType()))
          {
            LoadFaceToNative((ETABSWindLoadingFace)load);
          }
          else if (load.GetType().Equals(Loading1D.GetType()))
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

      var stories = StoriesToSpeckle();
      //Should stories belong here ? not sure 
      model.elements.Add(stories);


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
        var specklePropertyLineSpring = SpringPropertyToSpeckle(propertyLine);
        model.properties.Add(specklePropertyLineSpring);
      }

      string[] springAreaProperties = { };
      Model.PropAreaSpring.GetNameList(ref number, ref springAreaProperties);
      springAreaProperties.ToList();
      foreach (string propertyArea in springAreaProperties)
      {
        var specklePropertyAreaSpring = SpringPropertyToSpeckle(propertyArea);
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
