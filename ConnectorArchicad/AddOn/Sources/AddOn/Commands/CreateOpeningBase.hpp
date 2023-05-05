#ifndef CREATE_OPENING_BASE
#define CREATE_OPENING_BASE

#include "CreateCommand.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
#include "Objects/Point.hpp"

namespace AddOnCommands {

template<typename T>
GSErrCode GetOpeningBaseFromObjectState (const GS::ObjectState& os, T& element, API_Element& mask)
{
	GSErrCode err = NoError;

	os.Get (FieldNames::OpeningBase::width, element.openingBase.width);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.width);

	os.Get (FieldNames::OpeningBase::height, element.openingBase.height);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.height);

	os.Get (FieldNames::OpeningBase::subFloorThickness, element.openingBase.subFloorThickness);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.subFloorThickness);

	os.Get (FieldNames::OpeningBase::reflected, element.openingBase.reflected);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.reflected);

	os.Get (FieldNames::OpeningBase::oSide, element.openingBase.oSide);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.oSide);

	os.Get (FieldNames::OpeningBase::refSide, element.openingBase.refSide);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.refSide);

	if (os.Contains (FieldNames::OpeningBase::buildingMaterial)) {
		GS::UniString attrName;
		os.Get (FieldNames::OpeningBase::buildingMaterial, attrName);

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

	if (os.Contains (FieldNames::OpeningBase::libraryPart)) {
		GS::UniString libPartName;
		os.Get (FieldNames::OpeningBase::libraryPart, libPartName);

		if (!libPartName.IsEmpty ()) {
			API_LibPart libPart;
			BNZeroMemory (&libPart, sizeof (API_LibPart));
			GS::ucscpy (libPart.docu_UName, libPartName.ToUStr ());

			err = ACAPI_LibPart_Search (&libPart, false, true);
			if (err == NoError)
				element.openingBase.libInd = libPart.index;
		}
	}

	os.Get (FieldNames::OpeningBase::revealDepthFromSide, element.revealDepthFromSide);
	ACAPI_ELEMENT_MASK_SET (mask, T, revealDepthFromSide);

	os.Get (FieldNames::OpeningBase::jambDepthHead, element.jambDepthHead);
	ACAPI_ELEMENT_MASK_SET (mask, T, jambDepthHead);

	os.Get (FieldNames::OpeningBase::jambDepth, element.jambDepth);
	ACAPI_ELEMENT_MASK_SET (mask, T, jambDepth);

	os.Get (FieldNames::OpeningBase::jambDepth2, element.jambDepth2);
	ACAPI_ELEMENT_MASK_SET (mask, T, jambDepth2);

	os.Get (FieldNames::OpeningBase::objLoc, element.objLoc);
	ACAPI_ELEMENT_MASK_SET (mask, T, objLoc);

	os.Get (FieldNames::OpeningBase::lower, element.lower);
	ACAPI_ELEMENT_MASK_SET (mask, T, lower);

	if (os.Contains (FieldNames::OpeningBase::directionType)) {
		API_WindowDoorDirectionTypes realDirectionTypeName = API_WDAssociativeToWall;
		GS::UniString directionTypeName;
		os.Get (FieldNames::OpeningBase::directionType, directionTypeName);

		GS::Optional<API_WindowDoorDirectionTypes> tmpDirectionTypeName = windowDoorDirectionTypeNames.FindValue (directionTypeName);
		if (tmpDirectionTypeName.HasValue ())
			realDirectionTypeName = tmpDirectionTypeName.Get ();
		element.directionType = realDirectionTypeName;
		ACAPI_ELEMENT_MASK_SET (mask, T, directionType);
	}

	Objects::Point3D startPoint;
	if (os.Contains (FieldNames::OpeningBase::startPoint))
		os.Get (FieldNames::OpeningBase::startPoint, startPoint);
	element.startPoint = startPoint.ToAPI_Coord ();
	ACAPI_ELEMENT_MASK_SET (mask, T, startPoint);

	Objects::Point3D dirVector;
	if (os.Contains (FieldNames::OpeningBase::dirVector))
		os.Get (FieldNames::OpeningBase::dirVector, dirVector);
	element.dirVector = dirVector.ToAPI_Coord ();
	ACAPI_ELEMENT_MASK_SET (mask, T, dirVector);

	return err;
}


class CreateOpeningBase : public CreateCommand {
protected:
	static bool	CheckEnvironment (const GS::ObjectState& currentDoor, API_Element& element);

	GS::String	GetFieldName () const override;
};


} // namespace AddOnCommands


#endif // CREATE_OPENING_BASE
