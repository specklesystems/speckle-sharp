#include "APIMigrationHelper.hpp"
#include "Utility.hpp"
#include "ObjectState.hpp"
#include "RealNumber.h"
#include "ObjectState.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
#include "ResourceStrings.hpp"
#include "Polygon2DData.h"
using namespace FieldNames;

namespace Utility {


API_ElemType GetElementType (const API_Elem_Head& header)
{
#ifdef ServerMainVers_2600
	return header.type;
#else
	return API_ElemType (header.typeID, header.variationID);
#endif
}


API_ElemType GetElementType (const API_Guid& guid)
{
	API_Elem_Head elemHead = {};
	elemHead.guid = guid;

	GSErrCode error = ACAPI_Element_GetHeader (&elemHead);
	if (error == NoError)
		return GetElementType (elemHead);

	return API_ZombieElemID;
}


GSErrCode GetElementTypeStringItem (const API_Elem_Head& header, ResourceStrings::ElementTypeStringItems& elementTypeStringItem)
{
#ifdef ServerMainVers_2600
	switch (header.type.typeID) {
#else
	switch (header.typeID) {
#endif
	case API_WallID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::WallString; break;
	case API_ColumnID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::ColumnString; break;
	case API_BeamID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::BeamString; break;
	case API_WindowID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::WindowString; break;
	case API_DoorID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::DoorString; break;
	case API_ObjectID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::ObjectString; break;
	case API_LampID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::LampString; break;
	case API_SlabID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::SlabString; break;
	case API_RoofID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RoofString; break;
	case API_MeshID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::MeshString; break;
	case API_DimensionID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::DimensionString; break;
	case API_RadialDimensionID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RadialDimensionString; break;
	case API_LevelDimensionID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::LevelDimensionString; break;
	case API_AngleDimensionID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::AngleDimensionString; break;
	case API_TextID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::TextString; break;
	case API_LabelID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::LabelString; break;
	case API_ZoneID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::ZoneString; break;
	case API_HatchID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::HatchString; break;
	case API_LineID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::LineString; break;
	case API_PolyLineID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::PolyLineString; break;
	case API_ArcID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::ArcString; break;
	case API_CircleID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::CircleString; break;
	case API_SplineID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::SplineString; break;
	case API_HotspotID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::HotspotString; break;
	case API_CutPlaneID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::CutPlaneString; break;
	case API_CameraID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::CameraString; break;
	case API_CamSetID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::CamSetString; break;
	case API_GroupID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::GroupString; break;
	case API_SectElemID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::SectElemString; break;
	case API_DrawingID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::DrawingString; break;
	case API_PictureID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::PictureString; break;
	case API_DetailID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::DetailString; break;
	case API_ElevationID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::ElevationString; break;
	case API_InteriorElevationID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::InteriorElevationString; break;
	case API_WorksheetID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::WorksheetString; break;
	case API_HotlinkID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::HotlinkString; break;
	case API_CurtainWallID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::CurtainWallString; break;
	case API_CurtainWallSegmentID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::CurtainWallSegmentString; break;
	case API_CurtainWallFrameID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::CurtainWallFrameString; break;
	case API_CurtainWallPanelID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::CurtainWallPanelString; break;
	case API_CurtainWallJunctionID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::CurtainWallJunctionString; break;
	case API_CurtainWallAccessoryID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::CurtainWallAccessoryString; break;
	case API_ShellID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::ShellString; break;
	case API_SkylightID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::SkylightString; break;
	case API_MorphID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::MorphString; break;
	case API_ChangeMarkerID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::ChangeMarkerString; break;
	case API_StairID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::StairString; break;
	case API_RiserID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RiserString; break;
	case API_TreadID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::TreadString; break;
	case API_StairStructureID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::StairStructureString; break;
	case API_RailingID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RailingString; break;
	case API_RailingToprailID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RailingToprailString; break;
	case API_RailingHandrailID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RailingHandrailString; break;
	case API_RailingRailID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RailingRailString; break;
	case API_RailingPostID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RailingPostString; break;
	case API_RailingInnerPostID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RailingInnerPostString; break;
	case API_RailingBalusterID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RailingBalusterString; break;
	case API_RailingPanelID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RailingPanelString; break;
	case API_RailingSegmentID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RailingSegmentString; break;
	case API_RailingNodeID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RailingNodeString; break;
	case API_RailingBalusterSetID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RailingBalusterSetString; break;
	case API_RailingPatternID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RailingPatternString; break;
	case API_RailingToprailEndID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RailingToprailEndString; break;
	case API_RailingHandrailEndID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RailingHandrailEndString; break;
	case API_RailingRailEndID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RailingRailEndString; break;
	case API_RailingToprailConnectionID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RailingToprailConnectionString; break;
	case API_RailingHandrailConnectionID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RailingHandrailConnectionString; break;
	case API_RailingRailConnectionID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RailingRailConnectionString; break;
	case API_RailingEndFinishID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::RailingEndFinishString; break;
#ifndef ServerMainVers_2600
	case API_AnalyticalSupportID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::AnalyticalSupportString; break;
	case API_AnalyticalLinkID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::AnalyticalLinkString; break;
#endif
	case API_BeamSegmentID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::BeamSegmentString; break;
	case API_ColumnSegmentID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::ColumnSegmentString; break;
	case API_OpeningID: elementTypeStringItem = ResourceStrings::ElementTypeStringItems::OpeningString; break;
	default:
		return Error;
	}
			
	return Error;
}


GS::ErrCode GetNonLocalizedElementTypeName (const API_Elem_Head& header, GS::UniString& typeName)
{
	ResourceStrings::ElementTypeStringItems elementTypeStringItem;
	GetElementTypeStringItem (header, elementTypeStringItem);
	typeName = GetFixElementTypeStringFromResource (elementTypeStringItem);

	return typeName.IsEmpty () ? Error : NoError;
}


GS::ErrCode GetLocalizedElementTypeName (const API_Elem_Head& header, GS::UniString& typeName)
{
#ifdef ServerMainVers_2700
	return ACAPI_Element_GetElemTypeName (header.type, typeName);
#elif ServerMainVers_2600
	return ACAPI_Goodies_GetElemTypeName (header.type, typeName);
#else
	ResourceStrings::ElementTypeStringItems elementTypeStringItem;
	GetElementTypeStringItem (header, elementTypeStringItem);
	typeName = GetElementTypeStringFromResource (elementTypeStringItem);

	return typeName.IsEmpty () ? Error : NoError;
#endif
}


void SetElementType (API_Elem_Head& header, const API_ElemTypeID& elementType)
{
	SetElementType (header, API_ElemType (elementType, APIVarId_Generic));
}


void SetElementType (API_Elem_Head& header, const API_ElemType& elementType)
{
#ifdef ServerMainVers_2600
	header.type = elementType;
#else
	header.typeID = elementType.typeID;
	header.variationID = elementType.variationID;
#endif
}

	
bool ElementExists (const API_Guid& guid)
{
	return (GetElementType (guid) != API_ZombieElemID);
}


GSErrCode GetBaseElementData (API_Element& element, API_ElementMemo* memo, API_SubElement** marker, GS::Array<GS::UniString>& log)
{
	GSErrCode err = NoError;
	API_Guid guid = element.header.guid;

	API_ElemTypeID type = GetElementType (element.header).typeID;
	if (type == API_ZombieElemID)
		return Error;

	bool elemExists = ElementExists (guid);
	if (elemExists) {
		// type changed
		if (type != GetElementType (guid).typeID)
			return Error;

		err = ACAPI_Element_Get (&element);
		if (err == NoError && memo != nullptr) {
			err = ACAPI_Element_GetMemo (guid, memo);
		}
	} else {
		if (marker != nullptr) {
			BNZeroMemory (*marker, sizeof (API_SubElement));
			(*marker)->subType = APISubElement_MainMarker;

			err = ACAPI_Element_GetDefaultsExt (&element, memo, 1UL, *marker);
			if (err != NoError)
				return err;

			API_LibPart libPart;
			BNZeroMemory (&libPart, sizeof (API_LibPart));

#ifdef ServerMainVers_2600
			err = ACAPI_LibraryPart_GetMarkerParent (element.header.type, libPart);
#else
			err = ACAPI_Goodies (APIAny_GetMarkerParentID, &element.header.typeID, &libPart);
#endif
			if (err != NoError)
				return err;

			err = ACAPI_LibraryPart_Search (&libPart, false, true);
			if (libPart.location != nullptr)
				delete libPart.location;

			if (err != NoError) {
				log.Push (ComposeLogMessage (ID_LOG_MESSAGE_DEFAULT_MARKER_SEARCH_ERROR));
				return err;
			}

			double a = .0, b = .0;
			Int32 addParNum = 0;
			API_AddParType** markAddPars;
			err = ACAPI_LibraryPart_GetParams (libPart.index, &a, &b, &addParNum, &markAddPars);
			if (err != NoError)
				return err;

			(*marker)->memo.params = markAddPars;
		} else {
			err = ACAPI_Element_GetDefaults (&element, memo);
		}

		element.header.guid = guid;	// keep guid for creation
	}

	return err;
}


bool IsElement3D (const API_Guid& guid)
{
	switch (GetElementType (guid).typeID) {
	case API_WallID:
	case API_ColumnID:
	case API_BeamID:
	case API_WindowID:
	case API_DoorID:
	case API_ObjectID:
	case API_LampID:
	case API_SlabID:
	case API_RoofID:
	case API_MeshID:
	case API_ZoneID:
	case API_CurtainWallID:
	case API_ShellID:
	case API_SkylightID:
	case API_MorphID:
	case API_StairID:
	case API_RailingID:
	case API_OpeningID:
		return true;
	default: return false;
	}
}


GS::Array<API_StoryType> GetStoryItems ()
{
	GS::Array<API_StoryType> stories;

	API_StoryInfo storyInfo{};
	GSErrCode err = ACAPI_ProjectSetting_GetStorySettings (&storyInfo /* , nullptr */);
	if (err != NoError) {
		return stories;
	}

	short idx = 0;
	for (short i = storyInfo.firstStory; i <= storyInfo.lastStory; i++)
		stories.Push ((*storyInfo.data)[idx++]);

	BMKillHandle ((GSHandle*) &storyInfo.data);

	return stories;
}


API_StoryType GetStory (short floorIndex)
{
	const GS::Array<API_StoryType> stories = GetStoryItems ();

	for (const auto& story : stories) {
		if (story.index == floorIndex) {
			return story;
		}
	}

	return API_StoryType{};
}


double GetStoryLevel (short floorIndex)
{
	const GS::Array<API_StoryType> stories = GetStoryItems ();

	for (const auto& story : stories) {
		if (story.index == floorIndex) {
			return story.level;
		}
	}

	return 0.0;
}


void SetStoryLevel (const double& inLevel, const short& floorIndex, double& level)
{
	const GS::Array<API_StoryType> stories = GetStoryItems ();
	level = inLevel;
	for (const auto& story : stories) {
		if (story.index == floorIndex) {
			level = level - story.level;
			break;
		}
	}
}


static API_StoryType GetActualStoryItem ()
{
	API_StoryInfo storyInfo{};
	GSErrCode err = ACAPI_ProjectSetting_GetStorySettings (&storyInfo);
	if (err != NoError) {
		return API_StoryType{};
	}

	for (Int32 i = storyInfo.lastStory - storyInfo.firstStory; i >= 0; i--) {
		if ((*storyInfo.data)[i].index == storyInfo.actStory) {
			return (*storyInfo.data)[i];
		}
	}

	return API_StoryType{};
}


void SetStoryLevelAndFloor (const double& inLevel, short& floorInd, double& level)
{
	const GS::Array<API_StoryType> stories = GetStoryItems ();

	floorInd = 0;
	level = inLevel;
	for (const auto& story : stories) {
		if (inLevel + EPS >= story.level) {
			floorInd = story.index;
			level = inLevel - story.level;
		}
	}

	double bottomLevel = stories[0].level;
	if (inLevel < bottomLevel)
		level = inLevel - bottomLevel;

	API_WindowInfo windowInfo;
	BNZeroMemory (&windowInfo, sizeof (API_WindowInfo));
	GSErrCode err = ACAPI_Window_GetCurrentWindow(&windowInfo);
	if (err == NoError && (windowInfo.typeID != APIWind_FloorPlanID && windowInfo.typeID != APIWind_3DModelID))
		floorInd += GetActualStoryItem ().index;
}


GS::Array<API_Guid> GetElementSubelements (API_Element& element)
{
	GS::Array<API_Guid> result;

	API_ElemTypeID elementType = GetElementType (element.header).typeID;

	if (elementType == API_WallID) {
		if (element.wall.hasDoor) {
			GS::Array<API_Guid> doors;
			GSErrCode err = ACAPI_Grouping_GetConnectedElements (element.header.guid, API_DoorID, &doors);
			if (err == NoError)
				result.Append (doors);
		}
		if (element.wall.hasWindow) {
			GS::Array<API_Guid> windows;
			GSErrCode err = ACAPI_Grouping_GetConnectedElements (element.header.guid, API_WindowID, &windows);
			if (err == NoError)
				result.Append (windows);
		}
	} else if ((elementType == API_RoofID) || (elementType == API_ShellID)) {
		GS::Array<API_Guid> skylights;
		GSErrCode err = ACAPI_Grouping_GetConnectedElements (element.header.guid, API_SkylightID, &skylights);
		if (err == NoError)
			result.Append (skylights);
	}

	return result;
}


GSErrCode GetSegmentData (const API_AssemblySegmentData& segmentData, GS::ObjectState& out)
{
	// Currently there is no any serious case to check with GSErrCode, may be useful later.
	out.Add (AssemblySegmentData::circleBased, segmentData.circleBased);
	out.Add (AssemblySegmentData::modelElemStructureType, structureTypeNames.Get (segmentData.modelElemStructureType));
	out.Add (AssemblySegmentData::nominalHeight, segmentData.nominalHeight);
	out.Add (AssemblySegmentData::nominalWidth, segmentData.nominalWidth);
	out.Add (AssemblySegmentData::isWidthAndHeightLinked, segmentData.isWidthAndHeightLinked);
	out.Add (AssemblySegmentData::isHomogeneous, segmentData.isHomogeneous);
	out.Add (AssemblySegmentData::endWidth, segmentData.endWidth);
	out.Add (AssemblySegmentData::endHeight, segmentData.endHeight);
	out.Add (AssemblySegmentData::isEndWidthAndHeightLinked, segmentData.isEndWidthAndHeightLinked);

	API_Attribute attrib;
	switch (segmentData.modelElemStructureType) {
	case API_CompositeStructure:
		DBASSERT (segmentData.modelElemStructureType != API_CompositeStructure);
		break;
	case API_BasicStructure:
		BNZeroMemory (&attrib, sizeof (API_Attribute));
		attrib.header.typeID = API_BuildingMaterialID;
		attrib.header.index = segmentData.buildingMaterial;
		ACAPI_Attribute_Get (&attrib);

		out.Add (AssemblySegmentData::buildingMaterial, GS::UniString{attrib.header.name});
		break;
	case API_ProfileStructure:
		BNZeroMemory (&attrib, sizeof (API_Attribute));
		attrib.header.typeID = API_ProfileID;
		attrib.header.index = segmentData.profileAttr;
		ACAPI_Attribute_Get (&attrib);

		out.Add (AssemblySegmentData::profileAttrName, GS::UniString{attrib.header.name});
		break;
	default:
		break;

	}

	return NoError;
}


GSErrCode CreateOneSegmentData (GS::ObjectState& currentSegment, API_AssemblySegmentData& segmentData, API_Element& columnMask)
{
	GSErrCode err = NoError;
	if (!currentSegment.IsEmpty ()) {

		if (currentSegment.Contains (AssemblySegmentData::circleBased))
			currentSegment.Get (AssemblySegmentData::circleBased, segmentData.circleBased);
		ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnSegmentType, assemblySegmentData.circleBased);

		if (currentSegment.Contains (AssemblySegmentData::nominalHeight))
			currentSegment.Get (AssemblySegmentData::nominalHeight, segmentData.nominalHeight);
		ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnSegmentType, assemblySegmentData.nominalHeight);

		if (currentSegment.Contains (AssemblySegmentData::nominalWidth))
			currentSegment.Get (AssemblySegmentData::nominalWidth, segmentData.nominalWidth);
		ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnSegmentType, assemblySegmentData.nominalWidth);

		if (currentSegment.Contains (AssemblySegmentData::isWidthAndHeightLinked))
			currentSegment.Get (AssemblySegmentData::isWidthAndHeightLinked, segmentData.isWidthAndHeightLinked);
		ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnSegmentType, assemblySegmentData.isWidthAndHeightLinked);

		if (currentSegment.Contains (AssemblySegmentData::isHomogeneous))
			currentSegment.Get (AssemblySegmentData::isHomogeneous, segmentData.isHomogeneous);
		ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnSegmentType, assemblySegmentData.isHomogeneous);

		if (currentSegment.Contains (AssemblySegmentData::endWidth))
			currentSegment.Get (AssemblySegmentData::endWidth, segmentData.endWidth);
		ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnSegmentType, assemblySegmentData.endWidth);

		if (currentSegment.Contains (AssemblySegmentData::endHeight))
			currentSegment.Get (AssemblySegmentData::endHeight, segmentData.endHeight);
		ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnSegmentType, assemblySegmentData.endHeight);

		if (currentSegment.Contains (AssemblySegmentData::isEndWidthAndHeightLinked))
			currentSegment.Get (AssemblySegmentData::isEndWidthAndHeightLinked, segmentData.isEndWidthAndHeightLinked);
		ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnSegmentType, assemblySegmentData.isEndWidthAndHeightLinked);

		if (currentSegment.Contains (AssemblySegmentData::modelElemStructureType)) {
			API_ModelElemStructureType realStructureType = API_BasicStructure;
			GS::UniString structureName;
			currentSegment.Get (AssemblySegmentData::modelElemStructureType, structureName);

			GS::Optional<API_ModelElemStructureType> tmpStructureType = structureTypeNames.FindValue (structureName);
			if (tmpStructureType.HasValue ())
				realStructureType = tmpStructureType.Get ();
			segmentData.modelElemStructureType = realStructureType;
		}
		ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnSegmentType, assemblySegmentData.modelElemStructureType);

		if (currentSegment.Contains (AssemblySegmentData::profileAttrName)) {
			GS::UniString attrName;
			currentSegment.Get (AssemblySegmentData::profileAttrName, attrName);

			if (!attrName.IsEmpty ()) {
				API_Attribute attrib;
				BNZeroMemory (&attrib, sizeof (API_Attribute));
				attrib.header.typeID = API_ProfileID;
				CHCopyC (attrName.ToCStr (), attrib.header.name);
				err = ACAPI_Attribute_Get (&attrib);

				if (err == NoError)
					segmentData.profileAttr = attrib.header.index;
			}
		}

		if (currentSegment.Contains (AssemblySegmentData::buildingMaterial)) {
			GS::UniString attrName;
			currentSegment.Get (AssemblySegmentData::buildingMaterial, attrName);

			if (!attrName.IsEmpty ()) {
				API_Attribute attrib;
				BNZeroMemory (&attrib, sizeof (API_Attribute));
				attrib.header.typeID = API_BuildingMaterialID;
				CHCopyC (attrName.ToCStr (), attrib.header.name);
				err = ACAPI_Attribute_Get (&attrib);

				if (err == NoError)
					segmentData.buildingMaterial = attrib.header.index;
			}
		}
	}

	return err;
}


GSErrCode GetOneSchemeData (const API_AssemblySegmentSchemeData& schemeData, GS::ObjectState& out)
{
	out.Add (AssemblySegmentSchemeData::lengthType, segmentLengthTypeNames.Get (schemeData.lengthType));
	out.Add (AssemblySegmentSchemeData::fixedLength, schemeData.fixedLength);
	out.Add (AssemblySegmentSchemeData::lengthProportion, schemeData.lengthProportion);

	return NoError;
}


GSErrCode GetAllSchemeData (API_AssemblySegmentSchemeData* schemeData, GS::ObjectState& out)
{
	if (schemeData == nullptr)
		return Error;

	GSSize schemesCount = BMGetPtrSize (reinterpret_cast<GSPtr>(schemeData)) / sizeof (API_AssemblySegmentSchemeData);

	for (GSSize idx = 0; idx < schemesCount; ++idx) {
		GS::ObjectState currentScheme;
		Utility::GetOneSchemeData (schemeData[idx], currentScheme);
		out.Add (GS::String::SPrintf (AssemblySegment::SchemeName, idx + 1), currentScheme);
	}

	return NoError;
}


GSErrCode CreateOneSchemeData (GS::ObjectState& currentScheme, API_AssemblySegmentSchemeData& schemeData, API_Element& mask)
{
	UNUSED_PARAMETER (mask); // TODO: add ACAPI_ELEMENT_MASK_SET things
	GSErrCode err = NoError;

	if (!currentScheme.IsEmpty ()) {

		if (currentScheme.Contains (AssemblySegmentSchemeData::lengthType)) {
			API_AssemblySegmentLengthTypeID lengthType = APIAssemblySegment_Fixed;
			GS::UniString lengthTypeName;
			currentScheme.Get (AssemblySegmentSchemeData::lengthType, lengthTypeName);

			GS::Optional<API_AssemblySegmentLengthTypeID> type = segmentLengthTypeNames.FindValue (lengthTypeName);
			if (type.HasValue ())
				lengthType = type.Get ();
			schemeData.lengthType = lengthType;

			if (lengthType == APIAssemblySegment_Fixed && currentScheme.Contains (AssemblySegmentSchemeData::fixedLength)) {
				currentScheme.Get (AssemblySegmentSchemeData::fixedLength, schemeData.fixedLength);
				schemeData.lengthProportion = 0.0;
			} else if (lengthType == APIAssemblySegment_Proportional && currentScheme.Contains (AssemblySegmentSchemeData::lengthProportion)) {
				currentScheme.Get (AssemblySegmentSchemeData::lengthProportion, schemeData.lengthProportion);
				schemeData.fixedLength = 0.0;
			}
		}
	}
	return err;
}


GSErrCode CreateAllSchemeData (const GS::ObjectState& os,
	GS::UInt32& numberOfCuts,
	API_Element& element,
	API_Element& mask,
	API_ElementMemo* memo)
{
	GSErrCode err = NoError;
	API_AssemblySegmentSchemeData defaultSegmentScheme;
	if (memo->assemblySegmentSchemes != nullptr) {
		defaultSegmentScheme = memo->assemblySegmentSchemes[0];

		switch (Utility::GetElementType (element.header).typeID) {
		case API_BeamID:
			memo->assemblySegmentSchemes = (API_AssemblySegmentSchemeData*) BMAllocatePtr ((element.beam.nSchemes) * sizeof (API_AssemblySegmentSchemeData), ALLOCATE_CLEAR, 0);
			break;
		case API_ColumnID:
			memo->assemblySegmentSchemes = (API_AssemblySegmentSchemeData*) BMAllocatePtr ((element.column.nSchemes) * sizeof (API_AssemblySegmentSchemeData), ALLOCATE_CLEAR, 0);
			break;
		default:  // In case if not beam or column
			return Error;
			break;
		}
	} else {
		return Error;
	}

	GS::ObjectState allSchemes;
	if (os.Contains (AssemblySegment::SchemeData))
		os.Get (AssemblySegment::SchemeData, allSchemes);

	for (UInt32 idx = 0; idx < numberOfCuts; ++idx) {
		if (!allSchemes.IsEmpty ()) {
			GS::ObjectState currentScheme;
			allSchemes.Get (GS::String::SPrintf (AssemblySegment::SchemeName, idx + 1), currentScheme);

			memo->assemblySegmentSchemes[idx] = defaultSegmentScheme;
			Utility::CreateOneSchemeData (currentScheme, memo->assemblySegmentSchemes[idx], mask);
		}
	}
	return err;
}


GSErrCode GetOneCutData (const API_AssemblySegmentCutData& cutData, GS::ObjectState& out)
{
	out.Add (AssemblySegmentCutData::cutType, assemblySegmentCutTypeNames.Get (cutData.cutType));
	out.Add (AssemblySegmentCutData::customAngle, cutData.customAngle);

	return NoError;
}


GSErrCode GetAllCutData (API_AssemblySegmentCutData* cutData, GS::ObjectState& out)
{
	if (cutData == nullptr) return Error;

	GSSize cutsCount = BMGetPtrSize (reinterpret_cast<GSPtr>(cutData)) / sizeof (API_AssemblySegmentCutData);

	for (GSSize idx = 0; idx < cutsCount; ++idx) {
		GS::ObjectState currentCut;
		Utility::GetOneCutData (cutData[idx], currentCut);
		out.Add (GS::String::SPrintf (AssemblySegment::CutName, idx + 1), currentCut);
	}

	return NoError;
}


GSErrCode CreateOneCutData (GS::ObjectState& currentCut, API_AssemblySegmentCutData& cutData, API_Element& mask)
{
	UNUSED_PARAMETER (mask); // TODO: add ACAPI_ELEMENT_MASK_SET things
	GSErrCode err = NoError;

	if (!currentCut.IsEmpty ()) {

		if (currentCut.Contains (AssemblySegmentCutData::cutType)) {
			API_AssemblySegmentCutTypeID realCutType = APIAssemblySegmentCut_Vertical;
			GS::UniString structureName;
			currentCut.Get (AssemblySegmentCutData::cutType, structureName);

			GS::Optional<API_AssemblySegmentCutTypeID> tmpCutType = assemblySegmentCutTypeNames.FindValue (structureName);
			if (tmpCutType.HasValue ())
				realCutType = tmpCutType.Get ();
			cutData.cutType = realCutType;
		}
		if (currentCut.Contains (AssemblySegmentCutData::customAngle)) {
			currentCut.Get (AssemblySegmentCutData::customAngle, cutData.customAngle);
		}
	}
	return err;
}


GSErrCode CreateAllCutData (const GS::ObjectState& os, GS::UInt32& numberOfCuts, API_Element& element, API_Element& mask, API_ElementMemo* memo)
{
	GSErrCode err = NoError;
	API_AssemblySegmentCutData defaultSegmentCut;
	if (memo->assemblySegmentCuts != nullptr) {
		defaultSegmentCut = memo->assemblySegmentCuts[0];

		switch (GetElementType (element.header).typeID) {
		case API_BeamID:
			memo->assemblySegmentCuts = (API_AssemblySegmentCutData*) BMAllocatePtr ((element.beam.nCuts) * sizeof (API_AssemblySegmentCutData), ALLOCATE_CLEAR, 0);
			break;
		case API_ColumnID:
			memo->assemblySegmentCuts = (API_AssemblySegmentCutData*) BMAllocatePtr ((element.column.nCuts) * sizeof (API_AssemblySegmentCutData), ALLOCATE_CLEAR, 0);
		default: // In case if not beam or column
			return Error;
			break;
		}

	} else {
		return Error;
	}

	GS::ObjectState allCuts;
	if (os.Contains (AssemblySegment::CutData))
		os.Get (AssemblySegment::CutData, allCuts);

	for (GS::UInt32 idx = 0; idx < numberOfCuts; ++idx) {
		GS::ObjectState currentCut;
		allCuts.Get (GS::String::SPrintf (AssemblySegment::CutName, idx + 1), currentCut);

		memo->assemblySegmentCuts[idx] = defaultSegmentCut;
		Utility::CreateOneCutData (currentCut, memo->assemblySegmentCuts[idx], mask);
	}
	return err;
}


GSErrCode GetOneLevelEdgeData (const API_RoofSegmentData& levelEdgeData, GS::ObjectState& out)
{
	out.Add (RoofSegmentData::LevelAngle, levelEdgeData.angle);
	out.Add (RoofSegmentData::EavesOverhang, levelEdgeData.eavesOverhang);

	API_Attribute attribute;

	// Top Material
	BNZeroMemory (&attribute, sizeof (API_Attribute));
	attribute.header.typeID = API_MaterialID;
	attribute.header.index = levelEdgeData.topMaterial;

	if (NoError == ACAPI_Attribute_Get (&attribute))
		out.Add (RoofSegmentData::TopMaterial, GS::UniString{attribute.header.name});

	// Bottom Material
	BNZeroMemory (&attribute, sizeof (API_Attribute));
	attribute.header.typeID = API_MaterialID;
	attribute.header.index = levelEdgeData.bottomMaterial;

	if (NoError == ACAPI_Attribute_Get (&attribute))
		out.Add (RoofSegmentData::BottomMaterial, GS::UniString{attribute.header.name});

	// Cover Fill Type
	BNZeroMemory (&attribute, sizeof (API_Attribute));
	attribute.header.typeID = API_FilltypeID;
	attribute.header.index = levelEdgeData.coverFillType;

	if (NoError == ACAPI_Attribute_Get (&attribute))
		out.Add (RoofSegmentData::CoverFillType, GS::UniString{attribute.header.name});

	// Angle Type
	out.Add (RoofSegmentData::AngleType, polyRoofSegmentAngleTypeNames.Get (levelEdgeData.angleType));

	return NoError;
}


GSErrCode GetOnePivotPolyEdgeData (const API_PivotPolyEdgeData& pivotPolyEdgeData, GS::ObjectState& out)
{
	out.Add (PivotPolyEdgeData::NumLevelEdgeData, pivotPolyEdgeData.nLevelEdgeData);

	GS::ObjectState levelEdgeData;

	for (GSSize idx = 0; idx < pivotPolyEdgeData.nLevelEdgeData; ++idx) {
		GS::ObjectState currentLevelEdge;
		Utility::GetOneLevelEdgeData (pivotPolyEdgeData.levelEdgeData[idx], currentLevelEdge);
		levelEdgeData.Add (GS::String::SPrintf (LevelEdge::LevelEdgeName, idx + 1), currentLevelEdge);
	}
	out.Add (LevelEdge::LevelEdgeData, levelEdgeData);

	return NoError;
}


GSErrCode GetAllPivotPolyEdgeData (API_PivotPolyEdgeData* pivotPolyEdgeData, GS::ObjectState& out)
{
	if (pivotPolyEdgeData == nullptr) return Error;

	GSSize pivotPolyEdgesCount = BMGetPtrSize (reinterpret_cast<GSPtr>(pivotPolyEdgeData)) / sizeof (API_PivotPolyEdgeData);

	for (GSSize idx = 1; idx < pivotPolyEdgesCount; ++idx) {
		GS::ObjectState currentPivotPolyEdge;
		Utility::GetOnePivotPolyEdgeData (pivotPolyEdgeData[idx], currentPivotPolyEdge);
		out.Add (GS::String::SPrintf (PivotPolyEdge::EdgeName, idx), currentPivotPolyEdge);
	}

	return NoError;
}


GSErrCode CreateOneLevelEdgeData (GS::ObjectState& currentLevelEdge, API_RoofSegmentData& levelEdgeData)
{
	GSErrCode err = NoError;
	if (!currentLevelEdge.IsEmpty ()) {

		if (currentLevelEdge.Contains (RoofSegmentData::LevelAngle))
			currentLevelEdge.Get (RoofSegmentData::LevelAngle, levelEdgeData.angle);

		if (currentLevelEdge.Contains (RoofSegmentData::EavesOverhang))
			currentLevelEdge.Get (RoofSegmentData::EavesOverhang, levelEdgeData.eavesOverhang);

		GS::UniString attributeName;

		// Top Material
		if (currentLevelEdge.Contains (RoofSegmentData::TopMaterial)) {
			currentLevelEdge.Get (RoofSegmentData::TopMaterial, attributeName);

			if (!attributeName.IsEmpty ()) {
				API_Attribute attribute;
				BNZeroMemory (&attribute, sizeof (API_Attribute));
				attribute.header.typeID = API_MaterialID;
				CHCopyC (attributeName.ToCStr (), attribute.header.name);

				if (NoError == ACAPI_Attribute_Get (&attribute))
					levelEdgeData.topMaterial = attribute.header.index;
			}
		}

		// Bottom Material
		if (currentLevelEdge.Contains (RoofSegmentData::BottomMaterial)) {
			currentLevelEdge.Get (RoofSegmentData::BottomMaterial, attributeName);

			if (!attributeName.IsEmpty ()) {
				API_Attribute attribute;
				BNZeroMemory (&attribute, sizeof (API_Attribute));
				attribute.header.typeID = API_MaterialID;
				CHCopyC (attributeName.ToCStr (), attribute.header.name);

				if (NoError == ACAPI_Attribute_Get (&attribute))
					levelEdgeData.bottomMaterial = attribute.header.index;
			}
		}

		// Cover Fill Type
		if (currentLevelEdge.Contains (RoofSegmentData::CoverFillType)) {
			currentLevelEdge.Get (RoofSegmentData::CoverFillType, attributeName);

			if (!attributeName.IsEmpty ()) {
				API_Attribute attribute;
				BNZeroMemory (&attribute, sizeof (API_Attribute));
				attribute.header.typeID = API_FilltypeID;
				CHCopyC (attributeName.ToCStr (), attribute.header.name);

				if (NoError == ACAPI_Attribute_Get (&attribute))
					levelEdgeData.coverFillType = attribute.header.index;
			}
		}

		// Angle Type
		if (currentLevelEdge.Contains (RoofSegmentData::AngleType)) {
			GS::UniString angleTypeName;
			currentLevelEdge.Get (RoofSegmentData::AngleType, angleTypeName);

			GS::Optional<API_PolyRoofSegmentAngleTypeID> type = polyRoofSegmentAngleTypeNames.FindValue (angleTypeName);
			if (type.HasValue ())
				levelEdgeData.angleType = type.Get ();
		}

	}

	return err;
}


GSErrCode CreateOnePivotPolyEdgeData (GS::ObjectState& currentPivotPolyEdge, API_PivotPolyEdgeData& pivotPolyEdgeData)
{
	if (currentPivotPolyEdge.IsEmpty ())
		return NoError;

	if (currentPivotPolyEdge.Contains (PivotPolyEdgeData::NumLevelEdgeData))
		currentPivotPolyEdge.Get (PivotPolyEdgeData::NumLevelEdgeData, pivotPolyEdgeData.nLevelEdgeData);

	pivotPolyEdgeData.levelEdgeData = (API_RoofSegmentData*) BMAllocatePtr ((pivotPolyEdgeData.nLevelEdgeData) * sizeof (API_RoofSegmentData), ALLOCATE_CLEAR, 0);

	GS::ObjectState levelEdgeData;
	currentPivotPolyEdge.Get (LevelEdge::LevelEdgeData, levelEdgeData);

	for (GSSize idx = 0; idx < pivotPolyEdgeData.nLevelEdgeData; ++idx) {
		GS::ObjectState currentLevelEdge;
		levelEdgeData.Get (GS::String::SPrintf (LevelEdge::LevelEdgeName, idx + 1), currentLevelEdge);

		Utility::CreateOneLevelEdgeData (currentLevelEdge, pivotPolyEdgeData.levelEdgeData[idx]);
	}

	return NoError;
}


GSErrCode CreateAllPivotPolyEdgeData (GS::ObjectState& allPivotPolyEdges, GS::UInt32& numberOfPivotPolyEdges, API_ElementMemo* memo)
{
	memo->pivotPolyEdges = (API_PivotPolyEdgeData*) BMAllocatePtr ((numberOfPivotPolyEdges + 1) * sizeof (API_PivotPolyEdgeData), ALLOCATE_CLEAR, 0);

	for (UInt32 idx = 1; idx <= numberOfPivotPolyEdges; ++idx) {
		GS::ObjectState currentPivotPolyEdge;
		allPivotPolyEdges.Get (GS::String::SPrintf (PivotPolyEdge::EdgeName, idx), currentPivotPolyEdge);

		if (!currentPivotPolyEdge.IsEmpty ())
			Utility::CreateOnePivotPolyEdgeData (currentPivotPolyEdge, memo->pivotPolyEdges[idx]);
	}
	return NoError;
}


GSErrCode GetPredefinedVisibility (bool isAutoOnStoryVisibility, API_StoryVisibility visibility, GS::UniString& visibilityString)
{
	if (isAutoOnStoryVisibility) {
		visibilityString = AllRelevantStoriesValueName;
	} else if (visibility.showOnHome && visibility.showRelAbove == 1 && visibility.showRelBelow == 1) {
		visibilityString = HomeAndOneStoryUpAndDownValueName;
	} else if (visibility.showOnHome && visibility.showRelAbove == 1) {
		visibilityString = HomeAndOneStoryUpValueName;
	} else if (visibility.showOnHome && visibility.showRelBelow == 1) {
		visibilityString = HomeAndOneStoryDownValueName;
	} else if (visibility.showRelAbove == 1) {
		visibilityString = OneStoryUpValueName;
	} else if (visibility.showRelBelow == 1) {
		visibilityString = OneStoryDownValueName;
	} else if (visibility.showOnHome && visibility.showAllAbove && visibility.showAllBelow) {
		visibilityString = AllStoriesValueName;
	} else if (visibility.showOnHome && visibility.showRelAbove == 0 && visibility.showRelBelow == 0) {
		visibilityString = HomeStoryOnlyValueName;
	} else {
		visibilityString = CustomStoriesValueName;
	}

	return NoError;
}


GSErrCode GetVisibility (bool isAutoOnStoryVisibility,
	API_StoryVisibility visibility,
	GS::ObjectState& os,
	const char* fieldName,
	bool getVisibilityValues /*= false*/)
{
	GS::UniString visibilityString;
	if (NoError != GetPredefinedVisibility (isAutoOnStoryVisibility, visibility, visibilityString))
		return Error;

	if (!getVisibilityValues) {
		os.Add (fieldName, visibilityString);
	}

	if (visibilityString == CustomStoriesValueName || getVisibilityValues) {
		GS::ObjectState customVisibilityOs;

		customVisibilityOs.Add (ShowOnHome, visibility.showOnHome);
		customVisibilityOs.Add (ShowAllAbove, visibility.showAllAbove);
		customVisibilityOs.Add (ShowAllBelow, visibility.showAllBelow);
		customVisibilityOs.Add (ShowRelAbove, visibility.showRelAbove);
		customVisibilityOs.Add (ShowRelBelow, visibility.showRelBelow);
		os.Add (fieldName, customVisibilityOs);
	}

	return NoError;
}


GSErrCode CreatePredefinedVisibility (const GS::UniString& visibilityString, bool& isAutoOnStoryVisibility, API_StoryVisibility& visibility)
{
	isAutoOnStoryVisibility = false;
	visibility.showOnHome = true;
	visibility.showAllAbove = false;
	visibility.showAllBelow = false;
	visibility.showRelAbove = 0;
	visibility.showRelBelow = 0;

	if (visibilityString == AllRelevantStoriesValueName) {
		isAutoOnStoryVisibility = true;
		visibility.showOnHome = false;
		visibility.showAllAbove = false;
		visibility.showAllBelow = false;
	} else if (visibilityString == HomeAndOneStoryUpAndDownValueName) {
		isAutoOnStoryVisibility = false;
		visibility.showOnHome = true;
		visibility.showAllAbove = false;
		visibility.showAllBelow = false;
		visibility.showRelAbove = 1;
		visibility.showRelBelow = 1;
	} else if (visibilityString == HomeAndOneStoryUpValueName) {
		isAutoOnStoryVisibility = false;
		visibility.showOnHome = true;
		visibility.showAllAbove = false;
		visibility.showAllBelow = false;
		visibility.showRelAbove = 1;
		visibility.showRelBelow = 0;
	} else if (visibilityString == HomeAndOneStoryDownValueName) {
		isAutoOnStoryVisibility = false;
		visibility.showOnHome = true;
		visibility.showAllAbove = false;
		visibility.showAllBelow = false;
		visibility.showRelAbove = 0;
		visibility.showRelBelow = 1;
	} else if (visibilityString == OneStoryUpValueName) {
		isAutoOnStoryVisibility = false;
		visibility.showOnHome = false;
		visibility.showAllAbove = false;
		visibility.showAllBelow = false;
		visibility.showRelAbove = 1;
		visibility.showRelBelow = 0;
	} else if (visibilityString == OneStoryDownValueName) {
		isAutoOnStoryVisibility = false;
		visibility.showOnHome = false;
		visibility.showAllAbove = false;
		visibility.showAllBelow = false;
		visibility.showRelAbove = 0;
		visibility.showRelBelow = 1;
	} else if (visibilityString == AllStoriesValueName) {
		isAutoOnStoryVisibility = false;
		visibility.showOnHome = true;
		visibility.showAllAbove = true;
		visibility.showAllBelow = true;
	} else if (visibilityString == HomeStoryOnlyValueName) {
		isAutoOnStoryVisibility = false;
		visibility.showOnHome = true;
		visibility.showAllAbove = false;
		visibility.showAllBelow = false;
		visibility.showRelAbove = 0;
		visibility.showRelBelow = 0;
	}

	return NoError;
}


GSErrCode CreateVisibility (const GS::ObjectState& os,
	const char* fieldName,
	bool& isAutoOnStoryVisibility,
	API_StoryVisibility& visibility)
{
	if (os.Contains (ShowOnStories)) {
		GS::UniString visibilityString;
		os.Get (ShowOnStories, visibilityString);

		if (visibilityString != CustomStoriesValueName) {
			Utility::CreatePredefinedVisibility (visibilityString, isAutoOnStoryVisibility, visibility);
		} else {
			GS::ObjectState customVisibilityOs;
			os.Get (fieldName, customVisibilityOs);

			customVisibilityOs.Get (ShowOnHome, visibility.showOnHome);
			customVisibilityOs.Get (ShowAllAbove, visibility.showAllAbove);
			customVisibilityOs.Get (ShowAllBelow, visibility.showAllBelow);
			customVisibilityOs.Get (ShowRelAbove, visibility.showRelAbove);
			customVisibilityOs.Get (ShowRelBelow, visibility.showRelBelow);
		}
	}

	return NoError;
}


GSErrCode GetCoverFillTransformation (bool coverFillOrientationComesFrom3D,
	API_CoverFillTransformationTypeID coverFillTransformationType,
	GS::ObjectState& os)
{
	if (coverFillOrientationComesFrom3D) {
		os.Add (CoverFillTransformationType, ThreeDDistortionValueName);
	} else if (coverFillTransformationType == API_CoverFillTransformationType_Global) {
		os.Add (CoverFillTransformationType, LinkToProjectOriginValueName);
	} else if (coverFillTransformationType == API_CoverFillTransformationType_Rotated) {
		os.Add (CoverFillTransformationType, LinkToFillOriginValueName);
	} else if (coverFillTransformationType == API_CoverFillTransformationType_Distorted) {
		os.Add (CoverFillTransformationType, CustomDistortionValueName);
	}

	return NoError;
}


GSErrCode CreateCoverFillTransformation (const GS::ObjectState& os,
	bool& coverFillOrientationComesFrom3D,
	API_CoverFillTransformationTypeID& coverFillTransformationType)
{
	coverFillOrientationComesFrom3D = false;
	coverFillTransformationType = API_CoverFillTransformationType_Global;

	if (os.Contains (CoverFillTransformationType)) {
		GS::UniString coverFillTransformationTypeValueName;
		os.Get (CoverFillTransformationType, coverFillTransformationTypeValueName);

		if (coverFillTransformationTypeValueName == ThreeDDistortionValueName) {
			coverFillOrientationComesFrom3D = true;
		} else if (coverFillTransformationTypeValueName == LinkToProjectOriginValueName) {
			coverFillOrientationComesFrom3D = false;
			coverFillTransformationType = API_CoverFillTransformationType_Global;
		} else if (coverFillTransformationTypeValueName == LinkToFillOriginValueName) {
			coverFillOrientationComesFrom3D = false;
			coverFillTransformationType = API_CoverFillTransformationType_Rotated;
		} else if (coverFillTransformationTypeValueName == CustomDistortionValueName) {
			coverFillOrientationComesFrom3D = false;
			coverFillTransformationType = API_CoverFillTransformationType_Distorted;
		}
	}

	return NoError;
}


GSErrCode GetHatchOrientation (API_HatchOrientationTypeID hatchOrientationType, GS::ObjectState& os)
{
	if (hatchOrientationType == API_HatchGlobal) {
		os.Add (HatchOrientationType, LinkToProjectOriginValueName);
	} else if (hatchOrientationType == API_HatchRotated) {
		os.Add (HatchOrientationType, LinkToFillOriginValueName);
	} else if (hatchOrientationType == API_HatchDistorted) {
		os.Add (HatchOrientationType, CustomDistortionValueName);
	}

	return NoError;
}


GSErrCode CreateHatchOrientation (const GS::ObjectState& os, API_HatchOrientationTypeID& hatchOrientationType)
{
	hatchOrientationType = API_HatchGlobal;

	if (os.Contains (HatchOrientationType)) {
		GS::UniString hatchOrientationTypeValueName;
		os.Get (HatchOrientationType, hatchOrientationTypeValueName);

		if (hatchOrientationTypeValueName == LinkToProjectOriginValueName) {
			hatchOrientationType = API_HatchGlobal;
		} else if (hatchOrientationTypeValueName == LinkToFillOriginValueName) {
			hatchOrientationType = API_HatchRotated;
		} else if (hatchOrientationTypeValueName == CustomDistortionValueName) {
			hatchOrientationType = API_HatchDistorted;
		}
	}

	return NoError;
}


GSErrCode GetTransform (API_Tranmat transform, GS::ObjectState& out)
{
	GS::ObjectState matrixOs;
	for (GSSize idx1 = 0; idx1 < 3; ++idx1) {
		for (GSSize idx2 = 0; idx2 < 4; ++idx2) {
			matrixOs.Add (GS::String::SPrintf ("M%d%d", idx1 + 1, idx2 + 1), transform.tmx[idx1 * 4 + idx2]);
		}
	}

	matrixOs.Add ("M41", 0.0);
	matrixOs.Add ("M42", 0.0);
	matrixOs.Add ("M43", 0.0);
	matrixOs.Add ("M44", 1.0);

	out.Add ("matrix", matrixOs);

	return NoError;
}


GSErrCode CreateTransform (const GS::ObjectState& os, API_Tranmat& transform)
{
	GS::ObjectState matrixOs;
	os.Get ("matrix", matrixOs);

	for (GSSize idx1 = 0; idx1 < 3; ++idx1) {
		for (GSSize idx2 = 0; idx2 < 4; ++idx2) {
			matrixOs.Get (GS::String::SPrintf ("M%d%d", idx1 + 1, idx2 + 1), transform.tmx[idx1 * 4 + idx2]);
		}
	}

	return NoError;
}


GSErrCode ConstructPoly2DDataFromElementMemo (const API_ElementMemo& memo, Geometry::Polygon2DData& polygon2DData)
{
	GSErrCode err = NoError;

	Geometry::InitPolygon2DData (&polygon2DData);

	static_assert (sizeof (API_Coord) == sizeof (Coord), "sizeof (API_Coord) != sizeof (Coord)");
	static_assert (sizeof (API_PolyArc) == sizeof (PolyArcRec), "sizeof (API_PolyArc) != sizeof (PolyArcRec)");

	polygon2DData.nVertices = BMGetHandleSize (reinterpret_cast<GSHandle> (memo.coords)) / sizeof (Coord) - 1;
	polygon2DData.vertices = reinterpret_cast<Coord**> (BMAllocateHandle ((polygon2DData.nVertices + 1) * sizeof (Coord), ALLOCATE_CLEAR, 0));
	if (polygon2DData.vertices != nullptr)
		BNCopyMemory (*polygon2DData.vertices, *memo.coords, (polygon2DData.nVertices + 1) * sizeof (Coord));
	else
		err = APIERR_MEMFULL;

	if (err == NoError && memo.parcs != nullptr) {
		polygon2DData.nArcs = BMGetHandleSize (reinterpret_cast<GSHandle> (memo.parcs)) / sizeof (PolyArcRec);
		if (polygon2DData.nArcs > 0) {
			polygon2DData.arcs = reinterpret_cast<PolyArcRec**> (BMAllocateHandle ((polygon2DData.nArcs + 1) * sizeof (PolyArcRec), ALLOCATE_CLEAR, 0));
			if (polygon2DData.arcs != nullptr)
				BNCopyMemory (*polygon2DData.arcs + 1, *memo.parcs, polygon2DData.nArcs * sizeof (PolyArcRec));
			else
				err = APIERR_MEMFULL;
		}
	}

	if (err == NoError) {
		polygon2DData.nContours = BMGetHandleSize (reinterpret_cast<GSHandle> (memo.pends)) / sizeof (Int32) - 1;
		polygon2DData.contourEnds = reinterpret_cast<UIndex**> (BMAllocateHandle ((polygon2DData.nContours + 1) * sizeof (UIndex), ALLOCATE_CLEAR, 0));
		if (polygon2DData.contourEnds != nullptr)
			BNCopyMemory (*polygon2DData.contourEnds, *memo.pends, (polygon2DData.nContours + 1) * sizeof (UIndex));
		else
			err = APIERR_MEMFULL;
	}

	if (err == NoError) {
		Geometry::GetPolygon2DDataBoundBox (polygon2DData, &polygon2DData.boundBox);
		polygon2DData.status.isBoundBoxValid = true;
	} else {
		Geometry::FreePolygon2DData (&polygon2DData);
	}

	return err;
}


}
