#ifndef UTILITY_HPP
#define UTILITY_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"
#include "ResourceIds.hpp"
#include "Polygon2DData.h"

#define UNUSED(x) (void)(x)


namespace Utility {

// Element Type
API_ElemTypeID GetElementType (const API_Elem_Head& header);
API_ElemTypeID GetElementType (const API_Guid& guid);
GS::ErrCode GetNonLocalizedElementTypeName (const API_Elem_Head& header, GS::UniString& typeName);
GS::ErrCode GetLocalizedElementTypeName (const API_Elem_Head& header, GS::UniString& typeName);
void SetElementType (API_Elem_Head& header, const API_ElemTypeID& elementType);

bool ElementExists (const API_Guid& guid);

bool IsElement3D (const API_Guid& guid);

GSErrCode GetBaseElementData (API_Element& elem, API_ElementMemo* memo, API_SubElement** marker, GS::Array<GS::UniString>& log);

GS::Array<API_StoryType> GetStoryItems ();

API_StoryType GetStory (short floorNumber);

double GetStoryLevel (short floorNumber);

void SetStoryLevelAndFloor (const double& inLevel, short& floorInd, double& level);

void SetStoryLevel (const double& inLevel, const short& floorInd, double& level);

GS::Array<API_Guid> GetElementSubelements (API_Element& element);

// API_AssemblySegmentData
GSErrCode GetSegmentData (const API_AssemblySegmentData&, GS::ObjectState&);
GSErrCode CreateOneSegmentData (GS::ObjectState&, API_AssemblySegmentData&, API_Element&);

// API_AssemblySegmentSchemeData
GSErrCode GetOneSchemeData (const API_AssemblySegmentSchemeData&, GS::ObjectState&);
GSErrCode GetAllSchemeData (API_AssemblySegmentSchemeData*, GS::ObjectState&);
GSErrCode CreateOneSchemeData (GS::ObjectState&, API_AssemblySegmentSchemeData&, API_Element&);
GSErrCode CreateAllSchemeData (const GS::ObjectState&, GS::UInt32&, API_Element&, API_Element&, API_ElementMemo*);

// API_AssemblySegmentCutData
GSErrCode GetOneCutData (const API_AssemblySegmentCutData&, GS::ObjectState&);
GSErrCode GetAllCutData (API_AssemblySegmentCutData*, GS::ObjectState&);
GSErrCode CreateOneCutData (GS::ObjectState&, API_AssemblySegmentCutData&, API_Element&);
GSErrCode CreateAllCutData (const GS::ObjectState&, GS::UInt32&, API_Element&, API_Element&, API_ElementMemo*);

// API_PivotPolyEdgeData
GSErrCode GetOneLevelEdgeData (const API_RoofSegmentData& levelEdgeData, GS::ObjectState& out);
GSErrCode GetOnePivotPolyEdgeData (const API_PivotPolyEdgeData& pivotPolyEdgeData, GS::ObjectState& out);
GSErrCode GetAllPivotPolyEdgeData (API_PivotPolyEdgeData* pivotPolyEdgeData, GS::ObjectState& out);
GSErrCode CreateOneLevelEdgeData (GS::ObjectState& currentLevelEdge, API_RoofSegmentData& levelEdgeData);
GSErrCode CreateOnePivotPolyEdgeData (GS::ObjectState& currentPivotPolyEdge, API_PivotPolyEdgeData& pivotPolyEdgeData);
GSErrCode CreateAllPivotPolyEdgeData (GS::ObjectState& allPivotPolyEdges, GS::UInt32& numberOfPivotPolyEdges, API_ElementMemo* memo);

// Visibility
GSErrCode GetPredefinedVisibility (bool isAutoOnStoryVisibility, API_StoryVisibility visibility, GS::UniString& visibilityString);
GSErrCode GetVisibility (bool isAutoOnStoryVisibility, API_StoryVisibility visibility, GS::ObjectState& os, const char* fieldName, bool getVisibilityValues = false);
GSErrCode CreatePredefinedVisibility (const GS::UniString& visibilityString, bool& isAutoOnStoryVisibility, API_StoryVisibility& visibility);
GSErrCode CreateVisibility (const GS::ObjectState& os, const char* fieldName, bool& isAutoOnStoryVisibility, API_StoryVisibility& visibility);

// Cover Fill Transformation
GSErrCode GetCoverFillTransformation (bool coverFillOrientationComesFrom3D, API_CoverFillTransformationTypeID coverFillTransformationType, GS::ObjectState& os);
GSErrCode CreateCoverFillTransformation (const GS::ObjectState& os, bool& coverFillOrientationComesFrom3D, API_CoverFillTransformationTypeID& coverFillTransformationType);

// Hatch Orientation
GSErrCode GetHatchOrientation (API_HatchOrientationTypeID hatchOrientationType, GS::ObjectState& os);
GSErrCode CreateHatchOrientation (const GS::ObjectState& os, API_HatchOrientationTypeID& hatchOrientationType);

// Transformation matrix
GSErrCode GetTransform (API_Tranmat transform, GS::ObjectState& out);
GSErrCode CreateTransform (const GS::ObjectState& os, API_Tranmat& transform);

// Conversion logging
template<typename... Args>
GS::UniString ComposeLogMessage (const Int32 resourceIndex, Args... args)
{
	GS::UniString errMsgFromatString;
	RSGetIndString (&errMsgFromatString, ID_LOG_MESSAGES, resourceIndex, ACAPI_GetOwnResModule ());
	return GS::UniString::Printf (errMsgFromatString, args...);
}

// Geometry helpers
GSErrCode ConstructPoly2DDataFromElementMemo (const API_ElementMemo& memo, Geometry::Polygon2DData& polygon2DData);

}


#endif
