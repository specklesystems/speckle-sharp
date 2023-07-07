#include "GetSkylightData.hpp"
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


GS::String GetSkylightData::GetFieldName () const
{
	return Skylights;
}


API_ElemTypeID GetSkylightData::GetElemTypeID () const
{
	return API_SkylightID;
}


GS::ErrCode	GetSkylightData::SerializeElementType (const API_Element& element,
  const API_ElementMemo& memo,
  GS::ObjectState& os) const
{
	GS::ErrCode err = NoError;
	err = GetDataCommand::SerializeElementType (element, memo, os);
	if (NoError != err)
		return err;

	os.Add (ElementBase::ParentElementId, APIGuidToString (element.skylight.owner));

	os.Add (Skylight::VertexID, element.skylight.vertexID);
	os.Add (Skylight::SkylightFixMode, skylightFixModeNames.Get (element.skylight.fixMode));
	os.Add (Skylight::SkylightAnchor, skylightAnchorNames.Get (element.skylight.anchorPoint));
	os.Add (Skylight::AnchorPosition, Objects::Point3D (element.skylight.anchorPosition.x, element.skylight.anchorPosition.y, 0));
	os.Add (Skylight::AnchorLevel, element.skylight.anchorLevel);
	os.Add (Skylight::AzimuthAngle, element.skylight.azimuthAngle);
	os.Add (Skylight::ElevationAngle, element.skylight.elevationAngle);

	GetOpeningBaseData<API_SkylightType> (element.skylight, os);

	return NoError;
}


GS::String GetSkylightData::GetName () const
{
	return GetSkylightCommandName;
}


} // namespace AddOnCommands
