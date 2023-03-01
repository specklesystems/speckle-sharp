#include "GetBeamData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"

namespace AddOnCommands
{

static GS::ObjectState SerializeBeamType (const API_Element& elem, const API_ElementMemo& memo)
{
	GS::ObjectState os;

	os.Add (ApplicationIdFieldName, APIGuidToString (elem.beam.head.guid));
	os.Add (FloorIndexFieldName, elem.beam.head.floorInd);

	double z = Utility::GetStoryLevel (elem.beam.head.floorInd) + elem.beam.level;
	os.Add (Beam::begC, Objects::Point3D (elem.beam.begC.x, elem.beam.begC.y, z));
	os.Add (Beam::endC, Objects::Point3D (elem.beam.endC.x, elem.beam.endC.y, z));

	os.Add (Beam::offset, elem.beam.offset);
	os.Add (Beam::level, elem.beam.level);
	os.Add (Beam::aboveViewLinePen, elem.beam.aboveViewLinePen);
	os.Add (Beam::refPen, elem.beam.refPen);
	os.Add (Beam::cutContourLinePen, elem.beam.cutContourLinePen);
	os.Add (Beam::sequence, elem.beam.sequence);
	os.Add (Beam::isAutoOnStoryVisibility, elem.beam.isAutoOnStoryVisibility);
	os.Add (Beam::curveAngle, elem.beam.curveAngle);
	os.Add (Beam::verticalCurveHeight, elem.beam.verticalCurveHeight);
	os.Add (Beam::beamShape, beamShapeTypeNames.Get (elem.beam.beamShape));
	os.Add (Beam::hiddenLinePen, elem.beam.hiddenLinePen);
	os.Add (Beam::anchorPoint, elem.beam.anchorPoint);
	os.Add (Beam::belowViewLinePen, elem.beam.belowViewLinePen);
	os.Add (Beam::isFlipped, elem.beam.isFlipped);
	os.Add (Beam::isSlanted, elem.beam.isSlanted);
	os.Add (Beam::slantAngle, elem.beam.slantAngle);
	os.Add (Beam::profileAngle, elem.beam.profileAngle);
	os.Add (Beam::nSegments, elem.beam.nSegments);
	os.Add (Beam::nCuts, elem.beam.nCuts);
	os.Add (Beam::nSchemes, elem.beam.nSchemes);
	os.Add (Beam::nProfiles, elem.beam.nProfiles);
	os.Add (Beam::useCoverFill, elem.beam.useCoverFill);
	os.Add (Beam::useCoverFillFromSurface, elem.beam.useCoverFillFromSurface);
	os.Add (Beam::coverFillOrientationComesFrom3D, elem.beam.coverFillOrientationComesFrom3D);
	os.Add (Beam::coverFillForegroundPen, elem.beam.coverFillForegroundPen);

	// Segment
	if (memo.beamSegments != nullptr) {
		GS::ObjectState allSegments;

		GSSize segmentsCount = BMGetPtrSize (reinterpret_cast<GSPtr>(memo.beamSegments)) / sizeof (API_BeamSegmentType);
		DBASSERT (segmentsCount == elem.beam.nSegments);

		for (GSSize idx = 0; idx < segmentsCount; ++idx) {
			GS::ObjectState currentSegment;
			Utility::GetSegmentData (memo.beamSegments[idx].assemblySegmentData, currentSegment);

			allSegments.Add (GS::String::SPrintf (AssemblySegmentData::SegmentName, idx + 1), currentSegment);
		}

		os.Add (PartialObjects::SegmentData, allSegments);
	}

	// Scheme
	if (memo.assemblySegmentSchemes != nullptr) {
		GS::ObjectState allSchemes;
		Utility::GetAllSchemeData (memo.assemblySegmentSchemes, allSchemes);
		os.Add (PartialObjects::SchemeData, allSchemes);
	}

	// Cut
	if (memo.assemblySegmentCuts != nullptr) {
		GS::ObjectState allCuts;
		Utility::GetAllCutData (memo.assemblySegmentCuts, allCuts);
		os.Add (PartialObjects::CutData, allCuts);
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

		os.Add (PartialObjects::HoleData, allHoles);
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
	parameters.Get (ApplicationIdsFieldName, ids);
	GS::Array<API_Guid> elementGuids = ids.Transform<API_Guid> ([] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); });

	GS::ObjectState result;
	const auto& listAdder = result.AddList<GS::ObjectState> (BeamsFieldName);
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
