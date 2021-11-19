#include "Utility.hpp"


namespace Utility {


bool IsElement3D (const API_Guid& guid)
{
	API_Elem_Head elemHead = {};
	elemHead.guid = guid;

	GSErrCode error = ACAPI_Element_GetHeader (&elemHead);
	if (error == NoError) {
		API_ElemTypeID typeId = elemHead.typeID;

		switch (typeId) 
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
			case API_CurtainWallID : 
			case API_ShellID : 
			case API_SkylightID :
			case API_MorphID : 
			case API_StairID : 
			case API_RailingID : 
				return true;
			default: return false;
		}
	}

	return false;
}


}