#include "CreateWindow.hpp"
#include "CreateOpeningBase.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "DGModule.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
#include "Database.hpp"


namespace AddOnCommands
{
static GSErrCode CreateNewWindow (API_Element& element, API_ElementMemo& memo, API_SubElement& marker)
{
	return ACAPI_Element_CreateExt (&element, &memo, 1UL, &marker);
}

static GSErrCode ModifyExistingWindow (API_Element& Window, API_Element& mask)
{
	return ACAPI_Element_ChangeExt (&Window, &mask, nullptr, 0, 0, nullptr, true, 0);
}

static GSErrCode GetWindowFromObjectState (const GS::ObjectState& currentWindow, API_Element& element, API_Element& wallMask, API_ElementMemo& memo, API_SubElement& marker)
{
	GSErrCode err = NoError;

#ifdef ServerMainVers_2600
	element.header.type = API_WindowID;
#else
	element.header.typeID = API_WindowID;
#endif
	marker.subType = APISubElement_MainMarker;

	err = ACAPI_Element_GetDefaultsExt (&element, &memo, 1UL, &marker);
	if (err != NoError) {
		ACAPI_DisposeElemMemoHdls (&memo);
		ACAPI_DisposeElemMemoHdls (&marker.memo);
		return err;
	}

#pragma region ReviewRequired
	API_LibPart libPart;
	BNZeroMemory (&libPart, sizeof (API_LibPart));

#ifdef ServerMainVers_2600
	err = ACAPI_Goodies_GetMarkerParent (element.header.type, libPart);
#else
	err = ACAPI_Goodies (APIAny_GetMarkerParentID, &element.header.typeID, &libPart);
#endif
	if (err != NoError) {
		ACAPI_DisposeElemMemoHdls (&memo);
		ACAPI_DisposeElemMemoHdls (&marker.memo);
		return err;
	}

	err = ACAPI_LibPart_Search (&libPart, false, true);
	if (err != NoError) {
		ACAPI_DisposeElemMemoHdls (&memo);
		ACAPI_DisposeElemMemoHdls (&marker.memo);
		return err;
	}

	delete libPart.location;

	double a = .0, b = .0;
	Int32 addParNum = 0;
	API_AddParType** markAddPars;
	err = ACAPI_LibPart_GetParams (libPart.index, &a, &b, &addParNum, &markAddPars);
	if (err != NoError) {
		ACAPI_DisposeElemMemoHdls (&memo);
		ACAPI_DisposeElemMemoHdls (&marker.memo);
		return err;
	}

	marker.memo.params = markAddPars;
#pragma endregion

	GS::UniString guidString;
	currentWindow.Get (ApplicationIdFieldName, guidString);
	element.header.guid = APIGuidFromString (guidString.ToCStr ());

	err = GetOpeningBaseFromObjectState<API_WindowType> (currentWindow, element.window, wallMask);

	return err;
}

GS::String CreateWindow::GetName () const
{
	return CreateWindowCommandName;
}

GS::ObjectState CreateWindow::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::ObjectState result;
	GSErrCode err = NoError;

	GS::Array<GS::ObjectState> subElements;
	parameters.Get (SubElementsFieldName, subElements);

	const auto& listAdder = result.AddList<GS::UniString> (ApplicationIdsFieldName);

	ACAPI_CallUndoableCommand ("CreateSpeckleWindow", [&] () -> GSErrCode {
		Utility::Database db;
		db.SwitchToFloorPlan ();

		for (const GS::ObjectState& subElement : subElements) {
			API_Element element = {};

			// Check if parent exist
			GS::UniString parentGuidString;
			subElement.Get (ParentElementIdFieldName, parentGuidString);
			API_Guid parentGuid = APIGuidFromString (parentGuidString.ToCStr ());

#ifdef ServerMainVers_2600
			element.header.type = API_WallID;
#else
			element.header.typeID = API_WallID;
#endif
			element.header.guid = parentGuid;
			err = ACAPI_Element_Get (&element);
			if (err != NoError) {
				continue;
			}

			if (element.wall.type == APIWtyp_Poly) {
				continue;
			}

			// This may be redundant, should test it
			bool parentExists = Utility::ElementExists (parentGuid);
			bool isParentWall = Utility::GetElementType (parentGuid) == API_WallID;
			if (!(parentExists && isParentWall)) {
				continue;
			}

			// Subelement
			API_ElementMemo memo = {};
			API_SubElement marker = {};
			API_Element elementMask = {};

			// Try to get subelement
			err = GetWindowFromObjectState (subElement, element, elementMask, memo, marker);
			if (err != NoError) {
				ACAPI_DisposeElemMemoHdls (&memo);
				ACAPI_DisposeElemMemoHdls (&marker.memo);
				continue;
			}

			// Set its parent
			element.window.owner = parentGuid;

			// Update or create
			bool WindowExists = Utility::ElementExists (element.header.guid);
			if (WindowExists) {
				err = ModifyExistingWindow (element, elementMask);
			} else {
				err = CreateNewWindow (element, memo, marker);
			}

			if (err == NoError) {
				GS::UniString elemId = APIGuidToString (element.header.guid);
				listAdder (elemId);
			}

			err = ACAPI_DisposeElemMemoHdls (&memo);
			err = ACAPI_DisposeElemMemoHdls (&marker.memo);
		}

		return NoError;
	  });

	return result;
}
}
