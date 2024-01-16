#ifndef GET_OPENINGBASE_DATA_HPP
#define GET_OPENINGBASE_DATA_HPP

// API
#include "APIEnvir.h"
#include "ACAPinc.h"
#include "APIMigrationHelper.hpp"

#include "Objects/Point.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
#include "Utility.hpp"


// GSRoot
#include "ObjectState.hpp"

using namespace FieldNames;


namespace AddOnCommands {


template<typename T>
GSErrCode GetDoorWindowData (const T& element, GS::ObjectState& os)
{
	os.Add (OpeningBase::revealDepthFromSide, element.revealDepthFromSide);
	os.Add (OpeningBase::jambDepthHead, element.jambDepthHead);
	os.Add (OpeningBase::jambDepth, element.jambDepth);
	os.Add (OpeningBase::jambDepth2, element.jambDepth2);
	os.Add (OpeningBase::objLoc, element.objLoc);
	os.Add (OpeningBase::lower, element.lower);
	os.Add (OpeningBase::directionType, windowDoorDirectionTypeNames.Get (element.directionType));

	os.Add (OpeningBase::startPoint, Objects::Point3D (element.startPoint.x, element.startPoint.y, 0));
	os.Add (OpeningBase::dirVector, Objects::Point3D (element.dirVector.x, element.dirVector.y, 0));

	return NoError;
}


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

	API_ElemTypeID elementType = Utility::GetElementType (element.head).typeID;
	
	// Vertical Link type and story index
	if (elementType == API_WindowID || elementType == API_DoorID) {
		os.Add (OpeningBase::VerticalLinkTypeName, verticalLinkTypeNames.Get (element.openingBase.verticalLink.linkType));
		os.Add (OpeningBase::VerticalLinkStoryIndex, element.openingBase.verticalLink.linkValue);
	}

	// Wall cut using
	if (elementType == API_WindowID) {
		os.Add (OpeningBase::WallCutUsing, element.openingBase.wallCutUsing);
	}

	API_Attribute attribute;

	// The pen index and linetype name of ...
	os.Add (OpeningBase::PenIndex, element.openingBase.pen);

	BNZeroMemory (&attribute, sizeof (API_Attribute));
	attribute.header.typeID = API_LinetypeID;
	attribute.header.index = element.openingBase.ltypeInd;

	if (NoError == ACAPI_Attribute_Get (&attribute))
		os.Add (OpeningBase::LineTypeName, GS::UniString{attribute.header.name});

	// Building material
	{
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_BuildingMaterialID;
		attribute.header.index = element.openingBase.mat;
		ACAPI_Attribute_Get (&attribute);
		os.Add (OpeningBase::BuildingMaterialName, GS::UniString{attribute.header.name});
	}

	// The section fill attributes
	BNZeroMemory (&attribute, sizeof (API_Attribute));
	attribute.header.typeID = API_FilltypeID;
	attribute.header.index = element.openingBase.sectFill;

	if (NoError == ACAPI_Attribute_Get (&attribute))
		os.Add (OpeningBase::SectFillName, GS::UniString{attribute.header.name});

	os.Add (OpeningBase::SectFillPenIndex, element.openingBase.sectFillPen);
	os.Add (OpeningBase::SectBackgroundPenIndex, element.openingBase.sectBGPen);
	os.Add (OpeningBase::SectContPenIndex, element.openingBase.sectContPen);

	// Cut line type
	BNZeroMemory (&attribute, sizeof (API_Attribute));
	attribute.header.typeID = API_LinetypeID;
	attribute.header.index = element.openingBase.cutLineType;

	if (NoError == ACAPI_Attribute_Get (&attribute))
		os.Add (OpeningBase::CutLineTypeName, GS::UniString{attribute.header.name});

	// The pen index and linetype name of above view
	os.Add (OpeningBase::AboveViewLinePenIndex, element.openingBase.aboveViewLinePen);

	BNZeroMemory (&attribute, sizeof (API_Attribute));
	attribute.header.typeID = API_LinetypeID;
	attribute.header.index = element.openingBase.aboveViewLineType;

	if (NoError == ACAPI_Attribute_Get (&attribute))
		os.Add (OpeningBase::AboveViewLineTypeName, GS::UniString{attribute.header.name});

	// The pen index and linetype name of below view
	os.Add (OpeningBase::BelowViewLinePenIndex, element.openingBase.belowViewLinePen);

	BNZeroMemory (&attribute, sizeof (API_Attribute));
	attribute.header.typeID = API_LinetypeID;
	attribute.header.index = element.openingBase.belowViewLineType;

	if (NoError == ACAPI_Attribute_Get (&attribute))
		os.Add (OpeningBase::BelowViewLineTypeName, GS::UniString{attribute.header.name});

	os.Add (OpeningBase::UseObjectPens, element.openingBase.useObjPens);
	os.Add (OpeningBase::UseObjLinetypes, element.openingBase.useObjLtypes);
	os.Add (OpeningBase::UseObjMaterials, element.openingBase.useObjMaterials);
	os.Add (OpeningBase::UseObjSectAttrs, element.openingBase.useObjSectAttrs);

	// Library part name
	{
		API_LibPart libPart;
		BNZeroMemory (&libPart, sizeof (API_LibPart));
		libPart.index = element.openingBase.libInd;

		err = ACAPI_LibraryPart_Get (&libPart);
		if (err == NoError)
			os.Add (OpeningBase::LibraryPart, GS::UniString (libPart.docu_UName));
	}

	// The display options (Projected, Projected with Overhead, Cut Only, Outlines Only, Overhead All or Symbolic Cut)
	os.Add (OpeningBase::DisplayOptionName, displayOptionNames.Get (element.openingBase.displayOption));

	return err;
}


} // namespace AddOnCommands


#endif // GET_OPENINGBASE_DATA_HPP

