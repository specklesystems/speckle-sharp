#include "CreateOpening.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "Objects/Vector.hpp"
#include "RealNumber.h"
#include "DGModule.hpp"
#include "LibpartImportManager.hpp"
#include "FieldNames.hpp"
#include "OnExit.hpp"
#include "ExchangeManager.hpp"
#include "TypeNameTables.hpp"
#include "Database.hpp"
#include "BM.hpp"


using namespace FieldNames;


namespace AddOnCommands
{


GS::String CreateOpening::GetFieldName () const
{
	return FieldNames::Openings;
}


GS::UniString CreateOpening::GetUndoableCommandName () const
{
	return "CreateSpeckleOpening";
}


GS::ErrCode CreateOpening::GetElementFromObjectState (const GS::ObjectState& os,
		API_Element& element,
		API_Element& elementMask,
		API_ElementMemo& memo,
		GS::UInt64& /*memoMask*/,
		API_SubElement** /*marker*/,
		AttributeManager& /*attributeManager*/,
		LibpartImportManager& /*libpartImportManager*/,
		GS::Array<GS::UniString>& log) const
{
	GS::ErrCode err = NoError;

	Utility::SetElementType (element.header, API_OpeningID);

	err = Utility::GetBaseElementData (element, &memo, nullptr, log);
	if (err != NoError)
		return err;

	err = GetElementBaseFromObjectState (os, element, elementMask);
	if (err != NoError)
		return err;

	if (!CheckEnvironment (os, element))
		return Error;

	if (os.Contains (Opening::FloorPlanDisplayMode)) {
		GS::UniString floorPlanDisplayModeName;
		os.Get (Opening::FloorPlanDisplayMode, floorPlanDisplayModeName);

		GS::Optional<API_OpeningFloorPlanDisplayModeTypeID> type = openingFloorPlanDisplayModeNames.FindValue (floorPlanDisplayModeName);
		if (type.HasValue ()) {
			element.opening.floorPlanParameters.floorPlanDisplayMode = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.floorPlanDisplayMode);
		}
	}

	if (os.Contains (Opening::ConnectionMode)) {
		GS::UniString connectionModeName;
		os.Get (Opening::ConnectionMode, connectionModeName);

		GS::Optional<API_OpeningFloorPlanConnectionModeTypeID> type = openingFloorPlanConnectionModeNames.FindValue (connectionModeName);
		if (type.HasValue ()) {
			element.opening.floorPlanParameters.connectionMode = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.connectionMode);
		}
	}

	if (os.Contains (Opening::CutSurfacesUseLineOfCutElements)) {
		os.Get (Opening::CutSurfacesUseLineOfCutElements, element.opening.floorPlanParameters.cutSurfacesParameters.useLineOfCutElements);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.cutSurfacesParameters.useLineOfCutElements);
	}

	if (os.Contains (Opening::CutSurfacesLinePenIndex)) {
		os.Get (Opening::CutSurfacesLinePenIndex, element.opening.floorPlanParameters.cutSurfacesParameters.linePenIndex);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.cutSurfacesParameters.linePenIndex);
	}

	GS::UniString attributeName;
	if (os.Contains (Opening::CutSurfacesLineIndex)) {

		os.Get (Opening::CutSurfacesLineIndex, attributeName);

		if (!attributeName.IsEmpty ()) {
				API_Attribute attrib;
				BNZeroMemory (&attrib, sizeof (API_Attribute));
				attrib.header.typeID = API_LinetypeID;
				CHCopyC (attributeName.ToCStr (), attrib.header.name);

				if (NoError == ACAPI_Attribute_Get (&attrib)) {
					element.opening.floorPlanParameters.cutSurfacesParameters.lineIndex = attrib.header.index;
					ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.cutSurfacesParameters.lineIndex);
			}
		}
	}

	if (os.Contains (Opening::OutlinesStyle)) {
		GS::UniString outlinesStyleName;
		os.Get (Opening::OutlinesStyle, outlinesStyleName);

		GS::Optional<API_OpeningFloorPlanOutlinesStyleTypeID> type = openingOutlinesStyleNames.FindValue (outlinesStyleName);
		if (type.HasValue ()) {
			element.opening.floorPlanParameters.outlinesParameters.outlinesStyle = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.outlinesParameters.outlinesStyle);
		}
	}

	if (os.Contains (Opening::OutlinesUseLineOfCutElements)) {
		os.Get (Opening::OutlinesUseLineOfCutElements, element.opening.floorPlanParameters.outlinesParameters.useLineOfCutElements);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.outlinesParameters.useLineOfCutElements);
	}

	if (os.Contains (Opening::OutlinesUncutLineIndex)) {

		os.Get (Opening::OutlinesUncutLineIndex, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attrib;
			BNZeroMemory (&attrib, sizeof (API_Attribute));
			attrib.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attrib.header.name);

			if (NoError == ACAPI_Attribute_Get (&attrib)) {
				element.opening.floorPlanParameters.outlinesParameters.uncutLineIndex = attrib.header.index;
				ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.outlinesParameters.uncutLineIndex);
			}
		}
	}

	if (os.Contains (Opening::OutlinesOverheadLineIndex)) {

		os.Get (Opening::OutlinesOverheadLineIndex, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attrib;
			BNZeroMemory (&attrib, sizeof (API_Attribute));
			attrib.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attrib.header.name);

			if (NoError == ACAPI_Attribute_Get (&attrib)) {
				element.opening.floorPlanParameters.outlinesParameters.overheadLineIndex = attrib.header.index;
				ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.outlinesParameters.overheadLineIndex);
			}
		}
	}

	if (os.Contains (Opening::OutlinesOverheadLinePenIndex)) {
		os.Get (Opening::OutlinesOverheadLinePenIndex, element.opening.floorPlanParameters.outlinesParameters.overheadLinePenIndex);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.outlinesParameters.overheadLinePenIndex);
	}

	if (os.Contains (Opening::UseCoverFills)) {
		os.Get (Opening::UseCoverFills, element.opening.floorPlanParameters.coverFillsParameters.useCoverFills);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.coverFillsParameters.useCoverFills);
	}

	if (os.Contains (Opening::UseFillsOfCutElements)) {
		os.Get (Opening::UseFillsOfCutElements, element.opening.floorPlanParameters.coverFillsParameters.useFillsOfCutElements);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.coverFillsParameters.useFillsOfCutElements);
	}

	if (os.Contains (Opening::CoverFillIndex)) {

		os.Get (Opening::CoverFillIndex, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attrib;
			BNZeroMemory (&attrib, sizeof (API_Attribute));
			attrib.header.typeID = API_FilltypeID;
			CHCopyC (attributeName.ToCStr (), attrib.header.name);

			if (NoError == ACAPI_Attribute_Get (&attrib)) {
				element.opening.floorPlanParameters.coverFillsParameters.coverFillIndex = attrib.header.index;
				ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.coverFillsParameters.coverFillIndex);
			}
		}
	}

	if (os.Contains (Opening::CoverFillPenIndex)) {
		os.Get (Opening::CoverFillPenIndex, element.opening.floorPlanParameters.coverFillsParameters.coverFillPenIndex);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.coverFillsParameters.coverFillPenIndex);
	}

	if (os.Contains (Opening::CoverFillBackgroundPenIndex)) {
		os.Get (Opening::CoverFillBackgroundPenIndex, element.opening.floorPlanParameters.coverFillsParameters.coverFillBackgroundPenIndex);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.coverFillsParameters.coverFillBackgroundPenIndex);
	}

	if (os.Contains (Opening::CoverFillOrientation)) {
		os.Get (Opening::CoverFillOrientation, element.opening.floorPlanParameters.coverFillsParameters.coverFillOrientation);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.coverFillsParameters.coverFillOrientation);
	}

	if (os.Contains (Opening::CoverFillTransformationOrigoX)) {
		os.Get (Opening::CoverFillTransformationOrigoX, element.opening.floorPlanParameters.coverFillTransformation.origo.x);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.coverFillTransformation.origo.x);
	}

	if (os.Contains (Opening::CoverFillTransformationOrigoY)) {
		os.Get (Opening::CoverFillTransformationOrigoY, element.opening.floorPlanParameters.coverFillTransformation.origo.y);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.coverFillTransformation.origo.y);
	}

	if (os.Contains (Opening::CoverFillTransformationXAxisX)) {
		os.Get (Opening::CoverFillTransformationXAxisX, element.opening.floorPlanParameters.coverFillTransformation.xAxis.x);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.coverFillTransformation.xAxis.x);
	}

	if (os.Contains (Opening::CoverFillTransformationXAxisY)) {
		os.Get (Opening::CoverFillTransformationXAxisY, element.opening.floorPlanParameters.coverFillTransformation.xAxis.y);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.coverFillTransformation.xAxis.y);
	}

	if (os.Contains (Opening::CoverFillTransformationYAxisX)) {
		os.Get (Opening::CoverFillTransformationYAxisX, element.opening.floorPlanParameters.coverFillTransformation.yAxis.x);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.coverFillTransformation.yAxis.x);
	}

	if (os.Contains (Opening::CoverFillTransformationYAxisY)) {
		os.Get (Opening::CoverFillTransformationYAxisY, element.opening.floorPlanParameters.coverFillTransformation.yAxis.y);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.coverFillTransformation.yAxis.y);
	}

	if (os.Contains (Opening::ShowReferenceAxis)) {
		os.Get (Opening::ShowReferenceAxis, element.opening.floorPlanParameters.referenceAxisParameters.showReferenceAxis);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.referenceAxisParameters.showReferenceAxis);
	}

	if (os.Contains (Opening::ReferenceAxisPenIndex)) {
		os.Get (Opening::ReferenceAxisPenIndex, element.opening.floorPlanParameters.referenceAxisParameters.referenceAxisPenIndex);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.referenceAxisParameters.referenceAxisPenIndex);
	}

	if (os.Contains (Opening::ReferenceAxisLineTypeIndex)) {

		os.Get (Opening::ReferenceAxisLineTypeIndex, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attrib;
			BNZeroMemory (&attrib, sizeof (API_Attribute));
			attrib.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attrib.header.name);

			if (NoError == ACAPI_Attribute_Get (&attrib)) {
				element.opening.floorPlanParameters.referenceAxisParameters.referenceAxisLineTypeIndex = attrib.header.index;
				ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.referenceAxisParameters.referenceAxisLineTypeIndex);
			}
		}
	}

	if (os.Contains (Opening::ReferenceAxisOverhang)) {
		os.Get (Opening::ReferenceAxisOverhang, element.opening.floorPlanParameters.referenceAxisParameters.referenceAxisOverhang);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, floorPlanParameters.referenceAxisParameters.referenceAxisOverhang);
	}

	Objects::Point3D basePoint;
	if (os.Contains (Opening::ExtrusionGeometryBasePoint)) {
		os.Get (Opening::ExtrusionGeometryBasePoint, basePoint);
		element.opening.extrusionGeometryData.frame.basePoint = basePoint.ToAPI_Coord3D ();
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, extrusionGeometryData.frame.basePoint);
	}

	Objects::Vector3D axisX;
	if (os.Contains (Opening::ExtrusionGeometryXAxis)) {
		os.Get (Opening::ExtrusionGeometryXAxis, axisX);
		element.opening.extrusionGeometryData.frame.axisX = axisX.ToAPI_Vector3D ();
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, extrusionGeometryData.frame.axisX);
	}

	Objects::Vector3D axisY;
	if (os.Contains (Opening::ExtrusionGeometryYAxis)) {
		os.Get (Opening::ExtrusionGeometryYAxis, axisY);
		element.opening.extrusionGeometryData.frame.axisY = axisY.ToAPI_Vector3D ();
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, extrusionGeometryData.frame.axisY);
	}

	Objects::Vector3D axisZ;
	if (os.Contains (Opening::ExtrusionGeometryZAxis)) {
		os.Get (Opening::ExtrusionGeometryZAxis, axisZ);
		element.opening.extrusionGeometryData.frame.axisZ = axisZ.ToAPI_Vector3D ();
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, extrusionGeometryData.frame.axisZ);
	}

	if (os.Contains (Opening::BasePolygonType)) {
		GS::UniString basePolygonTypeName;
		os.Get (Opening::BasePolygonType, basePolygonTypeName);

		GS::Optional<API_OpeningBasePolygonTypeTypeID> type = openingBasePolygonTypeNames.FindValue (basePolygonTypeName);
		if (type.HasValue ()) {
			element.opening.extrusionGeometryData.parameters.basePolygonType = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, extrusionGeometryData.parameters.basePolygonType);
		}
	}

	if (os.Contains (Opening::Width)) {
		os.Get (Opening::Width, element.opening.extrusionGeometryData.parameters.width);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, extrusionGeometryData.parameters.width);

		element.opening.extrusionGeometryData.parameters.linkedStatus = API_OpeningNotLinked;
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, extrusionGeometryData.parameters.linkedStatus);
	}

	if (os.Contains (Opening::Height)) {
		os.Get (Opening::Height, element.opening.extrusionGeometryData.parameters.height);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, extrusionGeometryData.parameters.height);

		element.opening.extrusionGeometryData.parameters.linkedStatus = API_OpeningNotLinked;
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, extrusionGeometryData.parameters.linkedStatus);
	}

	if (os.Contains (Opening::Constraint)) {
		os.Get (Opening::Constraint, element.opening.extrusionGeometryData.parameters.constraint);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, extrusionGeometryData.parameters.constraint);
	}

	if (os.Contains (Opening::AnchorIndex)) {
		os.Get (Opening::AnchorIndex, element.opening.extrusionGeometryData.parameters.anchor);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, extrusionGeometryData.parameters.anchor);
	}

	if (os.Contains (Opening::AnchorAltitude)) {
		os.Get (Opening::AnchorAltitude, element.opening.extrusionGeometryData.parameters.anchorAltitude);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, extrusionGeometryData.parameters.anchorAltitude);
	}

	if (os.Contains (Opening::LimitType)) {
		GS::UniString limitTypeName;
		os.Get (Opening::LimitType, limitTypeName);

		GS::Optional<API_OpeningLimitTypeTypeID> type = openingLimitTypeNames.FindValue (limitTypeName);
		if (type.HasValue ()) {
			element.opening.extrusionGeometryData.parameters.limitType = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, extrusionGeometryData.parameters.limitType);
		}
	}

	if (os.Contains (Opening::ExtrusionStartOffSet)) {
		os.Get (Opening::ExtrusionStartOffSet, element.opening.extrusionGeometryData.parameters.extrusionStartOffset);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, extrusionGeometryData.parameters.extrusionStartOffset);
	}

	if (os.Contains (Opening::FiniteBodyLength)) {
		os.Get (Opening::FiniteBodyLength, element.opening.extrusionGeometryData.parameters.finiteBodyLength);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_OpeningType, extrusionGeometryData.parameters.finiteBodyLength);
	}

	return err;
}


GS::String CreateOpening::GetName () const
{
	return CreateOpeningCommandName;
}


}
