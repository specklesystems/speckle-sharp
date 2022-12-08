#include "Utility.hpp"
#include "RealNumber.h"


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
}