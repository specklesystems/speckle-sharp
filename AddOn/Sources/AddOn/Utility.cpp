#include "Utility.hpp"


namespace Utility {


API_ElemTypeID GetElementType (const API_Guid& guid)
{
	API_Elem_Head elemHead = {};
	elemHead.guid = guid;

	GSErrCode error = ACAPI_Element_GetHeader(&elemHead);
	if (error == NoError)
		return elemHead.typeID;
	
	return API_ZombieElemID;
}


bool IsElement3D (const API_Guid& guid)
{
	switch (GetElementType (guid))
	{
		case API_WallID :
		case API_ColumnID :
		case API_BeamID :
		case API_WindowID :
		case API_DoorID : 
		case API_ObjectID : 
		case API_LampID : 
		case API_SlabID : 
		case API_RoofID : 
		case API_MeshID : 
		case API_ZoneID :
		case API_CurtainWallID : 
		case API_ShellID : 
		case API_SkylightID :
		case API_MorphID : 
		case API_StairID : 
		case API_RailingID : 
		case API_OpeningID :
			return true;
		default: return false;
	}
}


GS::Array<API_StoryType> GetStoryItems ()
{
	GS::Array<API_StoryType> stories;

	API_StoryInfo storyInfo {};
	GSErrCode err = ACAPI_Environment (APIEnv_GetStorySettingsID, &storyInfo, nullptr);
	if (err != NoError) {
		return stories;
	}

	short idx = 0;
	for (short i = storyInfo.firstStory; i <= storyInfo.lastStory; i++)
		stories.Push ((*storyInfo.data)[idx++]);

	BMKillHandle ((GSHandle*)&storyInfo.data);

	return stories;
}


double GetStoryLevel (short floorNumber)
{
	const GS::Array<API_StoryType> stories = GetStoryItems ();

	for (const auto& story : stories) {
		if (story.floorId == floorNumber) {
			return story.level;
		}
	}

	return 0.0;
}


}