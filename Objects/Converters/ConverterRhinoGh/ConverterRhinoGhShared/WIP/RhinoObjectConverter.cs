//using Rhino.DocObjects.Custom;
//using Rhino.DocObjects;
//using Speckle.Core.Models;

//public class RhinoObjectConverter : IRhinoObjectConverter
//{
//  public Base Convert(string appIdKey, RhinoObject ro)
//  {
//    var roId = ro.Attributes.GetUserString(appIdKey) ?? ro.Id.ToString();
//    var reportObj = new ApplicationObject(ro.Id.ToString(), ro.ObjectType.ToString()) { applicationId = roId };
//    var material = RenderMaterialToSpeckle(ro.GetMaterial(true));
//    style = DisplayStyleToSpeckle(ro.Attributes);
//    userDictionary = ro.UserDictionary;
//    userStrings = ro.Attributes.GetUserStrings();
//    objName = ro.Attributes.Name;

//    // Fast way to get the displayMesh, try to get the mesh rhino shows on the viewport when available.
//    // This will only return a mesh if the object has been displayed in any mode other than Wireframe.
//    if (ro is BrepObject || ro is ExtrusionObject)
//      displayMesh = GetRhinoRenderMesh(ro);

//    //rhino BIM to be deprecated after the mapping tool is released
//    if (ro.Attributes.GetUserString(SpeckleSchemaKey) != null) // schema check - this will change in the near future
//      schema = ConvertToSpeckleBE(ro, reportObj, displayMesh) ?? ConvertToSpeckleStr(ro, reportObj);

//    //mapping tool
//    var mappingString = ro.Attributes.GetUserString(SpeckleMappingKey);
//    if (mappingString != null)
//      schema = MappingToSpeckle(mappingString, ro, notes);

//    if (!(@object is InstanceObject))
//      @object = ro.Geometry; // block instance check
//  }
//}
