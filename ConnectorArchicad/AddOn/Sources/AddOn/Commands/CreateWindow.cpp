#include "CreateWindow.hpp"
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


GS::UniString CreateWindow::GetUndoableCommandName () const
{
	return "CreateSpeckleWindow";
}


GSErrCode CreateWindow::GetElementFromObjectState (const GS::ObjectState& currentWindow,
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
	element.header.type = API_WindowID;
#else
	element.header.typeID = API_WindowID;
#endif

	*marker = new API_SubElement ();
	BNZeroMemory (*marker, sizeof (API_SubElement));
	err = Utility::GetBaseElementData (element, &memo, marker);
	if (err != NoError)
		return err;

	if (!CheckEnvironment (currentWindow, element))
		return Error;

	err = GetOpeningBaseFromObjectState<API_WindowType> (currentWindow, element.window, elementMask);

	return err;
}


GS::String CreateWindow::GetName () const
{
	return CreateWindowCommandName;
}


}
