using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Objects.Geometry;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using DB = Autodesk.Revit.DB.Mechanical;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.Revit
{
    public partial class ConverterRevit
    {
        
        public List<ApplicationPlaceholderObject> SpaceToNative(Space speckleSpace)
        {
            // create new space or update existing space
            var revitSpace = GetExistingElementByApplicationId(speckleSpace.applicationId) as DB.Space;
            var level = LevelToNative(speckleSpace.level);
            
            if (speckleSpace.separationLines.Any())
            {
                foreach (var separationLine in speckleSpace.separationLines)
                {
                    // create new space separation lines, update existing space separation lines 
                    SpaceSeparationLineToNative(separationLine);
                }
            } 
            else
            {                
                var boundaries = revitSpace.GetBoundarySegments(new SpatialElementBoundaryOptions());
                foreach (var loop in boundaries)
                {
                    foreach (var segment in loop)
                    {
                        var curve = Doc.GetElement(segment.ElementId) as ModelCurve;
                        if (curve == null)
                        {
                            continue;
                        }

                        // remove unused space separation lines
                        if (curve.CurveElementType == CurveElementType.SpaceSeparation)
                        {                            
                            Doc.Delete(curve.Id);
                        }
                    }
                }
            }
            
            if (revitSpace == null)
            {
                revitSpace = Doc.Create.NewSpace(level, new UV(speckleSpace.basePoint.x, speckleSpace.basePoint.y));
            }

            revitSpace.Name = speckleSpace.name;
            revitSpace.Number = speckleSpace.number;
            revitSpace.SpaceType = !string.IsNullOrEmpty(speckleSpace.spaceType) ? (DB.SpaceType) Enum.Parse(typeof(DB.SpaceType), speckleSpace.spaceType) : DB.SpaceType.NoSpaceType; 

            SetInstanceParameters(revitSpace, speckleSpace);

            var placeholders = new List<ApplicationPlaceholderObject>()
              {
                new ApplicationPlaceholderObject
                {
                applicationId = speckleSpace.applicationId,
                ApplicationGeneratedId = revitSpace.UniqueId,
                NativeObject = revitSpace
                }
              };

            return placeholders;
        }

        public BuiltElements.Space SpaceToSpeckle(DB.Space revitSpace)
        {
            var profiles = GetProfiles(revitSpace);

            var speckleSpace = new Space();

            speckleSpace.name = revitSpace.Name;
            speckleSpace.number = revitSpace.Number;
            speckleSpace.basePoint = (Point)LocationToSpeckle(revitSpace);
            speckleSpace.level = ConvertAndCacheLevel(revitSpace.LevelId);
            speckleSpace.baseOffset = GetParamValue<double>(revitSpace, BuiltInParameter.ROOM_LOWER_OFFSET);
            speckleSpace.upperLimit = ConvertAndCacheLevel(revitSpace.UpperLimit.Id);
            speckleSpace.limitOffset = GetParamValue<double>(revitSpace, BuiltInParameter.ROOM_UPPER_OFFSET);
            speckleSpace.boundary = profiles[0];
            speckleSpace.area = GetParamValue<double>(revitSpace, BuiltInParameter.ROOM_AREA);
            speckleSpace.volume = GetParamValue<double>(revitSpace, BuiltInParameter.ROOM_VOLUME);
            speckleSpace.spaceType = revitSpace.SpaceType.ToString();

            var spaceSeparationLines = new List<BuiltElements.Revit.Curve.SpaceSeparationLine>();
            var boundaries = revitSpace.GetBoundarySegments(new SpatialElementBoundaryOptions());
            foreach (var loop in boundaries)
            {
                foreach (var segment in loop)
                {
                    var curve = Doc.GetElement(segment.ElementId) as ModelCurve;
                    if (curve == null)
                    {
                        continue;
                    }

                    // what about room separation lines?
                    if (curve.CurveElementType == CurveElementType.SpaceSeparation)
                    {
                        var spaceSeparationLine = SpaceSeparationLineToSpeckle(curve);
                        spaceSeparationLines.Add(spaceSeparationLine);
                    }
                }
            }

            speckleSpace.separationLines = spaceSeparationLines;

            GetAllRevitParamsAndIds(speckleSpace, revitSpace);
            speckleSpace.displayMesh = GetElementDisplayMesh(revitSpace);

            return speckleSpace;
        }
    }
}