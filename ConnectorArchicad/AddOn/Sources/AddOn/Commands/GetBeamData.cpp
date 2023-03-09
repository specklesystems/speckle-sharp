#include "GetBeamData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
using namespace FieldNames;

namespace AddOnCommands
{

static GS::ObjectState SerializeBeamType (const API_Element& elem, const API_ElementMemo& memo)
{
	GS::ObjectState os;

	// The identifier of the beam
	os.Add (ApplicationId, APIGuidToString (elem.beam.head.guid));

	// Positioning
	os.Add (FloorIndex, elem.beam.head.floorInd);

	double z = Utility::GetStoryLevel (elem.beam.head.floorInd) + elem.beam.level;
	os.Add (Beam::begC, Objects::Point3D (elem.beam.begC.x, elem.beam.begC.y, z));
	os.Add (Beam::endC, Objects::Point3D (elem.beam.endC.x, elem.beam.endC.y, z));
	os.Add (Beam::level, elem.beam.level);
	os.Add (Beam::isSlanted, elem.beam.isSlanted);
	os.Add (Beam::slantAngle, elem.beam.slantAngle);
	os.Add (Beam::beamShape, beamShapeTypeNames.Get (elem.beam.beamShape));
	os.Add (Beam::sequence, elem.beam.sequence);
	os.Add (Beam::curveAngle, elem.beam.curveAngle);
	os.Add (Beam::verticalCurveHeight, elem.beam.verticalCurveHeight);
	os.Add (Beam::isFlipped, elem.beam.isFlipped);

	// End Cuts
	os.Add (Beam::nCuts, elem.beam.nCuts);

	if (memo.assemblySegmentCuts != nullptr) {
		GS::ObjectState allCuts;
		Utility::GetAllCutData (memo.assemblySegmentCuts, allCuts);
		os.Add (AssemblySegment::CutData, allCuts);
	}

	// Reference Axis
	os.Add (Beam::anchorPoint, elem.beam.anchorPoint);
	os.Add (Beam::offset, elem.beam.offset);
	os.Add (Beam::profileAngle, elem.beam.profileAngle);

	// Segment
	API_Attribute attrib;
	os.Add (Beam::nSegments, elem.beam.nSegments);
	os.Add (Beam::nProfiles, elem.beam.nProfiles);

	bool NotOnlyProfileSegment = false;
	if (memo.beamSegments != nullptr) {
		GS::ObjectState allSegments;

		GSSize segmentsCount = BMGetPtrSize (reinterpret_cast<GSPtr>(memo.beamSegments)) / sizeof (API_BeamSegmentType);
		DBASSERT (segmentsCount == elem.beam.nSegments);

		for (GSSize idx = 0; idx < segmentsCount; ++idx) {
			API_BeamSegmentType beamSegment = memo.beamSegments[idx];
			GS::ObjectState currentSegment;

			GS::ObjectState assemblySegment;
			Utility::GetSegmentData (beamSegment.assemblySegmentData, assemblySegment);
			currentSegment.Add (Beam::BeamSegment::segmentData, assemblySegment);

			if (beamSegment.assemblySegmentData.modelElemStructureType != API_ProfileStructure)
				NotOnlyProfileSegment = true;

			// The left overridden material name
			int countOverriddenMaterial = 0;
			if (beamSegment.leftMaterial.overridden) {
				BNZeroMemory (&attrib, sizeof (API_Attribute));
				attrib.header.typeID = API_MaterialID;
				attrib.header.index = beamSegment.leftMaterial.attributeIndex;

				if (NoError == ACAPI_Attribute_Get (&attrib))
					countOverriddenMaterial = countOverriddenMaterial + 1;
				currentSegment.Add (Beam::BeamSegment::LeftMaterial, GS::UniString{attrib.header.name});
			}

			// The top overridden material name
			if (beamSegment.topMaterial.overridden) {
				BNZeroMemory (&attrib, sizeof (API_Attribute));
				attrib.header.typeID = API_MaterialID;
				attrib.header.index = beamSegment.topMaterial.attributeIndex;

				if (NoError == ACAPI_Attribute_Get (&attrib))
					countOverriddenMaterial = countOverriddenMaterial + 1;
				currentSegment.Add (Beam::BeamSegment::TopMaterial, GS::UniString{attrib.header.name});
			}

			// The right overridden material name
			if (beamSegment.rightMaterial.overridden) {
				BNZeroMemory (&attrib, sizeof (API_Attribute));
				attrib.header.typeID = API_MaterialID;
				attrib.header.index = beamSegment.rightMaterial.attributeIndex;

				if (NoError == ACAPI_Attribute_Get (&attrib))
					countOverriddenMaterial = countOverriddenMaterial + 1;
				currentSegment.Add (Beam::BeamSegment::RightMaterial, GS::UniString{attrib.header.name});
			}

			// The bottom overridden material name
			if (beamSegment.bottomMaterial.overridden) {
				BNZeroMemory (&attrib, sizeof (API_Attribute));
				attrib.header.typeID = API_MaterialID;
				attrib.header.index = beamSegment.bottomMaterial.attributeIndex;

				if (NoError == ACAPI_Attribute_Get (&attrib))
					countOverriddenMaterial = countOverriddenMaterial + 1;
				currentSegment.Add (Beam::BeamSegment::BottomMaterial, GS::UniString{attrib.header.name});
			}

			// The ends overridden material name
			if (beamSegment.endsMaterial.overridden) {
				BNZeroMemory (&attrib, sizeof (API_Attribute));
				attrib.header.typeID = API_MaterialID;
				attrib.header.index = beamSegment.endsMaterial.attributeIndex;

				if (NoError == ACAPI_Attribute_Get (&attrib))
					countOverriddenMaterial = countOverriddenMaterial + 1;
				currentSegment.Add (Beam::BeamSegment::EndsMaterial, GS::UniString{attrib.header.name});
			}

			// The overridden materials are chained
			if (countOverriddenMaterial > 1) {
				currentSegment.Add (Beam::BeamSegment::MaterialsChained, beamSegment.materialsChained);
			}

			allSegments.Add (GS::String::SPrintf (AssemblySegment::SegmentName, idx + 1), currentSegment);
		}

		os.Add (Beam::segments, allSegments);
	}

	// Scheme
	os.Add (Beam::nSchemes, elem.beam.nSchemes);

	if (memo.assemblySegmentSchemes != nullptr) {
		GS::ObjectState allSchemes;
		Utility::GetAllSchemeData (memo.assemblySegmentSchemes, allSchemes);
		os.Add (AssemblySegment::SchemeData, allSchemes);
	}

	// Hole
	if (memo.beamHoles != nullptr) {
		GS::ObjectState allHoles;

		GSSize holesCount = BMGetHandleSize (reinterpret_cast<GSHandle>(memo.beamHoles)) / sizeof (API_Beam_Hole);

		for (GSSize idx = 0; idx < holesCount; idx++) {
			GS::ObjectState currentHole;
			currentHole.Add (Beam::holeType, beamHoleTypeNames.Get ((*memo.beamHoles)[idx].holeType));
			currentHole.Add (Beam::holeContourOn, (*memo.beamHoles)[idx].holeContureOn);
			currentHole.Add (Beam::holeId, (*memo.beamHoles)[idx].holeID);
			currentHole.Add (Beam::centerx, (*memo.beamHoles)[idx].centerx);
			currentHole.Add (Beam::centerz, (*memo.beamHoles)[idx].centerz);
			currentHole.Add (Beam::width, (*memo.beamHoles)[idx].width);
			currentHole.Add (Beam::height, (*memo.beamHoles)[idx].height);

			allHoles.Add (GS::String::SPrintf (Beam::HoleName, idx + 1), currentHole);
		}

		os.Add (AssemblySegment::HoleData, allHoles);
	}

	// Floor Plan and Section - Floor Plan Display

	// Show on Stories - Story visibility
	Utility::ExportVisibility (elem.beam.isAutoOnStoryVisibility, elem.beam.visibility, os, ShowOnStories);

	// The display options (Projected, Projected with Overhead, Cut Only, Outlines Only, Overhead All or Symbolic Cut)
	os.Add (Beam::DisplayOptionName, displayOptionNames.Get (elem.beam.displayOption));

	// Uncut projection mode (Symbolic, Projected)
	os.Add (Beam::UncutProjectionModeName, projectionModeNames.Get (elem.beam.uncutProjectionMode));

	// Overhead projection mode (Symbolic, Projected)
	os.Add (Beam::OverheadProjectionModeName, projectionModeNames.Get (elem.beam.overheadProjectionMode));

	// Show projection (To Floor Plan Range, To Absolute Display Limit, Entire Element)
	os.Add (Beam::ViewDepthLimitationName, viewDepthLimitationNames.Get (elem.beam.viewDepthLimitation));

	// Floor Plan and Section - Cut Surfaces

	// The pen index and linetype name of beam contour line
	if (NotOnlyProfileSegment) {
		os.Add (Beam::cutContourLinePen, elem.beam.cutContourLinePen);

		BNZeroMemory (&attrib, sizeof (API_Attribute));
		attrib.header.typeID = API_LinetypeID;
		attrib.header.index = elem.beam.cutContourLineType;

		if (NoError == ACAPI_Attribute_Get (&attrib))
			os.Add (Beam::CutContourLinetypeName, GS::UniString{attrib.header.name});
	}
	
	// Override cut fill pen
	if (elem.beam.penOverride.overrideCutFillPen) {
		os.Add (Beam::OverrideCutFillPenIndex, elem.beam.penOverride.cutFillPen);
	}

	// Override cut fill backgound pen
	if (elem.beam.penOverride.overrideCutFillBackgroundPen) {
		os.Add (Beam::OverrideCutFillBackgroundPenIndex, elem.beam.penOverride.cutFillBackgroundPen);
	}

	// Floor Plan and Section - Outlines

	// Always show outline
	os.Add (Beam::ShowOutlineIndex, beamVisibleLinesNames.Get (elem.beam.showContourLines));

	// The pen index of beam uncut contour line
	os.Add (Beam::UncutLinePenIndex, elem.beam.belowViewLinePen);

	// The linetype name of beam uncut contour line
	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = elem.beam.belowViewLineType;

	if (NoError == ACAPI_Attribute_Get (&attrib))
		os.Add (Beam::UncutLinetypeName, GS::UniString{attrib.header.name});

	// The pen index of beam overhead contour line
	os.Add (Beam::OverheadLinePenIndex, elem.beam.aboveViewLinePen);

	// The linetype name of beam overhead contour line
	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = elem.beam.aboveViewLineType;

	if (NoError == ACAPI_Attribute_Get (&attrib))
		os.Add (Beam::OverheadLinetypeName, GS::UniString{attrib.header.name});

	// The pen index of beam hidden contour line
	os.Add (Beam::HiddenLinePenIndex, elem.beam.hiddenLinePen);

	// The linetype name of beam hidden contour line
	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = elem.beam.hiddenLineType;

	if (NoError == ACAPI_Attribute_Get (&attrib))
		os.Add (Beam::HiddenLinetypeName, GS::UniString{attrib.header.name});

	// Floor Plan and Section - Symbol

	// Always show outline
	os.Add (Beam::ShowReferenceAxisIndex, beamVisibleLinesNames.Get (elem.beam.showReferenceAxis));

	// Reference Axis Pen
	os.Add (Beam::refPen, elem.beam.refPen);

	// Reference Axis Type
	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = elem.beam.refLtype;

	if (NoError == ACAPI_Attribute_Get (&attrib))
		os.Add (Beam::refLtype, GS::UniString{attrib.header.name});
	
	// Floor Plan and Section - Cover Fills
	os.Add (Beam::useCoverFill, elem.beam.useCoverFill);
	if (elem.beam.useCoverFill) {
		os.Add (Beam::useCoverFillFromSurface, elem.beam.useCoverFillFromSurface);
		os.Add (Beam::coverFillForegroundPen, elem.beam.coverFillForegroundPen);
		os.Add (Beam::coverFillBackgroundPen, elem.beam.coverFillBackgroundPen);

		// Cover fill type
		if (!elem.beam.useCoverFillFromSurface) {

			BNZeroMemory (&attrib, sizeof (API_Attribute));
			attrib.header.typeID = API_FilltypeID;
			attrib.header.index = elem.beam.coverFillType;

			if (NoError == ACAPI_Attribute_Get (&attrib))
				os.Add (Beam::coverFillType, GS::UniString{attrib.header.name});
		}

		// Cover Fill Transformation
		Utility::ExportCoverFillTransformation (elem.beam.coverFillOrientationComesFrom3D, elem.beam.coverFillTransformationType, os);

		if ((elem.beam.coverFillTransformationType == API_CoverFillTransformationType_Rotated || elem.beam.coverFillTransformationType == API_CoverFillTransformationType_Distorted) && !elem.beam.coverFillOrientationComesFrom3D) {
			os.Add (Beam::CoverFillTransformationOrigoX, elem.beam.coverFillTransformation.origo.x);
			os.Add (Beam::CoverFillTransformationOrigoY, elem.beam.coverFillTransformation.origo.y);
			os.Add (Beam::CoverFillTransformationXAxisX, elem.beam.coverFillTransformation.xAxis.x);
			os.Add (Beam::CoverFillTransformationXAxisY, elem.beam.coverFillTransformation.xAxis.y);
			os.Add (Beam::CoverFillTransformationYAxisX, elem.beam.coverFillTransformation.yAxis.x);
			os.Add (Beam::CoverFillTransformationYAxisY, elem.beam.coverFillTransformation.yAxis.y);
		}
	}

	return os;
}


GS::String GetBeamData::GetName () const
{
	return GetBeamDataCommandName;
}


GS::ObjectState GetBeamData::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::Array<GS::UniString> ids;
	parameters.Get (ApplicationIds, ids);
	GS::Array<API_Guid> elementGuids = ids.Transform<API_Guid> ([] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); });

	GS::ObjectState result;
	const auto& listAdder = result.AddList<GS::ObjectState> (Beams);
	for (const API_Guid& guid : elementGuids) {
		API_Element element{};
		element.header.guid = guid;

		GSErrCode err = ACAPI_Element_Get (&element);
		if (err != NoError)
			continue;

		API_ElementMemo memo{};
		ACAPI_Element_GetMemo (guid, &memo,
			APIMemoMask_BeamSegment |
			APIMemoMask_AssemblySegmentScheme |
			APIMemoMask_AssemblySegmentCut |
			APIMemoMask_BeamHole);
		if (err != NoError)
			continue;

#ifdef ServerMainVers_2600
		if (element.header.type.typeID != API_BeamID)
#else
		if (element.header.typeID != API_BeamID)
#endif
		{
			continue;
		}

		listAdder (SerializeBeamType (element, memo));
	}

	return result;
}


} // namespace AddOnCommands
