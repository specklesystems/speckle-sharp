#include "GetOpeningData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "Objects/Vector.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
using namespace FieldNames;


namespace AddOnCommands {


GS::String GetOpeningData::GetFieldName () const
{
	return Openings;
}


API_ElemTypeID GetOpeningData::GetElemTypeID () const
{
	return API_OpeningID;
}


GS::ErrCode GetOpeningData::SerializeElementType (const API_Element& element,
  const API_ElementMemo& /*memo*/,
  GS::ObjectState& os) const
{
	os.Add (ElementBase::ParentElementId, APIGuidToString (element.opening.owner));

	API_Attribute attrib;

	// Opening Floor Parameters
	os.Add (Opening::FloorPlanDisplayMode, openingFloorPlanDisplayModeNames.Get (element.opening.floorPlanParameters.floorPlanDisplayMode));
	os.Add (Opening::ConnectionMode, openingFloorPlanConnectionModeNames.Get (element.opening.floorPlanParameters.connectionMode));

	// Opening Floor Plan Parameters - Cut Surfaces
	os.Add (Opening::CutSurfacesUseLineOfCutElements, element.opening.floorPlanParameters.cutSurfacesParameters.useLineOfCutElements);
	os.Add (Opening::CutSurfacesLinePenIndex, element.opening.floorPlanParameters.cutSurfacesParameters.linePenIndex);
	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = element.opening.floorPlanParameters.cutSurfacesParameters.lineIndex;

	if (NoError == ACAPI_Attribute_Get (&attrib)) {
		os.Add (Opening::CutSurfacesLineIndex, GS::UniString{attrib.header.name});
	}

	// Opening Floor Plan Parameters - Outlines Parameters
	os.Add (Opening::OutlinesStyle, openingOutlinesStyleNames.Get (element.opening.floorPlanParameters.outlinesParameters.outlinesStyle));
	os.Add (Opening::OutlinesUseLineOfCutElements, element.opening.floorPlanParameters.outlinesParameters.useLineOfCutElements);

	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = element.opening.floorPlanParameters.outlinesParameters.uncutLineIndex;

	if (NoError == ACAPI_Attribute_Get (&attrib)) {
		os.Add (Opening::OutlinesUncutLineIndex, GS::UniString{attrib.header.name});
	}

	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = element.opening.floorPlanParameters.outlinesParameters.overheadLineIndex;

	if (NoError == ACAPI_Attribute_Get (&attrib)) {
		os.Add (Opening::OutlinesOverheadLineIndex, GS::UniString{attrib.header.name});
	}

	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = element.opening.floorPlanParameters.outlinesParameters.uncutLineIndex;

	if (NoError == ACAPI_Attribute_Get (&attrib)) {
		os.Add (Opening::OutlinesUncutLineIndex, GS::UniString{attrib.header.name});
	}
	os.Add (Opening::OutlinesOverheadLinePenIndex, element.opening.floorPlanParameters.outlinesParameters.overheadLinePenIndex);

	// Opening Floor Plan Parameters - Cover Fills
	os.Add (Opening::UseCoverFills, element.opening.floorPlanParameters.coverFillsParameters.useCoverFills);
	if (element.opening.floorPlanParameters.coverFillsParameters.useCoverFills) {
		os.Add (Opening::UseFillsOfCutElements, element.opening.floorPlanParameters.coverFillsParameters.useFillsOfCutElements);

		BNZeroMemory (&attrib, sizeof (API_Attribute));
		attrib.header.typeID = API_FilltypeID;
		attrib.header.index = element.opening.floorPlanParameters.coverFillsParameters.coverFillIndex;

		if (NoError == ACAPI_Attribute_Get (&attrib)) {
			os.Add (Opening::CoverFillIndex, GS::UniString{attrib.header.name});
		}

		os.Add (Opening::CoverFillPenIndex, element.opening.floorPlanParameters.coverFillsParameters.coverFillPenIndex);
		os.Add (Opening::CoverFillBackgroundPenIndex, element.opening.floorPlanParameters.coverFillsParameters.coverFillBackgroundPenIndex);
		os.Add (Opening::CoverFillOrientation, element.opening.floorPlanParameters.coverFillsParameters.coverFillOrientation);
	}
	
	// Opening Floor Plan Parameters - Cover Fill Transformation
	os.Add (Opening::CoverFillTransformationOrigoX, element.opening.floorPlanParameters.coverFillTransformation.origo.x);
	os.Add (Opening::CoverFillTransformationOrigoY, element.opening.floorPlanParameters.coverFillTransformation.origo.y);
	os.Add (Opening::CoverFillTransformationXAxisX, element.opening.floorPlanParameters.coverFillTransformation.xAxis.x);
	os.Add (Opening::CoverFillTransformationXAxisY, element.opening.floorPlanParameters.coverFillTransformation.xAxis.y);
	os.Add (Opening::CoverFillTransformationYAxisX, element.opening.floorPlanParameters.coverFillTransformation.yAxis.x);
	os.Add (Opening::CoverFillTransformationYAxisY, element.opening.floorPlanParameters.coverFillTransformation.yAxis.y);

	// Opening Floor Plan Parameters - Reference Axis
	os.Add (Opening::ShowReferenceAxis, element.opening.floorPlanParameters.referenceAxisParameters.showReferenceAxis);
	if (element.opening.floorPlanParameters.referenceAxisParameters.showReferenceAxis) {
		os.Add (Opening::ReferenceAxisPenIndex, element.opening.floorPlanParameters.referenceAxisParameters.referenceAxisPenIndex);

		BNZeroMemory (&attrib, sizeof (API_Attribute));
		attrib.header.typeID = API_LinetypeID;
		attrib.header.index = element.opening.floorPlanParameters.referenceAxisParameters.referenceAxisLineTypeIndex;

		if (NoError == ACAPI_Attribute_Get (&attrib)) {
			os.Add (Opening::ReferenceAxisLineTypeIndex, GS::UniString{attrib.header.name});
		}

		os.Add (Opening::ReferenceAxisOverhang, element.opening.floorPlanParameters.referenceAxisParameters.referenceAxisOverhang);
	}

	// Extrusion Geometry
	os.Add (Opening::ExtrusionGeometryBasePoint, Objects::Point3D (element.opening.extrusionGeometryData.frame.basePoint));
	os.Add (Opening::ExtrusionGeometryXAxis, Objects::Vector3D(element.opening.extrusionGeometryData.frame.axisX));
	os.Add (Opening::ExtrusionGeometryYAxis, Objects::Vector3D (element.opening.extrusionGeometryData.frame.axisY));
	os.Add (Opening::ExtrusionGeometryZAxis, Objects::Vector3D (element.opening.extrusionGeometryData.frame.axisZ));

	// Extrusion Geometry - Opening Extrusion Parameters
	os.Add (Opening::BasePolygonType, openingBasePolygonTypeNames.Get (element.opening.extrusionGeometryData.parameters.basePolygonType));
	os.Add (Opening::Width, element.opening.extrusionGeometryData.parameters.width);
	os.Add (Opening::Height, element.opening.extrusionGeometryData.parameters.height);
	os.Add (Opening::Constraint, element.opening.extrusionGeometryData.parameters.constraint);
	os.Add (Opening::Anchor, openingAnchorNames.Get (element.opening.extrusionGeometryData.parameters.anchor));
	os.Add (Opening::AnchorIndex, (element.opening.extrusionGeometryData.parameters.anchor));
	os.Add (Opening::AnchorAltitude, element.opening.extrusionGeometryData.parameters.anchorAltitude);
	os.Add (Opening::LimitType, openingLimitTypeNames.Get (element.opening.extrusionGeometryData.parameters.limitType));
	os.Add (Opening::ExtrusionStartOffSet, element.opening.extrusionGeometryData.parameters.extrusionStartOffset);
	os.Add (Opening::FiniteBodyLength, element.opening.extrusionGeometryData.parameters.finiteBodyLength);
	os.Add (Opening::LinkedStatus, openingLinkedStatusNames.Get (element.opening.extrusionGeometryData.parameters.linkedStatus));

	// Extrusion Geometry - Custom Base Polygon
	if (element.opening.extrusionGeometryData.parameters.basePolygonType == API_OpeningBasePolygonCustom) {
		// Reserved for future use
	}

	return NoError;
}


GS::String GetOpeningData::GetName () const
{
	return GetOpeningDataCommandName;
}


} // namespace AddOnCommands
