#ifndef GET_OPENINGBASE_DATA_HPP
#define GET_OPENINGBASE_DATA_HPP

// API
#include "APIEnvir.h"
#include "ACAPinc.h"

#include "Objects/Point.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"

// GSRoot
#include "ObjectState.hpp"

using namespace FieldNames;


namespace AddOnCommands {


template<typename T>
GSErrCode GetOpeningBaseData (const T& element, GS::ObjectState& os)
{
	GSErrCode err = NoError;

	os.Add (OpeningBase::width, element.openingBase.width);
	os.Add (OpeningBase::height, element.openingBase.height);
	os.Add (OpeningBase::subFloorThickness, element.openingBase.subFloorThickness);
	os.Add (OpeningBase::reflected, element.openingBase.reflected);
	os.Add (OpeningBase::oSide, element.openingBase.oSide);
	os.Add (OpeningBase::refSide, element.openingBase.refSide);

	{
		API_Attribute attrib;
		BNZeroMemory (&attrib, sizeof (API_Attribute));
		attrib.header.typeID = API_BuildingMaterialID;
		attrib.header.index = element.openingBase.mat;
		ACAPI_Attribute_Get (&attrib);
		os.Add (OpeningBase::buildingMaterial, GS::UniString{attrib.header.name});
	}

	{
		API_LibPart libPart;
		BNZeroMemory (&libPart, sizeof (API_LibPart));
		libPart.index = element.openingBase.libInd;

		err = ACAPI_LibPart_Get (&libPart);
		if (err == NoError)
			os.Add (OpeningBase::libraryPart, GS::UniString (libPart.docu_UName));
	}

	os.Add (OpeningBase::revealDepthFromSide, element.revealDepthFromSide);
	os.Add (OpeningBase::jambDepthHead, element.jambDepthHead);
	os.Add (OpeningBase::jambDepth, element.jambDepth);
	os.Add (OpeningBase::jambDepth2, element.jambDepth2);
	os.Add (OpeningBase::objLoc, element.objLoc);
	os.Add (OpeningBase::lower, element.lower);
	os.Add (OpeningBase::directionType, windowDoorDirectionTypeNames.Get (element.directionType));

	os.Add (OpeningBase::startPoint, Objects::Point3D (element.startPoint.x, element.startPoint.y, 0));
	os.Add (OpeningBase::dirVector, Objects::Point3D (element.dirVector.x, element.dirVector.y, 0));

	return err;
}


} // namespace AddOnCommands


#endif // GET_OPENINGBASE_DATA_HPP
