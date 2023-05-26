#ifndef CREATE_OPENING_BASE
#define CREATE_OPENING_BASE

#include "CreateCommand.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
#include "Objects/Point.hpp"
#include "Utility.hpp"
#include "ResourceIds.hpp"

namespace AddOnCommands {

template<typename T>
GSErrCode GetDoorWindowFromObjectState (const GS::ObjectState& os, T& element, API_Element& mask, GS::Array<GS::UniString>& /*log*/)
{
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
	if (os.Contains (FieldNames::OpeningBase::startPoint)) {
		os.Get (FieldNames::OpeningBase::startPoint, startPoint);
		element.startPoint = startPoint.ToAPI_Coord ();
		ACAPI_ELEMENT_MASK_SET (mask, T, startPoint);
	}

	Objects::Point3D dirVector;
	if (os.Contains (FieldNames::OpeningBase::dirVector)) {
		os.Get (FieldNames::OpeningBase::dirVector, dirVector);
		element.dirVector = dirVector.ToAPI_Coord ();
		ACAPI_ELEMENT_MASK_SET (mask, T, dirVector);
	}

	return NoError;
}


template<typename T>
GSErrCode GetOpeningBaseFromObjectState (const GS::ObjectState& os, T& element, API_Element& mask, GS::Array<GS::UniString>& log)
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

	// Vertical Link type and story index
	if (os.Contains (FieldNames::OpeningBase::VerticalLinkTypeName)) {
		GS::UniString verticalLinkTypeName;
		os.Get (FieldNames::OpeningBase::VerticalLinkTypeName, verticalLinkTypeName);

		GS::Optional<API_VerticalLinkID> type = verticalLinkTypeNames.FindValue (verticalLinkTypeName);
		if (type.HasValue ()) {
			element.openingBase.verticalLink.linkType = type.Get ();
			ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.verticalLink.linkType);
		}

		os.Get (FieldNames::OpeningBase::VerticalLinkStoryIndex, element.openingBase.verticalLink.linkValue);
		ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.verticalLink.linkValue);
	}

	// Wall cut using
	if (os.Contains (FieldNames::OpeningBase::WallCutUsing)) {
		os.Get (FieldNames::OpeningBase::WallCutUsing, element.openingBase.wallCutUsing);
		ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.wallCutUsing);
	}

	// The pen index and linetype name of ...
	os.Get (FieldNames::OpeningBase::PenIndex, element.openingBase.pen);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.pen);

	GS::UniString attributeName;
	if (os.Contains (FieldNames::OpeningBase::LineTypeName)) {

		os.Get (FieldNames::OpeningBase::LineTypeName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.openingBase.ltypeInd = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.ltypeInd);
			} else {
				log.Push (Utility::ComposeLogMessage (ID_LOG_MESSAGE_ATTRIBUTE_SEARCH_ERROR, attributeName.ToPrintf ()));
			}
		}
	}

	// Building material
	if (os.Contains (FieldNames::OpeningBase::BuildingMaterialName)) {

		os.Get (FieldNames::OpeningBase::BuildingMaterialName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_BuildingMaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.openingBase.mat = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.mat);
			} else {
				log.Push (Utility::ComposeLogMessage (ID_LOG_MESSAGE_ATTRIBUTE_SEARCH_ERROR, attributeName.ToPrintf ()));
			}
		}
	}

	// The section fill attributes
	if (os.Contains (FieldNames::OpeningBase::SectFillName)) {

		os.Get (FieldNames::OpeningBase::SectFillName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_FilltypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.openingBase.sectFill = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.sectFill);
			} else {
				log.Push (Utility::ComposeLogMessage (ID_LOG_MESSAGE_ATTRIBUTE_SEARCH_ERROR, attributeName.ToPrintf ()));
			}
		}
	}

	os.Get (FieldNames::OpeningBase::SectFillPenIndex, element.openingBase.sectFillPen);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.sectFillPen);

	os.Get (FieldNames::OpeningBase::SectBackgroundPenIndex, element.openingBase.sectBGPen);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.sectBGPen);

	os.Get (FieldNames::OpeningBase::SectContPenIndex, element.openingBase.sectContPen);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.sectContPen);

	// Cut line type
	if (os.Contains (FieldNames::OpeningBase::CutLineTypeName)) {

		os.Get (FieldNames::OpeningBase::CutLineTypeName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.openingBase.cutLineType = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.cutLineType);
			} else {
				log.Push (Utility::ComposeLogMessage (ID_LOG_MESSAGE_ATTRIBUTE_SEARCH_ERROR, attributeName.ToPrintf ()));
			}
		}
	}

	// The pen index and linetype name of above view
	os.Get (FieldNames::OpeningBase::AboveViewLinePenIndex, element.openingBase.aboveViewLinePen);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.aboveViewLinePen);

	if (os.Contains (FieldNames::OpeningBase::AboveViewLineTypeName)) {

		os.Get (FieldNames::OpeningBase::AboveViewLineTypeName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.openingBase.aboveViewLineType = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.aboveViewLineType);
			} else {
				log.Push (Utility::ComposeLogMessage (ID_LOG_MESSAGE_ATTRIBUTE_SEARCH_ERROR, attributeName.ToPrintf ()));
			}
		}
	}

	// The pen index and linetype name of below view
	os.Get (FieldNames::OpeningBase::BelowViewLinePenIndex, element.openingBase.belowViewLinePen);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.belowViewLinePen);

	if (os.Contains (FieldNames::OpeningBase::BelowViewLineTypeName)) {

		os.Get (FieldNames::OpeningBase::BelowViewLineTypeName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.openingBase.belowViewLineType = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.belowViewLineType);
			} else {
				log.Push (Utility::ComposeLogMessage (ID_LOG_MESSAGE_ATTRIBUTE_SEARCH_ERROR, attributeName.ToPrintf ()));
			}
		}
	}

	os.Get (FieldNames::OpeningBase::UseObjectPens, element.openingBase.useObjPens);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.useObjPens);

	os.Get (FieldNames::OpeningBase::UseObjLinetypes, element.openingBase.useObjLtypes);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.useObjLtypes);

	os.Get (FieldNames::OpeningBase::UseObjMaterials, element.openingBase.useObjMaterials);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.useObjMaterials);

	os.Get (FieldNames::OpeningBase::UseObjSectAttrs, element.openingBase.useObjSectAttrs);
	ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.useObjSectAttrs);

	if (os.Contains (FieldNames::OpeningBase::LibraryPart)) {
		GS::UniString libPartName;
		os.Get (FieldNames::OpeningBase::LibraryPart, libPartName);

		if (!libPartName.IsEmpty ()) {
			API_LibPart libPart;
			BNZeroMemory (&libPart, sizeof (API_LibPart));
			GS::ucscpy (libPart.docu_UName, libPartName.ToUStr ());

			err = ACAPI_LibPart_Search (&libPart, false, true);
			if (err == NoError) {
				element.openingBase.libInd = libPart.index;
				ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.libInd);
			} else {
				log.Push (Utility::ComposeLogMessage (ID_LOG_MESSAGE_LIBPART_SEARCH_ERROR, libPartName.ToPrintf ()));
			}
		}
	}

	// The display options (Projected, Projected with Overhead, Cut Only, Outlines Only, Overhead All or Symbolic Cut)
	if (os.Contains (FieldNames::OpeningBase::DisplayOptionName)) {
		GS::UniString displayOptionName;
		os.Get (FieldNames::OpeningBase::DisplayOptionName, displayOptionName);

		GS::Optional<API_ElemDisplayOptionsID> type = displayOptionNames.FindValue (displayOptionName);
		if (type.HasValue ()) {
			element.openingBase.displayOption = type.Get ();
			ACAPI_ELEMENT_MASK_SET (mask, T, openingBase.displayOption);
		}
	}

	return err;
}


class CreateOpeningBase : public CreateCommand {
protected:
	static bool	CheckEnvironment (const GS::ObjectState& os, API_Element& element);

	GS::String	GetFieldName () const override;
};


} // namespace AddOnCommands


#endif // CREATE_OPENING_BASE
