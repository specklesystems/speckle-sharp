#include "GetRoofData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Level.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
using namespace FieldNames;

namespace AddOnCommands
{


GS::String GetRoofData::GetFieldName () const
{
	return Roofs;
}


API_ElemTypeID GetRoofData::GetElemTypeID () const
{
	return API_RoofID;
}


GS::ErrCode GetRoofData::SerializeElementType (const API_Element& element,
	const API_ElementMemo& memo,
	GS::ObjectState& os) const
{
	UNUSED_PARAMETER (memo);

	// quantities
	API_ElementQuantity quantity = {};
	API_Quantities quantities = {};
	API_QuantitiesMask quantityMask;

	ACAPI_ELEMENT_QUANTITY_MASK_CLEAR (quantityMask);
	ACAPI_ELEMENT_QUANTITY_MASK_SET (quantityMask, roof, volume);

	quantities.elements = &quantity;
	GSErrCode err = ACAPI_Element_GetQuantities (element.roof.head.guid, nullptr, &quantities, &quantityMask);
	if (err != NoError)
		return err;

	// memo.coords;
	// quantity.roof.volume;
	// The identifier of the room
	os.Add (ElementBase::ApplicationId, APIGuidToString (element.roof.head.guid));
	// GS::UniString roomName = roof.roomName;
	// GS::UniString roomNum = roof.roomNoStr;
	// os.Add(Room::Name, roomName);
	// os.Add(Room::Number, roomNum);

	// The index of the roof's floor
	API_StoryType story = Utility::GetStory (element.roof.head.floorInd);
	os.Add (ElementBase::Level, Objects::Level (story));

	return NoError;
}


GS::String GetRoofData::GetName () const
{
	return GetRoofDataCommandName
}


}
