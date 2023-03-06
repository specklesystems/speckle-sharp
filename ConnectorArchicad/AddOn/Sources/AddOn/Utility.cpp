#include "Utility.hpp"
#include "ObjectState.hpp"
#include "RealNumber.h"
#include "ObjectState.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
using namespace FieldNames;

namespace Utility {


API_ElemTypeID GetElementType (const API_Guid& guid)
{
	API_Elem_Head elemHead = {};
	elemHead.guid = guid;

	GSErrCode error = ACAPI_Element_GetHeader (&elemHead);
	if (error == NoError)
#ifdef ServerMainVers_2600
		return elemHead.type.typeID;
#else
		return elemHead.typeID;
#endif

	return API_ZombieElemID;
}


bool ElementExists (const API_Guid& guid)
{
	return (GetElementType (guid) != API_ZombieElemID);
}


GSErrCode GetBaseElementData (API_Element& element, API_ElementMemo* memo)
{
	GSErrCode err;
	API_Guid guid = element.header.guid;

	bool elemExists = ElementExists (guid);
	if (elemExists) {
		err = ACAPI_Element_Get (&element);
		if (err == NoError && memo != nullptr) {
			err = ACAPI_Element_GetMemo (guid, memo);
		}
	} else {
		err = ACAPI_Element_GetDefaults (&element, memo);
		element.header.guid = guid;	// keep guid for creation
	}

	return err;
}


bool IsElement3D (const API_Guid& guid)
{
	switch (GetElementType (guid)) {
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
	GSErrCode err = ACAPI_Environment (APIEnv_GetStorySettingsID, &storyInfo, nullptr);
	if (err != NoError) {
		return stories;
	}

	short idx = 0;
	for (short i = storyInfo.firstStory; i <= storyInfo.lastStory; i++)
		stories.Push ((*storyInfo.data)[idx++]);

	BMKillHandle ((GSHandle*) &storyInfo.data);

	return stories;
}


double GetStoryLevel (short floorNumber)
{
	const GS::Array<API_StoryType> stories = GetStoryItems ();

	for (const auto& story : stories) {
		if (story.index == floorNumber) {
			return story.level;
		}
	}

	return 0.0;
}


void SetStoryLevel (const double& inLevel, const short& floorInd, double& level)
{
	const GS::Array<API_StoryType> stories = GetStoryItems ();
	level = inLevel;
	for (const auto& story : stories) {
		if (story.index == floorInd) {
			level = level - story.level;
			break;
		}
	}
}


static API_StoryType GetActualStoryItem ()
{
	API_StoryInfo storyInfo{};
	GSErrCode err = ACAPI_Environment (APIEnv_GetStorySettingsID, &storyInfo, nullptr);
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
	GSErrCode err = ACAPI_Database (APIDb_GetCurrentWindowID, &windowInfo, nullptr);
	if (err == NoError && (windowInfo.typeID != APIWind_FloorPlanID && windowInfo.typeID != APIWind_3DModelID))
		floorInd += GetActualStoryItem ().index;
}

GS::Array<API_Guid> GetWallSubelements (API_WallType& wall)
{
	GS::Array<API_Guid> result;

	if (wall.hasDoor) {
		GS::Array<API_Guid> doors;
		GSErrCode err = ACAPI_Element_GetConnectedElements (wall.head.guid, API_DoorID, &doors);
		if (err == NoError)
			result.Append (doors);
	}
	if (wall.hasWindow) {
		GS::Array<API_Guid> windows;
		GSErrCode err = ACAPI_Element_GetConnectedElements (wall.head.guid, API_WindowID, &windows);
		if (err == NoError)
			result.Append (windows);
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
		DBASSERT (segment.modelElemStructureType != API_CompositeStructure)
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


GSErrCode GetOneSchemeData (const API_AssemblySegmentSchemeData& schemeData, GS::ObjectState& out)
{
	out.Add (AssemblySegmentSchemeData::lengthType, segmentLengthTypeNames.Get (schemeData.lengthType));
	out.Add (AssemblySegmentSchemeData::fixedLength, schemeData.fixedLength);
	out.Add (AssemblySegmentSchemeData::lengthProportion, schemeData.lengthProportion);

	return NoError;
}


GSErrCode GetAllSchemeData (API_AssemblySegmentSchemeData* schemeData, GS::ObjectState& out)
{
	if (schemeData == nullptr) return Error;

	GSSize schemesCount = BMGetPtrSize (reinterpret_cast<GSPtr>(schemeData)) / sizeof (API_AssemblySegmentSchemeData);
	DBASSERT (schemesCount == elem.column.nSchemes)

		for (GSSize idx = 0; idx < schemesCount; ++idx) {
			GS::ObjectState currentScheme;
			Utility::GetOneSchemeData (schemeData[idx], currentScheme);
			out.Add (GS::String::SPrintf (AssemblySegment::SchemeName, idx + 1), currentScheme);
		}

	return NoError;
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
	DBASSERT (cutsCount == elem.column.nCuts)

		for (GSSize idx = 0; idx < cutsCount; ++idx) {
			GS::ObjectState currentCut;
			Utility::GetOneCutData (cutData[idx], currentCut);
			out.Add (GS::String::SPrintf (AssemblySegment::CutName, idx + 1), currentCut);
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

#ifdef ServerMainVers_2600
		switch (element.header.type.typeID) {
#else
		switch (element.header.typeID) {
#endif
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


GSErrCode CreateAllSchemeData (const GS::ObjectState& os, GS::UInt32& numberOfCuts, API_Element& element, API_Element& mask, API_ElementMemo* memo)
{
	GSErrCode err = NoError;
	API_AssemblySegmentSchemeData defaultSegmentScheme;
	if (memo->assemblySegmentSchemes != nullptr) {
		defaultSegmentScheme = memo->assemblySegmentSchemes[0];
#ifdef ServerMainVers_2600
		switch (element.header.type.typeID) {
#else
		switch (element.header.typeID) {
#endif
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


GSErrCode GetVisibility (bool isAutoOnStoryVisibility, API_StoryVisibility visibility, GS::UniString& visibilityString)
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


GSErrCode ExportVisibility (bool isAutoOnStoryVisibility, API_StoryVisibility visibility, GS::ObjectState& os, const char* fieldName, bool exportVisibilityValues /*= false*/)
{
	GS::UniString visibilityString;
	if (NoError != GetVisibility (isAutoOnStoryVisibility, visibility, visibilityString))
		return Error;

	if (!exportVisibilityValues) {
		os.Add (fieldName, visibilityString);
	}
	
	if (visibilityString == CustomStoriesValueName || exportVisibilityValues) {
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


GSErrCode SetVisibility (const GS::UniString& visibilityString, bool& isAutoOnStoryVisibility, API_StoryVisibility& visibility)
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


GSErrCode ImportVisibility (const GS::ObjectState& os, const char* fieldName, bool& isAutoOnStoryVisibility, API_StoryVisibility& visibility)
{
	if (os.Contains (ShowOnStories)) {
		GS::UniString visibilityString;
		os.Get (ShowOnStories, visibilityString);

		if (visibilityString != CustomStoriesValueName) {
			Utility::SetVisibility (visibilityString, isAutoOnStoryVisibility, visibility);
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


GSErrCode ExportCoverFillTransformation (bool coverFillOrientationComesFrom3D, API_CoverFillTransformationTypeID coverFillTransformationType, GS::ObjectState& os)
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


GSErrCode ImportCoverFillTransformation (const GS::ObjectState& os, bool& coverFillOrientationComesFrom3D, API_CoverFillTransformationTypeID& coverFillTransformationType)
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


GSErrCode ExportHatchOrientation (API_HatchOrientationTypeID hatchOrientationType, GS::ObjectState& os)
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


GSErrCode ImportHatchOrientation (const GS::ObjectState& os, API_HatchOrientationTypeID& hatchOrientationType)
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

}
