#include "GetElementIds.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "FieldNames.hpp"
using namespace FieldNames;


namespace AddOnCommands {


static GS::Array<API_Guid> GetSelectedElementGuids ()
{
	GS::Array<API_Guid>		elementGuids;
	API_SelectionInfo		selectionInfo;
	GS::Array<API_Neig>		selNeigs;

	GSErrCode err = ACAPI_Selection_Get (&selectionInfo, &selNeigs, true);
	if (err == NoError) {
		if (selectionInfo.typeID != API_SelEmpty) {
			for (const API_Neig& neig : selNeigs) {
				if (Utility::IsElement3D (neig.guid)) {
					elementGuids.Push (neig.guid);
				}
			}
		}
	}

	BMKillHandle ((GSHandle*) &selectionInfo.marquee.coords);

	return elementGuids;
}


static GS::Array<API_Guid> GetAllElementGuids ()
{
	GS::Array<API_Guid> elementGuids;
	GS::Array<API_Guid> element3DGuids;
	GSErrCode err = ACAPI_Element_GetElemList (API_ZombieElemID, &elementGuids, APIFilt_OnVisLayer | APIFilt_In3D);
	if (err == NoError) {
		for (const API_Guid& guid : elementGuids) {
			if (Utility::IsElement3D (guid)) {
				element3DGuids.Push (guid);
			}
		}
	}

	return element3DGuids;
}


static GS::Array<API_Guid> GetElementsFilteredByElementTypes (GS::Array<GS::UniString>& elementTypes)
{
	GS::Array<API_Guid> filteredGuids;
	GS::Array<API_Guid> elementGuids;
	GSErrCode err = ACAPI_Element_GetElemList (API_ZombieElemID, &elementGuids, APIFilt_OnVisLayer | APIFilt_In3D);
	if (err == NoError) {
		for (const API_Guid& guid : elementGuids) {
			if (Utility::IsElement3D (guid)) {
				API_Elem_Head elementHead = {};
				elementHead.guid = guid;
				err = ACAPI_Element_GetHeader (&elementHead);
				if (err != NoError)
					continue;

				GS::UniString elementTypeName;
				Utility::GetNonLocalizedElementTypeName (elementHead, elementTypeName);
				if (elementTypes.Contains (elementTypeName))
					filteredGuids.Push (guid);
			}
		}
	}
	return filteredGuids;
}


GS::String GetElementIds::GetName () const
{
	return GetElementIdsCommandName;
}


GS::ObjectState GetElementIds::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::UniString elementFilter;
	parameters.Get (ElementBase::ElementFilter, elementFilter);

	GS::ObjectState retVal;

	GS::Array<API_Guid> elementGuids;
	if (elementFilter == "Selection")
		elementGuids = GetSelectedElementGuids ();
	else if (elementFilter == "All")
		elementGuids = GetAllElementGuids ();
	else if (elementFilter == "ElementType") {
		GS::Array<GS::UniString> elementTypes;
		parameters.Get (ElementBase::FilterBy, elementTypes);
		if(elementTypes.GetSize() > 0)
			elementGuids = GetElementsFilteredByElementTypes (elementTypes);
	}

	const auto& listAdder = retVal.AddList<GS::UniString> (ElementBase::ApplicationIds);
	for (const API_Guid& guid : elementGuids) {
		listAdder (APIGuidToString (guid));
	}

	return retVal;
}


}
