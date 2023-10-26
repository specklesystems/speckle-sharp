using System;
using System.Collections.Generic;
using Objects.Structural.Analysis;
using Speckle.Core.Models;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    private Base ModelElementsCountToSpeckle()
    {
      int count = SpeckleModel.elements.Count;
      count += SpeckleModel.nodes.Count;

      Base elementsCount = new() { applicationId = count.ToString() };
      return elementsCount;
    }

    private Model ModelToSpeckle()
    {
      var model = new Model
      {
        specs = ModelInfoToSpeckle(),
        nodes = new List<Base>(),
        materials = new List<Base>(),
        elements = new List<Base>(),
        properties = new List<Base>(),
        restraints = new List<Base>(),
        loads = new List<Base>()
      };
      int number = 0;

      //var stories = StoriesToSpeckle();
      ////Should stories belong here ? not sure
      //model.elements.Add(stories);


      //Properties are sent by default whether you want them to be sent or not. Easier this way to manage information about the model
      {
        string[] properties1D = Array.Empty<string>();
        Model.PropFrame.GetNameList(ref number, ref properties1D);

        foreach (string property1D in properties1D)
        {
          var speckleProperty1D = Property1DToSpeckle(property1D);
          model.properties.Add(speckleProperty1D);
        }
      }

      {
        string[] springPointProperties = Array.Empty<string>();
        Model.PropPointSpring.GetNameList(ref number, ref springPointProperties);

        foreach (string propertySpring in springPointProperties)
        {
          var specklePropertyPointSpring = SpringPropertyToSpeckle(propertySpring);
          model.properties.Add(specklePropertyPointSpring);
        }
      }

      {
        string[] springLineProperties = Array.Empty<string>();
        Model.PropLineSpring.GetNameList(ref number, ref springLineProperties);

        foreach (string propertyLine in springLineProperties)
        {
          var specklePropertyLineSpring = LinearSpringToSpeckle(propertyLine);
          model.properties.Add(specklePropertyLineSpring);
        }
      }

      {
        string[] springAreaProperties = Array.Empty<string>();
        Model.PropAreaSpring.GetNameList(ref number, ref springAreaProperties);

        foreach (string propertyArea in springAreaProperties)
        {
          var specklePropertyAreaSpring = AreaSpringToSpeckle(propertyArea);
          model.properties.Add(specklePropertyAreaSpring);
        }
      }

      {
        string[] linkProperties = Array.Empty<string>();
        Model.PropLink.GetNameList(ref number, ref linkProperties);

        foreach (string propertyLink in linkProperties)
        {
          var specklePropertyLink = LinkPropertyToSpeckle(propertyLink);
          model.properties.Add(specklePropertyLink);
        }
      }

      {
        string[] tendonProperties = Array.Empty<string>();
        Model.PropTendon.GetNameList(ref number, ref tendonProperties);

        foreach (string propertyTendon in tendonProperties)
        {
          var specklePropertyTendon = TendonPropToSpeckle(propertyTendon);
          model.properties.Add(specklePropertyTendon);
        }
      }

      {
        string[] diaphragmProperties = Array.Empty<string>();
        Model.Diaphragm.GetNameList(ref number, ref diaphragmProperties);

        foreach (string propertyDiaphragm in diaphragmProperties)
        {
          var specklePropertyDiaphragm = DiaphragmToSpeckle(propertyDiaphragm);
          model.properties.Add(specklePropertyDiaphragm);
        }
      }

      {
        string[] properties2D = Array.Empty<string>();
        Model.PropArea.GetNameList(ref number, ref properties2D);

        foreach (string property in properties2D)
        {
          var speckleProperty2D = FloorPropertyToSpeckle(property) ?? WallPropertyToSpeckle(property);
          model.properties.Add(speckleProperty2D);
        }
      }

      {
        string[] materials = Array.Empty<string>();
        Model.PropMaterial.GetNameList(ref number, ref materials);
        foreach (string material in materials)
        {
          var speckleMaterial = MaterialToSpeckle(material);
          if (speckleMaterial is not null)
            model.materials.Add(speckleMaterial);
        }
      }

      return model;
    }
  }
}
