#if REVIT2024

using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  public ApplicationObject ToposolidToNative(RevitToposolid fromSpeckle)
  {
    string name = null;
    ApplicationObject appObj = null;

    var docObj = GetExistingElementByApplicationId(fromSpeckle.applicationId);
    appObj = new ApplicationObject(fromSpeckle.id, fromSpeckle.speckle_type)
    {
      applicationId = fromSpeckle.applicationId
    };

    // skip if element already exists in doc & receive mode is set to ignore
    if (IsIgnore(docObj, appObj))
    {
      return appObj;
    }

    // get the curves and the points
    var curveLoops = new List<CurveLoop>();
    foreach (var curveArray in fromSpeckle.profiles)
    {
      curveLoops.Add(CurveArrayToCurveLoop(CurveToNative(curveArray.segments)));
    }
  
    var points = fromSpeckle.points.Select(x => PointToNative(x)).ToList();
    
    // NOTE: if the level is null this will not create
    // there maybe something more elegant we can do to automatically drop to direct shape
    DB.Level level = ConvertLevelToRevit(fromSpeckle.level, out ApplicationObject.State state);
  
    var topoSolidType = GetElementType<ToposolidType>(fromSpeckle, appObj, out bool _);

    Toposolid revitToposolid = null;
    if (points.Count > 0)
    {
      revitToposolid = Toposolid.Create(Doc, curveLoops, points, topoSolidType.Id, level?.Id);          
    }
    else
    {
      revitToposolid = Toposolid.Create(Doc, curveLoops, topoSolidType.Id, level?.Id);          
    }

    SetInstanceParameters(revitToposolid, fromSpeckle);

    appObj.Update(status: ApplicationObject.State.Created, createdId: revitToposolid.UniqueId, convertedItem: revitToposolid);

    return appObj;
  }

  private RevitToposolid ToposolidToSpeckle(Toposolid topoSolid, out List<string> notes)
  { 
    var toSpeckle = new RevitToposolid();
    notes = new List<string>();
    
    // we will store the list of interior points in order to recreate the Toposolid
    var slabShapeEditor = topoSolid.GetSlabShapeEditor();
    var vertices = slabShapeEditor.SlabShapeVertices;
    toSpeckle.points = vertices.Cast<SlabShapeVertex>()
      .Select(x => PointToSpeckle(x.Position, Doc))
      .ToList();

    var sketch = Doc.GetElement(topoSolid.SketchId) as Sketch;
    toSpeckle.profiles = GetSketchProfiles(sketch);

    var type = topoSolid.Document.GetElement(topoSolid.GetTypeId()) as ElementType;

    toSpeckle.level = ConvertAndCacheLevel(topoSolid, BuiltInParameter.LEVEL_PARAM);
    toSpeckle.family = type?.FamilyName;
    toSpeckle.type = type?.Name;

    GetAllRevitParamsAndIds(
      toSpeckle,
      topoSolid,
      new List<string>
      {
        "LEVEL_PARAM",
      }
    );

    toSpeckle.displayValue = GetElementDisplayValue(
      topoSolid,
      new Options() { DetailLevel = ViewDetailLevel.Fine });

    GetHostedElements(toSpeckle, topoSolid, out List<string> hostedNotes);
    if (hostedNotes.Any())
    {
      notes.AddRange(hostedNotes);
    }

    return toSpeckle;
  }
}

#endif
