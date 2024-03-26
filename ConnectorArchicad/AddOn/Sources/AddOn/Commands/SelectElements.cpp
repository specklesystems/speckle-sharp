#include "SelectElements.hpp"
#include "APIMigrationHelper.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "FieldNames.hpp"
#include "APIdefs_Automate.h"


GS::String AddOnCommands::SelectElements::GetName () const
{
	return SelectElementsCommandName;
}


GS::ObjectState AddOnCommands::SelectElements::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::ObjectState result{};
	GSErrCode err = NoError;

	GS::Array<GS::UniString> ids;
	parameters.Get (FieldNames::ElementBase::ApplicationIds, ids);

	bool deselect;
	parameters.Get ("deselect", deselect);

	bool clearSelection;
	parameters.Get ("clearSelection", clearSelection);

	GS::Array<API_Neig> selectionBasket = ids.Transform<API_Neig> ([] (const GS::UniString& idStr) { return API_Neig (APIGuidFromString (idStr.ToCStr ())); });

	if (clearSelection) {
		err = ACAPI_Selection_DeselectAll ();
		if (err != NoError)
			return result;
	}

	err = ACAPI_Selection_Select (selectionBasket, !deselect);
	if (err != NoError)
		return result;

	ACAPI_View_ZoomToSelected ();

	result.Add (FieldNames::ApplicationObject::Status, true);
	return result;
}
