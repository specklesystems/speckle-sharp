#include "CreateBeam.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "DGModule.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"

namespace AddOnCommands
{
static GSErrCode CreateNewBeam (API_Element& beam, API_ElementMemo* memo)
{
	return ACAPI_Element_Create (&beam, memo);
}


static GSErrCode ModifyExistingBeam (API_Element& beam, API_Element& mask, API_ElementMemo* memo)
{
	return ACAPI_Element_Change (&beam, &mask, memo,
		APIMemoMask_BeamSegment |
		APIMemoMask_AssemblySegmentScheme |
		APIMemoMask_BeamHole |
		APIMemoMask_AssemblySegmentCut,
		true);
}


static GSErrCode GetBeamFromObjectState (const GS::ObjectState& os, API_Element& element, API_Element& beamMask, API_ElementMemo* memo)
{
	GSErrCode err = NoError;

	GS::UniString guidString;
	os.Get (ApplicationIdFieldName, guidString);
	element.header.guid = APIGuidFromString (guidString.ToCStr ());
#ifdef ServerMainVers_2600
	element.header.type.typeID = API_BeamID;
#else
	element.header.typeID = API_BeamID;
#endif
	err = Utility::GetBaseElementData (element, memo);
	if (err != NoError)
		return err;

	Objects::Point3D startPoint;
	if (os.Contains (Beam::begC))
		os.Get (Beam::begC, startPoint);
	element.beam.begC = startPoint.ToAPI_Coord ();
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, begC);

	Objects::Point3D endPoint;
	if (os.Contains (Beam::endC))
		os.Get (Beam::endC, endPoint);
	element.beam.endC = endPoint.ToAPI_Coord ();
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, endC);

	if (os.Contains (FloorIndexFieldName)) {
		os.Get (FloorIndexFieldName, element.header.floorInd);
		Utility::SetStoryLevel (startPoint.Z, element.header.floorInd, element.beam.offset);
	} else {
		Utility::SetStoryLevelAndFloor (startPoint.Z, element.header.floorInd, element.beam.offset);
	}
	ACAPI_ELEMENT_MASK_SET (beamMask, API_Elem_Head, floorInd);

	if (os.Contains (Beam::offset))
		os.Get (Beam::offset, element.beam.offset);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, offset);

	if (os.Contains (Beam::level))
		os.Get (Beam::level, element.beam.level);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, level);

	if (os.Contains (Beam::aboveViewLinePen))
		os.Get (Beam::aboveViewLinePen, element.beam.aboveViewLinePen);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, aboveViewLinePen);

	if (os.Contains (Beam::refPen))
		os.Get (Beam::refPen, element.beam.refPen);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, refPen);

	if (os.Contains (Beam::cutContourLinePen))
		os.Get (Beam::cutContourLinePen, element.beam.cutContourLinePen);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, cutContourLinePen);

	if (os.Contains (Beam::sequence))
		os.Get (Beam::sequence, element.beam.sequence);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, sequence);

	if (os.Contains (Beam::holeContourOn))
		os.Get (Beam::holeContourOn, element.beam.holeContureOn);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, holeContureOn);

	if (os.Contains (Beam::isAutoOnStoryVisibility))
		os.Get (Beam::isAutoOnStoryVisibility, element.beam.isAutoOnStoryVisibility);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, isAutoOnStoryVisibility);

	if (os.Contains (Beam::holeType)) {
		API_BHoleTypeID realHoleType = APIBHole_Rectangular;
		GS::UniString holeName;
		os.Get (Beam::holeType, holeName);

		GS::Optional<API_BHoleTypeID> tmpHoleType = beamHoleTypeNames.FindValue (holeName);
		if (tmpHoleType.HasValue ())
			realHoleType = tmpHoleType.Get ();
		element.beam.holeType = realHoleType;
	}
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, holeType);

	if (os.Contains (Beam::curveAngle))
		os.Get (Beam::curveAngle, element.beam.curveAngle);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, curveAngle);

	if (os.Contains (Beam::verticalCurveHeight))
		os.Get (Beam::verticalCurveHeight, element.beam.verticalCurveHeight);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, verticalCurveHeight);

	if (os.Contains (Beam::beamShape)) {
		API_BeamShapeTypeID realBeamShapeType = API_StraightBeam;
		GS::UniString beamShapeName;
		os.Get (Beam::beamShape, beamShapeName);

		GS::Optional<API_BeamShapeTypeID> tmpBeamShapeType = beamShapeTypeNames.FindValue (beamShapeName);
		if (tmpBeamShapeType.HasValue ())
			realBeamShapeType = tmpBeamShapeType.Get ();
		element.beam.beamShape = realBeamShapeType;
	}
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, beamShape);

	if (os.Contains (Beam::hiddenLinePen))
		os.Get (Beam::hiddenLinePen, element.beam.hiddenLinePen);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, hiddenLinePen);

	if (os.Contains (Beam::anchorPoint))
		os.Get (Beam::anchorPoint, element.beam.anchorPoint);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, anchorPoint);

	if (os.Contains (Beam::belowViewLinePen))
		os.Get (Beam::belowViewLinePen, element.beam.belowViewLinePen);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, belowViewLinePen);

	if (os.Contains (Beam::isFlipped))
		os.Get (Beam::isFlipped, element.beam.isFlipped);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, isFlipped);

	if (os.Contains (Beam::isSlanted))
		os.Get (Beam::isSlanted, element.beam.isSlanted);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, isSlanted);

	if (os.Contains (Beam::slantAngle))
		os.Get (Beam::slantAngle, element.beam.slantAngle);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, slantAngle);

	if (os.Contains (Beam::profileAngle))
		os.Get (Beam::profileAngle, element.beam.profileAngle);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, profileAngle);

	if (os.Contains (Beam::nSegments))
		os.Get (Beam::nSegments, element.beam.nSegments);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, nSegments);

	if (os.Contains (Beam::nCuts))
		os.Get (Beam::nCuts, element.beam.nCuts);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, nCuts);

	if (os.Contains (Beam::nSchemes))
		os.Get (Beam::nSchemes, element.beam.nSchemes);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, nSchemes);

	if (os.Contains (Beam::nProfiles))
		os.Get (Beam::nProfiles, element.beam.nProfiles);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, nProfiles);

	if (os.Contains (Beam::useCoverFill))
		os.Get (Beam::useCoverFill, element.beam.useCoverFill);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, useCoverFill);

	if (os.Contains (Beam::useCoverFillFromSurface))
		os.Get (Beam::useCoverFillFromSurface, element.beam.useCoverFillFromSurface);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, useCoverFillFromSurface);

	if (os.Contains (Beam::coverFillOrientationComesFrom3D))
		os.Get (Beam::coverFillOrientationComesFrom3D, element.beam.coverFillOrientationComesFrom3D);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, coverFillOrientationComesFrom3D);

	if (os.Contains (Beam::coverFillForegroundPen))
		os.Get (Beam::coverFillForegroundPen, element.beam.coverFillForegroundPen);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, coverFillForegroundPen);

	if (os.Contains (Beam::coverFillBackgroundPen))
		os.Get (Beam::coverFillBackgroundPen, element.beam.coverFillBackgroundPen);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, coverFillBackgroundPen);

	API_BeamSegmentType defaultBeamSegment;
	if (memo->beamSegments != nullptr) {
		defaultBeamSegment = memo->beamSegments[0];
		memo->beamSegments = (API_BeamSegmentType*) BMAllocatePtr ((element.beam.nSegments) * sizeof (API_BeamSegmentType), ALLOCATE_CLEAR, 0);
	} else {
		return Error;
	}

	API_AssemblySegmentSchemeData defaultBeamSegmentScheme;
	if (memo->assemblySegmentSchemes != nullptr) {
		defaultBeamSegmentScheme = memo->assemblySegmentSchemes[0];
		memo->assemblySegmentSchemes = (API_AssemblySegmentSchemeData*) BMAllocatePtr ((element.beam.nSchemes) * sizeof (API_AssemblySegmentSchemeData), ALLOCATE_CLEAR, 0);
	} else {
		return Error;
	}

	API_AssemblySegmentCutData defaultBeamSegmentCut;
	if (memo->assemblySegmentCuts != nullptr) {
		defaultBeamSegmentCut = memo->assemblySegmentCuts[0];
		memo->assemblySegmentCuts = (API_AssemblySegmentCutData*) BMAllocatePtr ((element.beam.nCuts) * sizeof (API_AssemblySegmentCutData), ALLOCATE_CLEAR, 0);
	} else {
		return Error;
	}

#pragma region Segment
	GS::ObjectState allSegments;
	if (os.Contains (Beam::segmentData))
		os.Get (Beam::segmentData, allSegments);

	for (UInt32 idx = 0; idx < element.beam.nSegments; ++idx) {
		GS::ObjectState currentSegment;
		allSegments.Get (GS::String::SPrintf (Beam::BeamSegmentName, idx + 1), currentSegment);

		memo->beamSegments[idx] = defaultBeamSegment;
		if (!currentSegment.IsEmpty ()) {

			if (currentSegment.Contains (Beam::circleBased))
				currentSegment.Get (Beam::circleBased, memo->beamSegments[idx].assemblySegmentData.circleBased);
			ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamSegmentType, assemblySegmentData.circleBased);

			if (currentSegment.Contains (Beam::nominalHeight))
				currentSegment.Get (Beam::nominalHeight, memo->beamSegments[idx].assemblySegmentData.nominalHeight);
			ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamSegmentType, assemblySegmentData.nominalHeight);

			if (currentSegment.Contains (Beam::nominalWidth))
				currentSegment.Get (Beam::nominalWidth, memo->beamSegments[idx].assemblySegmentData.nominalWidth);
			ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamSegmentType, assemblySegmentData.nominalWidth);

			if (currentSegment.Contains (Beam::isWidthAndHeightLinked))
				currentSegment.Get (Beam::isWidthAndHeightLinked, memo->beamSegments[idx].assemblySegmentData.isWidthAndHeightLinked);
			ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamSegmentType, assemblySegmentData.isWidthAndHeightLinked);

			if (currentSegment.Contains (Beam::isHomogeneous))
				currentSegment.Get (Beam::isHomogeneous, memo->beamSegments[idx].assemblySegmentData.isHomogeneous);
			ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamSegmentType, assemblySegmentData.isHomogeneous);

			if (currentSegment.Contains (Beam::endWidth))
				currentSegment.Get (Beam::endWidth, memo->beamSegments[idx].assemblySegmentData.endWidth);
			ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamSegmentType, assemblySegmentData.endWidth);

			if (currentSegment.Contains (Beam::endHeight))
				currentSegment.Get (Beam::endHeight, memo->beamSegments[idx].assemblySegmentData.endHeight);
			ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamSegmentType, assemblySegmentData.endHeight);

			if (currentSegment.Contains (Beam::isEndWidthAndHeightLinked))
				currentSegment.Get (Beam::isEndWidthAndHeightLinked, memo->beamSegments[idx].assemblySegmentData.isEndWidthAndHeightLinked);
			ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamSegmentType, assemblySegmentData.isEndWidthAndHeightLinked);

			if (currentSegment.Contains (Beam::modelElemStructureType)) {
				API_ModelElemStructureType realStructureType = API_BasicStructure;
				GS::UniString structureName;
				currentSegment.Get (Beam::modelElemStructureType, structureName);

				GS::Optional<API_ModelElemStructureType> tmpStructureType = structureTypeNames.FindValue (structureName);
				if (tmpStructureType.HasValue ())
					realStructureType = tmpStructureType.Get ();
				memo->beamSegments[idx].assemblySegmentData.modelElemStructureType = realStructureType;
			}
			ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamSegmentType, assemblySegmentData.modelElemStructureType);

			if (currentSegment.Contains (Beam::profileAttrName)) {
				GS::UniString attrName;
				currentSegment.Get (Beam::profileAttrName, attrName);

				if (!attrName.IsEmpty ()) {
					API_Attribute attrib;
					BNZeroMemory (&attrib, sizeof (API_Attribute));
					attrib.header.typeID = API_ProfileID;
					CHCopyC (attrName.ToCStr (), attrib.header.name);
					err = ACAPI_Attribute_Get (&attrib);

					if (err == NoError)
						memo->beamSegments[idx].assemblySegmentData.profileAttr = attrib.header.index;
				}
			}

			if (currentSegment.Contains (Beam::buildingMaterial)) {
				GS::UniString attrName;
				currentSegment.Get (Beam::buildingMaterial, attrName);

				if (!attrName.IsEmpty ()) {
					API_Attribute attrib;
					BNZeroMemory (&attrib, sizeof (API_Attribute));
					attrib.header.typeID = API_BuildingMaterialID;
					CHCopyC (attrName.ToCStr (), attrib.header.name);
					err = ACAPI_Attribute_Get (&attrib);

					if (err == NoError)
						memo->beamSegments[idx].assemblySegmentData.buildingMaterial = attrib.header.index;
				}
			}
		}
	}
#pragma endregion

#pragma region Scheme
	GS::ObjectState allSchemes;
	if (os.Contains (Beam::schemeData))
		os.Get (Beam::schemeData, allSchemes);

	for (UInt32 idx = 0; idx < element.beam.nSchemes; ++idx) {
		if (!allSchemes.IsEmpty ()) {
			GS::ObjectState currentScheme;
			allSchemes.Get (GS::String::SPrintf (Beam::SchemeName, idx + 1), currentScheme);

			memo->assemblySegmentSchemes[idx] = defaultBeamSegmentScheme;
			if (!currentScheme.IsEmpty ()) {

				if (currentScheme.Contains (Beam::lengthType)) {
					API_AssemblySegmentLengthTypeID lengthType = APIAssemblySegment_Fixed;
					GS::UniString lengthTypeName;
					currentScheme.Get (Beam::lengthType, lengthTypeName);

					GS::Optional<API_AssemblySegmentLengthTypeID> type = segmentLengthTypeNames.FindValue (lengthTypeName);
					if (type.HasValue ())
						lengthType = type.Get ();
					memo->assemblySegmentSchemes[idx].lengthType = lengthType;

					if (lengthType == APIAssemblySegment_Fixed && currentScheme.Contains (Beam::fixedLength)) {
						currentScheme.Get (Beam::fixedLength, memo->assemblySegmentSchemes[idx].fixedLength);
						memo->assemblySegmentSchemes[idx].lengthProportion = 0.0;
					} else if (lengthType == APIAssemblySegment_Proportional && currentScheme.Contains (Beam::lengthProportion)) {
						currentScheme.Get (Beam::lengthProportion, memo->assemblySegmentSchemes[idx].lengthProportion);
						memo->assemblySegmentSchemes[idx].fixedLength = 0.0;
					}
				}
			}
		}
	}
#pragma endregion

#pragma region Cut
	GS::ObjectState allCuts;
	if (os.Contains (Beam::cutData))
		os.Get (Beam::cutData, allCuts);

	for (UInt32 idx = 0; idx < element.beam.nCuts; ++idx) {
		GS::ObjectState currentCut;
		allCuts.Get (GS::String::SPrintf (Beam::CutName, idx + 1), currentCut);

		memo->assemblySegmentCuts[idx] = defaultBeamSegmentCut;
		if (!currentCut.IsEmpty ()) {

			if (currentCut.Contains (Beam::cutType)) {
				API_AssemblySegmentCutTypeID realCutType = APIAssemblySegmentCut_Vertical;
				GS::UniString structureName;
				currentCut.Get (Beam::cutType, structureName);

				GS::Optional<API_AssemblySegmentCutTypeID> tmpCutType = assemblySegmentCutTypeNames.FindValue (structureName);
				if (tmpCutType.HasValue ())
					realCutType = tmpCutType.Get ();
				memo->assemblySegmentCuts[idx].cutType = realCutType;
			}
			if (currentCut.Contains (Beam::customAngle)) {
				currentCut.Get (Beam::customAngle, memo->assemblySegmentCuts[idx].customAngle);
			}
		}
	}
#pragma endregion

#pragma region Hole
	GS::ObjectState allHoles;
	UInt32 holesCount = 0;

	if (os.Contains (Beam::holeData)) {
		os.Get (Beam::holeData, allHoles);
		holesCount = allHoles.GetFieldCount ();
	}

	if (holesCount > 0) {
		memo->beamHoles = reinterpret_cast<API_Beam_Hole**> (BMAllocateHandle (holesCount * sizeof (API_Beam_Hole), ALLOCATE_CLEAR, 0));
		for (UInt32 idx = 0; idx < holesCount; ++idx) {
			GS::ObjectState currentHole;
			allHoles.Get (GS::String::SPrintf (Beam::HoleName, idx + 1), currentHole);

			if (!currentHole.IsEmpty ()) {
				if (currentHole.Contains (Beam::holeType)) {
					API_BHoleTypeID realHoleType = APIBHole_Rectangular;
					GS::UniString holeName;
					currentHole.Get (Beam::holeType, holeName);

					GS::Optional<API_BHoleTypeID> tmpHoleType = beamHoleTypeNames.FindValue (holeName);
					if (tmpHoleType.HasValue ())
						realHoleType = tmpHoleType.Get ();
					(*memo->beamHoles)[idx].holeType = realHoleType;
				}

				if (currentHole.Contains (Beam::holeContourOn))
					currentHole.Get (Beam::holeContourOn, (*memo->beamHoles)[idx].holeContureOn);

				if (currentHole.Contains (Beam::holeId))
					currentHole.Get (Beam::holeId, (*memo->beamHoles)[idx].holeID);

				if (currentHole.Contains (Beam::centerx))
					currentHole.Get (Beam::centerx, (*memo->beamHoles)[idx].centerx);

				if (currentHole.Contains (Beam::centerz))
					currentHole.Get (Beam::centerz, (*memo->beamHoles)[idx].centerz);

				if (currentHole.Contains (Beam::width))
					currentHole.Get (Beam::width, (*memo->beamHoles)[idx].width);

				if (currentHole.Contains (Beam::height))
					currentHole.Get (Beam::height, (*memo->beamHoles)[idx].height);
			}
		}
	}

	return NoError;
}
#pragma endregion

GS::String CreateBeam::GetName () const
{
	return CreateBeamCommandName;
}

GS::ObjectState CreateBeam::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::ObjectState result;

	GS::Array<GS::ObjectState> beams;
	parameters.Get (BeamsFieldName, beams);

	const auto& listAdder = result.AddList<GS::UniString> (ApplicationIdsFieldName);

	ACAPI_CallUndoableCommand ("CreateSpeckleBeam", [&] () -> GSErrCode {
		for (const GS::ObjectState& beamOs : beams) {
			API_Element beam{};
			API_Element beamMask{};
			API_ElementMemo memo{}; // Neccessary for beam

			GSErrCode err = GetBeamFromObjectState (beamOs, beam, beamMask, &memo);
			if (err != NoError)
				continue;

			bool beamExists = Utility::ElementExists (beam.header.guid);
			if (beamExists) {
				err = ModifyExistingBeam (beam, beamMask, &memo);
			} else {
				err = CreateNewBeam (beam, &memo);
			}

			if (err == NoError) {
				GS::UniString elemId = APIGuidToString (beam.header.guid);
				listAdder (elemId);
			}

			ACAPI_DisposeElemMemoHdls (&memo);
		}
		return NoError;
		});

	return result;
}
}

