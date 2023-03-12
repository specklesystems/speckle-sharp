#ifndef UTILITY_HPP
#define UTILITY_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"


namespace Utility {


API_ElemTypeID GetElementType (const API_Guid& guid);

bool ElementExists (const API_Guid& guid);

bool IsElement3D (const API_Guid& guid);

GSErrCode GetBaseElementData (API_Element& elem, API_ElementMemo* memo = nullptr);

GS::Array<API_StoryType> GetStoryItems ();

double GetStoryLevel (short floorNumber);

void SetStoryLevelAndFloor (const double& inLevel, short& floorInd, double& level);

void SetStoryLevel (const double& inLevel, const short& floorInd, double& level);

GS::Array<API_Guid> GetWallSubelements (API_WallType& wall);

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

// Visibility
GSErrCode GetVisibility (bool isAutoOnStoryVisibility, API_StoryVisibility visibility, GS::UniString& visibilityString);
GSErrCode ExportVisibility (bool isAutoOnStoryVisibility, API_StoryVisibility visibility, GS::ObjectState& os, const char* fieldName, bool exportVisibilityValues = false);
GSErrCode SetVisibility (const GS::UniString& visibilityString, bool& isAutoOnStoryVisibility, API_StoryVisibility& visibility);
GSErrCode ImportVisibility (const GS::ObjectState& os, const char* fieldName, bool& isAutoOnStoryVisibility, API_StoryVisibility& visibility);

// Cover Fill Transformation
GSErrCode ExportCoverFillTransformation (bool coverFillOrientationComesFrom3D, API_CoverFillTransformationTypeID coverFillTransformationType, GS::ObjectState& os);
GSErrCode ImportCoverFillTransformation (const GS::ObjectState& os, bool& coverFillOrientationComesFrom3D, API_CoverFillTransformationTypeID& coverFillTransformationType);

// Hatch Orientation
GSErrCode ExportHatchOrientation (API_HatchOrientationTypeID hatchOrientationType, GS::ObjectState& os);
GSErrCode ImportHatchOrientation (const GS::ObjectState& os, API_HatchOrientationTypeID& hatchOrientationType);

}


#endif
