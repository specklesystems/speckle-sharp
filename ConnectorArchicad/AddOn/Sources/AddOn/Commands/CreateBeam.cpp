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

#pragma region Segment
	GS::ObjectState allSegments;
	if (os.Contains (PartialObjects::SegmentData))
		os.Get (PartialObjects::SegmentData, allSegments);

	for (UInt32 idx = 0; idx < element.beam.nSegments; ++idx) {
		GS::ObjectState currentSegment;
		allSegments.Get (GS::String::SPrintf (AssemblySegmentData::SegmentName, idx + 1), currentSegment);

		memo->beamSegments[idx] = defaultBeamSegment;
		Utility::CreateOneSegmentData (currentSegment, memo->beamSegments[idx].assemblySegmentData, beamMask);

	}
#pragma endregion

	Utility::CreateAllSchemeData (os, element.beam.nSchemes, element, beamMask, memo);
	Utility::CreateAllCutData (os, element.beam.nCuts, element, beamMask, memo);

#pragma region Hole
	GS::ObjectState allHoles;
	UInt32 holesCount = 0;

	if (os.Contains (PartialObjects::HoleData)) {
		os.Get (PartialObjects::HoleData, allHoles);
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
#pragma endregion

	return NoError;
}

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

