#ifndef CREATE_OPENINGBASE_HPP
#define CREATE_OPENINGBASE_HPP

// API
#include "APIEnvir.h"
#include "ACAPinc.h"

#include "Objects/Point.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"

// GSRoot
#include "ObjectState.hpp"


namespace AddOnCommands {


template<typename T>
GSErrCode GetOpeningBaseFromObjectState (const GS::ObjectState& os, T& element, API_Element& mask)
{
	GSErrCode err = NoError;

	os.Get (OpeningBase::width, element.openingBase.width);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.width);

	os.Get (OpeningBase::height, element.openingBase.height);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.height);

	os.Get (OpeningBase::subFloorThickness, element.openingBase.subFloorThickness);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.subFloorThickness);

	os.Get (OpeningBase::reflected, element.openingBase.reflected);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.reflected);

	os.Get (OpeningBase::oSide, element.openingBase.oSide);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.oSide);

	os.Get (OpeningBase::refSide, element.openingBase.refSide);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.refSide);

	if (os.Contains (OpeningBase::buildingMaterial)) {
		GS::UniString attrName;
		os.Get (OpeningBase::buildingMaterial, attrName);

		if (!attrName.IsEmpty ()) {
			API_Attribute attrib;
			BNZeroMemory (&attrib, sizeof (API_Attribute));
			attrib.header.typeID = API_BuildingMaterialID;
			CHCopyC (attrName.ToCStr (), attrib.header.name);
			err = ACAPI_Attribute_Get (&attrib);

			if (err == NoError)
				element.openingBase.mat = attrib.header.index;
		}
	}

	if (os.Contains (OpeningBase::libraryPart)) {
		GS::UniString libPartName;
		os.Get (OpeningBase::libraryPart, libPartName);

		if (!libPartName.IsEmpty ()) {
			API_LibPart libPart;
			BNZeroMemory (&libPart, sizeof (API_LibPart));
			GS::ucscpy (libPart.docu_UName, libPartName.ToUStr ());

			err = ACAPI_LibPart_Search (&libPart, false, true);
			if (err == NoError)
				element.openingBase.libInd = libPart.index;
		}
	}

	os.Get (OpeningBase::revealDepthFromSide, element.revealDepthFromSide);
	ACAPI_ELEMENT_MASK_SET (mask, T, revealDepthFromSide);

	os.Get (OpeningBase::jambDepthHead, element.jambDepthHead);
	ACAPI_ELEMENT_MASK_SET (mask, T, jambDepthHead);

	os.Get (OpeningBase::jambDepth, element.jambDepth);
	ACAPI_ELEMENT_MASK_SET (mask, T, jambDepth);

	os.Get (OpeningBase::jambDepth2, element.jambDepth2);
	ACAPI_ELEMENT_MASK_SET (mask, T, jambDepth2);

	os.Get (OpeningBase::objLoc, element.objLoc);
	ACAPI_ELEMENT_MASK_SET (mask, T, objLoc);

	os.Get (OpeningBase::lower, element.lower);
	ACAPI_ELEMENT_MASK_SET (mask, T, lower);

	if (os.Contains (OpeningBase::directionType)) {
		API_WindowDoorDirectionTypes realDirectionTypeName = API_WDAssociativeToWall;
		GS::UniString directionTypeName;
		os.Get (OpeningBase::directionType, directionTypeName);

		GS::Optional<API_WindowDoorDirectionTypes> tmpDirectionTypeName = windowDoorDirectionTypeNames.FindValue (directionTypeName);
		if (tmpDirectionTypeName.HasValue ())
			realDirectionTypeName = tmpDirectionTypeName.Get ();
		element.directionType = realDirectionTypeName;
		ACAPI_ELEMENT_MASK_SET (mask, T, directionType);
	}

	Objects::Point3D startPoint;
	if (os.Contains (OpeningBase::startPoint))
		os.Get (OpeningBase::startPoint, startPoint);
	element.startPoint = startPoint.ToAPI_Coord ();
	ACAPI_ELEMENT_MASK_SET (mask, T, startPoint);

	Objects::Point3D dirVector;
	if (os.Contains (OpeningBase::dirVector))
		os.Get (OpeningBase::dirVector, dirVector);
	element.dirVector = dirVector.ToAPI_Coord ();
	ACAPI_ELEMENT_MASK_SET (mask, T, dirVector);

	return err;
}



} // namespace AddOnCommands


#endif // CREATE_OPENINGBASE_HPP
