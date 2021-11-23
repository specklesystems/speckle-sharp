#include "Utility.hpp"


namespace Utility {

static const GS::HashTable<API_ElemTypeID, GS::UniString> elementNames
{
	{ API_ZombieElemID,					"InvalidType"},
	{ API_WallID,						"Wall"},
	{ API_ColumnID,						"Column"},
	{ API_BeamID,						"Beam"},
	{ API_WindowID,						"Window"},
	{ API_DoorID,						"Door"},
	{ API_ObjectID,						"Object"},
	{ API_LampID,						"Lamp"},
	{ API_SlabID,						"Slab"},
	{ API_RoofID,						"Roof"},
	{ API_MeshID,						"Mesh"},
	{ API_ZoneID,						"Zone"},
	{ API_CurtainWallID,				"CurtainWall"},
	{ API_ShellID,						"Shell"},
	{ API_SkylightID,					"Skylight"},
	{ API_MorphID,						"Morph"},
	{ API_StairID,						"Stair"},
	{ API_RailingID,					"Railing"},
	{ API_OpeningID,					"Opening"}
};

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


}