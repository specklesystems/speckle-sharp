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


GSErrCode CreateWindow::GetElementFromObjectState (const GS::ObjectState& os,
	API_Element& element,
	API_Element& elementMask,
	API_ElementMemo& memo,
	GS::UInt64& /*memoMask*/,
	API_SubElement** marker,
	AttributeManager& /*attributeManager*/,
	LibpartImportManager& /*libpartImportManager*/,
	GS::Array<GS::UniString>& log) const
{
	GSErrCode err = NoError;

	Utility::SetElementType (element.header, API_WindowID);

	*marker = new API_SubElement ();
	BNZeroMemory (*marker, sizeof (API_SubElement));
	err = Utility::GetBaseElementData (element, &memo, marker, log);
	if (err != NoError)
		return err;

	if (!CheckEnvironment (os, element))
		return Error;

	GetDoorWindowFromObjectState<API_WindowType> (os, element.window, elementMask, log);

	err = GetOpeningBaseFromObjectState<API_WindowType> (os, element.window, elementMask, log);

	return err;
}


GS::String CreateWindow::GetName () const
{
	return CreateWindowCommandName;
}


}
