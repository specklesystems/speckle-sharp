#include "GetSubElementInfo.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"


namespace AddOnCommands {

GS::String GetSubElementInfo::GetName () const
{
	return GetSubelementInfoCommandName;
}

GS::ObjectState GetSubElementInfo::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GSErrCode err = NoError;
	GS::UniString id;
	parameters.Get (ApplicationIdFieldName, id);
	API_Guid wallGuid = APIGuidFromString (id.ToCStr ());

	GS::ObjectState result;
	const auto& listAdder = result.AddList<GS::ObjectState> (SubelementModelsFieldName);

	API_Element wallElement{};
	wallElement.header.guid = wallGuid;
	err = ACAPI_Element_Get (&wallElement);

	if (err == NoError) {
		GS::Array<API_Guid> elementGuids = Utility::GetWallSubelements (wallElement.wall);
		for (GS::UInt32 i = 0; i < elementGuids.GetSize (); i++) {
			API_Guid currentGuid = elementGuids.Get (i);

			GS::UniString guid = APIGuidToString (currentGuid);
			API_ElemTypeID elementTypeId = Utility::GetElementType (currentGuid);
			GS::UniString elemType = elementNames.Get (elementTypeId);

			GS::ObjectState subelementModel;
			subelementModel.Add (ApplicationIdFieldName, guid);
			subelementModel.Add (ElementTypeFieldName, elemType);
			listAdder (subelementModel);
		}
	}

	return result;
}

}