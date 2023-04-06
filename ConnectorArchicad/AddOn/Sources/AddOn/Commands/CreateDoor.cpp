#include "CreateDoor.hpp"
#include "CreateOpeningBase.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "DGModule.hpp"
#include "LibpartImportManager.hpp"
#include "APIHelper.hpp"
#include "FieldNames.hpp"
#include "OnExit.hpp"
#include "ExchangeManager.hpp"
#include "TypeNameTables.hpp"
#include "Database.hpp"
#include "BM.hpp"


using namespace FieldNames;


namespace AddOnCommands
{


GS::UniString CreateDoor::GetUndoableCommandName () const
{
	return "CreateSpeckleDoor";
}


GSErrCode CreateDoor::GetElementFromObjectState (const GS::ObjectState& currentDoor,
	API_Element& element,
	API_Element& elementMask,
	API_ElementMemo& memo,
	GS::UInt64& /*memoMask*/,
	AttributeManager& /*attributeManager*/,
	LibpartImportManager& /*libpartImportManager*/,
	API_SubElement** marker /*= nullptr*/) const
{
	GSErrCode err = NoError;

#ifdef ServerMainVers_2600
	element.header.type = API_DoorID;
#else
	element.header.typeID = API_DoorID;
#endif

	* marker = new API_SubElement ();
	BNZeroMemory (*marker, sizeof (API_SubElement));
	err = Utility::GetBaseElementData (element, &memo, marker);
	if (err != NoError)
		return err;

	if (!CheckEnvironment (currentDoor, element))
		return Error;

	err = GetOpeningBaseFromObjectState<API_DoorType> (currentDoor, element.door, elementMask);

	return err;
}


GS::String CreateDoor::GetName () const
{
	return CreateDoorCommandName;
}


} // namespace AddOnCommands
