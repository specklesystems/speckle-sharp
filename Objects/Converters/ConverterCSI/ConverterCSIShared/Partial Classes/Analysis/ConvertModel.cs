using System;
using System.Collections.Generic;
using Objects.Structural.Analysis;
using Objects.Geometry;
using Objects.Structural.Properties;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.CSI.Properties;
using Objects.Structural.GSA.Properties;
using Objects.Structural.GSA.Geometry;
using Objects.Structural.Loading;
using Objects.Structural.CSI.Loading;
using Speckle.Core.Models;
using Objects.Structural.CSI.Geometry;
using Objects.Structural.CSI.Analysis;
using System.Linq;
using CSiAPIv1;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public object ModelToNative(Model model, ref ApplicationObject appObj)
    {
      
      return null;
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
      foreach (string propertySpring in springPointProperties)
      {
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
      foreach (string propertyLink in LinkProperties)
      {
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
        var specklePropertyDiaphragm = diaphragmToSpeckle(propertyDiaphragm);
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