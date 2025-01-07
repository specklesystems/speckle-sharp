using System.Collections.Generic;
using System.Linq;
using Objects.Structural.Analysis;
using Speckle.Core.Models;

namespace Objects.Converter.CSI;

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
    string[] properties1D = System.Array.Empty<string>();

    //var stories = StoriesToSpeckle();
    ////Should stories belong here ? not sure
    //model.elements.Add(stories);


    //Properties are sent by default whether you want them to be sent or not. Easier this way to manage information about the model
    Model.PropFrame.GetNameList(ref number, ref properties1D);
    foreach (string property1D in properties1D)
    {
      var speckleProperty1D = Property1DToSpeckle(property1D);
      model.properties.Add(speckleProperty1D);
    }

    string[] springPointProperties = System.Array.Empty<string>();
    Model.PropPointSpring.GetNameList(ref number, ref springPointProperties);
    foreach (string propertySpring in springPointProperties)
    {
      var specklePropertyPointSpring = SpringPropertyToSpeckle(propertySpring);
      model.properties.Add(specklePropertyPointSpring);
    }

    string[] springLineProperties = System.Array.Empty<string>();
    Model.PropLineSpring.GetNameList(ref number, ref springLineProperties);
    foreach (string propertyLine in springLineProperties)
    {
      var specklePropertyLineSpring = LinearSpringToSpeckle(propertyLine);
      model.properties.Add(specklePropertyLineSpring);
    }

    string[] springAreaProperties = System.Array.Empty<string>();
    Model.PropAreaSpring.GetNameList(ref number, ref springAreaProperties);
    foreach (string propertyArea in springAreaProperties)
    {
      var specklePropertyAreaSpring = AreaSpringToSpeckle(propertyArea);
      model.properties.Add(specklePropertyAreaSpring);
    }
    string[] LinkProperties = System.Array.Empty<string>();
    Model.PropLink.GetNameList(ref number, ref LinkProperties);
    foreach (string propertyLink in LinkProperties)
    {
      var specklePropertyLink = LinkPropertyToSpeckle(propertyLink);
      model.properties.Add(specklePropertyLink);
    }

    string[] TendonProperties = System.Array.Empty<string>();
    Model.PropTendon.GetNameList(ref number, ref TendonProperties);
    foreach (string propertyTendon in TendonProperties)
    {
      var specklePropertyTendon = TendonPropToSpeckle(propertyTendon);
      model.properties.Add(specklePropertyTendon);
    }

    string[] DiaphragmProperties = System.Array.Empty<string>();
    Model.Diaphragm.GetNameList(ref number, ref DiaphragmProperties);
    foreach (string propertyDiaphragm in DiaphragmProperties)
    {
      var specklePropertyDiaphragm = diaphragmToSpeckle(propertyDiaphragm);
      model.properties.Add(specklePropertyDiaphragm);
    }

    string[] properties2D = System.Array.Empty<string>();
    Model.PropArea.GetNameList(ref number, ref properties2D);
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

    string[] materials = System.Array.Empty<string>();
    Model.PropMaterial.GetNameList(ref number, ref materials);
    foreach (string material in materials)
    {
      var speckleMaterial = MaterialToSpeckle(material);
      model.materials.Add(speckleMaterial);
    }

    return model;
  }
}
