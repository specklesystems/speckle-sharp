#include "GetWindowData.hpp"
#include "GetOpeningBaseData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
using namespace FieldNames;


namespace AddOnCommands {


GS::String GetWindowData::GetFieldName () const
{
	return Windows;
}


API_ElemTypeID GetWindowData::GetElemTypeID () const
{
	return API_WindowID;
}


GS::ErrCode	GetWindowData::SerializeElementType (const API_Element& element,
	const API_ElementMemo& /*memo*/,
	GS::ObjectState& os) const
{
	os.Add (ElementBase::ParentElementId, APIGuidToString (element.window.owner));

	AddOnCommands::GetDoorWindowData<API_WindowType> (element.window, os);

	AddOnCommands::GetOpeningBaseData<API_WindowType> (element.window, os);

	return NoError;
}


GS::String GetWindowData::GetName () const
{
	return GetWindowCommandName;
}


}
