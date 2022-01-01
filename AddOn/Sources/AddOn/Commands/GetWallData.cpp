#include "GetWallData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"


namespace AddOnCommands {


static GS::ObjectState SerializeWallType (const API_WallType& wall)
{
	GS::ObjectState os;

	os.Add (ElementIdFieldName, APIGuidToString (wall.head.guid));
	os.Add (FloorIndexFieldName, wall.head.floorInd);

	double z = Utility::GetStoryLevel (wall.head.floorInd) + wall.bottomOffset;
	os.Add (Wall::StartPointFieldName, Objects::Point3D (wall.begC.x, wall.begC.y, z));
	os.Add (Wall::EndPointFieldName, Objects::Point3D (wall.endC.x, wall.endC.y, z));

	if (abs (wall.angle) > EPS)
		os.Add (Wall::ArcAngleFieldName, wall.angle);

	os.Add (Wall::HeightFieldName, wall.height);

	os.Add (Wall::StructureFieldName, structureTypeNames.Get (wall.modelElemStructureType));

	os.Add (Wall::GeometryMethodFieldName, wallTypeNames.Get (wall.type));

	os.Add (Wall::WallComplexityFieldName, profileTypeNames.Get (wall.profileType));

	if (wall.type == APIWtyp_Trapez) {
		os.Add (Wall::FirstThicknessFieldName, wall.thickness);
		os.Add (Wall::SecondThicknessFieldName, wall.thickness1);
	} else {
		os.Add (Wall::ThicknessFieldName, wall.thickness);
	}

	os.Add (Wall::OutsideSlantAngleFieldName, wall.slantAlpha);
	if (wall.profileType == APISect_Trapez)
		os.Add (Wall::InsideSlantAngleFieldName, wall.slantBeta);

	return os;
}


GS::String GetWallData::GetNamespace () const
{
	return CommandNamespace;
}


GS::String GetWallData::GetName () const
{
	return GetWallDataCommandName;
}
	

GS::ObjectState GetWallData::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::Array<GS::UniString> ids;
	parameters.Get (ElementIdsFieldName, ids);
	GS::Array<API_Guid>	elementGuids = ids.Transform<API_Guid> ([] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); });

	GS::ObjectState result;
	const auto& listAdder = result.AddList<GS::ObjectState> (WallsFieldName);
	for (const API_Guid& guid : elementGuids) {
		API_Element element {};
		element.header.guid = guid;

		GSErrCode err = ACAPI_Element_Get (&element);
		if (err != NoError) {
			continue;
		}

		if (element.header.typeID != API_WallID) {
			continue;
		}

		listAdder (SerializeWallType (element.wall));
	}

	return result;
}


}