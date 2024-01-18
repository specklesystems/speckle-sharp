#include "CreateBeam.hpp"
#include "APIMigrationHelper.hpp"
#include "CommandHelpers.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Level.hpp"
#include "Objects/Point.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
using namespace FieldNames;

namespace AddOnCommands {


GS::String CreateBeam::GetFieldName () const
{
	return FieldNames::Beams;
}


GS::UniString CreateBeam::GetUndoableCommandName () const
{
	return "CreateSpeckleBeam";
}


GSErrCode CreateBeam::GetElementFromObjectState (const GS::ObjectState& os,
	API_Element& element,
	API_Element& beamMask,
	API_ElementMemo& memo,
	GS::UInt64& memoMask,
	API_SubElement** /*marker*/,
	AttributeManager& /*attributeManager*/,
	LibpartImportManager& /*libpartImportManager*/,
	GS::Array<GS::UniString>& log) const
{
	GSErrCode err = NoError;

	Utility::SetElementType (element.header, API_BeamID);
	err = Utility::GetBaseElementData (element, &memo, nullptr, log);
	if (err != NoError)
		return err;

	err = GetElementBaseFromObjectState (os, element, beamMask);
	if (err != NoError)
		return err;

	// Positioning
	Objects::Point3D startPoint;
	if (os.Contains (Beam::begC)) {
		os.Get (Beam::begC, startPoint);
		element.beam.begC = startPoint.ToAPI_Coord ();
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, begC);
	}

	Objects::Point3D endPoint;
	if (os.Contains (Beam::endC)) {
		os.Get (Beam::endC, endPoint);
		element.beam.endC = endPoint.ToAPI_Coord ();
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, endC);
	}


	if (os.Contains (ElementBase::Level)) {
		GetStoryFromObjectState (os, startPoint.z, element.header.floorInd, element.beam.offset);
	} else {
		Utility::SetStoryLevelAndFloor (startPoint.z, element.header.floorInd, element.beam.offset);
	}
	ACAPI_ELEMENT_MASK_SET (beamMask, API_Elem_Head, floorInd);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, offset);

	if (os.Contains (Beam::level)) {
		os.Get (Beam::level, element.beam.level);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, level);
	}

	if (os.Contains (Beam::isSlanted)) {
		os.Get (Beam::isSlanted, element.beam.isSlanted);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, isSlanted);
	}

	if (os.Contains (Beam::slantAngle)) {
		os.Get (Beam::slantAngle, element.beam.slantAngle);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, slantAngle);
	}

	if (os.Contains (Beam::beamShape)) {
		API_BeamShapeTypeID realBeamShapeType = API_StraightBeam;
		GS::UniString beamShapeName;
		os.Get (Beam::beamShape, beamShapeName);

		GS::Optional<API_BeamShapeTypeID> tmpBeamShapeType = beamShapeTypeNames.FindValue (beamShapeName);
		if (tmpBeamShapeType.HasValue ())
			realBeamShapeType = tmpBeamShapeType.Get ();
		element.beam.beamShape = realBeamShapeType;
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, beamShape);
	}

	if (os.Contains (Beam::sequence)) {
		os.Get (Beam::sequence, element.beam.sequence);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, sequence);
	}

	if (os.Contains (Beam::curveAngle)) {
		os.Get (Beam::curveAngle, element.beam.curveAngle);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, curveAngle);
	}

	if (os.Contains (Beam::verticalCurveHeight)) {
		os.Get (Beam::verticalCurveHeight, element.beam.verticalCurveHeight);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, verticalCurveHeight);
	}

	if (os.Contains (Beam::isFlipped)) {
		os.Get (Beam::isFlipped, element.beam.isFlipped);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, isFlipped);
	}

	// End Cuts
	if (os.Contains (Beam::nCuts)) {
		os.Get (Beam::nCuts, element.beam.nCuts);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, nCuts);
	}

	Utility::CreateAllCutData (os, element.beam.nCuts, element, beamMask, &memo);

	// Reference Axis
	if (os.Contains (Beam::anchorPoint)) {
		os.Get (Beam::anchorPoint, element.beam.anchorPoint);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, anchorPoint);
	}

	if (os.Contains (Beam::offset)) {
		os.Get (Beam::offset, element.beam.offset);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, offset);
	}

	if (os.Contains (Beam::profileAngle)) {
		os.Get (Beam::profileAngle, element.beam.profileAngle);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, profileAngle);
	}

	// Segment
	if (os.Contains (Beam::nSegments)) {
		os.Get (Beam::nSegments, element.beam.nSegments);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, nSegments);
	}

	if (os.Contains (Beam::nProfiles)) {
		os.Get (Beam::nProfiles, element.beam.nProfiles);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, nProfiles);
	}

#pragma region Segment
	GS::ObjectState allSegments;
	if (os.Contains (Beam::segments))
		os.Get (Beam::segments, allSegments);

	if (!allSegments.IsEmpty ()) {
		API_BeamSegmentType defaultBeamSegment;
		if (memo.beamSegments != nullptr) {
			defaultBeamSegment = memo.beamSegments[0];
			memo.beamSegments = (API_BeamSegmentType*) BMAllocatePtr ((element.beam.nSegments) * sizeof (API_BeamSegmentType), ALLOCATE_CLEAR, 0);
		} else {
			return Error;
		}

		memoMask = APIMemoMask_BeamSegment |
			APIMemoMask_AssemblySegmentScheme |
			APIMemoMask_BeamHole |
			APIMemoMask_AssemblySegmentCut;

		for (UInt32 idx = 0; idx < element.beam.nSegments; ++idx) {
			GS::ObjectState currentSegment;
			allSegments.Get (GS::String::SPrintf (AssemblySegment::SegmentName, idx + 1), currentSegment);

			if (!currentSegment.IsEmpty ()) {

				memo.beamSegments[idx] = defaultBeamSegment;
				GS::ObjectState assemblySegment;
				currentSegment.Get (Beam::BeamSegment::segmentData, assemblySegment);
				Utility::CreateOneSegmentData (assemblySegment, memo.beamSegments[idx].assemblySegmentData, beamMask);

				// The left overridden material name - in case of circle or profiled segment the left surface is the extrusion surface, but the import does not work properly in API
				ResetAPIOverriddenAttribute (memo.beamSegments[idx].leftMaterial);
				if (currentSegment.Contains (Beam::BeamSegment::LeftMaterial)) {
					GS::UniString attrName;
					currentSegment.Get (Beam::BeamSegment::LeftMaterial, attrName);

					if (!attrName.IsEmpty ()) {
						API_Attribute attrib;
						BNZeroMemory (&attrib, sizeof (API_Attribute));
						attrib.header.typeID = API_MaterialID;
						CHCopyC (attrName.ToCStr (), attrib.header.name);
						err = ACAPI_Attribute_Get (&attrib);

						if (err == NoError) {
							SetAPIOverriddenAttribute (memo.beamSegments[idx].leftMaterial, attrib.header.index);
							ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamSegmentType, GetAPIOverriddenAttributeIndexField (leftMaterial));
						}
					}
				}
				ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamSegmentType, GetAPIOverriddenAttributeIndexField (leftMaterial));

				// The top overridden material name
				ResetAPIOverriddenAttribute (memo.beamSegments[idx].topMaterial);
				if (currentSegment.Contains (Beam::BeamSegment::TopMaterial)) {
					GS::UniString attrName;
					currentSegment.Get (Beam::BeamSegment::TopMaterial, attrName);

					if (!attrName.IsEmpty ()) {
						API_Attribute attrib;
						BNZeroMemory (&attrib, sizeof (API_Attribute));
						attrib.header.typeID = API_MaterialID;
						CHCopyC (attrName.ToCStr (), attrib.header.name);
						err = ACAPI_Attribute_Get (&attrib);

						if (err == NoError) {
							SetAPIOverriddenAttribute (memo.beamSegments[idx].topMaterial, attrib.header.index);
							ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamSegmentType, GetAPIOverriddenAttributeIndexField (topMaterial));
						}
					}
				}
				ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamSegmentType, GetAPIOverriddenAttributeBoolField (topMaterial));

				// The right overridden material name
				ResetAPIOverriddenAttribute (memo.beamSegments[idx].rightMaterial);
				if (currentSegment.Contains (Beam::BeamSegment::RightMaterial)) {
					GS::UniString attrName;
					currentSegment.Get (Beam::BeamSegment::RightMaterial, attrName);

					if (!attrName.IsEmpty ()) {
						API_Attribute attrib;
						BNZeroMemory (&attrib, sizeof (API_Attribute));
						attrib.header.typeID = API_MaterialID;
						CHCopyC (attrName.ToCStr (), attrib.header.name);
						err = ACAPI_Attribute_Get (&attrib);

						if (err == NoError) {
							SetAPIOverriddenAttribute (memo.beamSegments[idx].rightMaterial, attrib.header.index);
							ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamSegmentType, GetAPIOverriddenAttributeIndexField (rightMaterial));
						}
					}
				}
				ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamSegmentType, GetAPIOverriddenAttributeBoolField (rightMaterial));

				// The bottom overridden material name
				ResetAPIOverriddenAttribute (memo.beamSegments[idx].bottomMaterial);
				if (currentSegment.Contains (Beam::BeamSegment::BottomMaterial)) {
					GS::UniString attrName;
					currentSegment.Get (Beam::BeamSegment::BottomMaterial, attrName);

					if (!attrName.IsEmpty ()) {
						API_Attribute attrib;
						BNZeroMemory (&attrib, sizeof (API_Attribute));
						attrib.header.typeID = API_MaterialID;
						CHCopyC (attrName.ToCStr (), attrib.header.name);
						err = ACAPI_Attribute_Get (&attrib);

						if (err == NoError) {
							SetAPIOverriddenAttribute (memo.beamSegments[idx].bottomMaterial, attrib.header.index);
							ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamSegmentType, GetAPIOverriddenAttributeIndexField (bottomMaterial));
						}
					}
				}
				ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamSegmentType, GetAPIOverriddenAttributeBoolField (bottomMaterial));

				// The ends overridden material name
				ResetAPIOverriddenAttribute (memo.beamSegments[idx].endsMaterial);
				if (currentSegment.Contains (Beam::BeamSegment::EndsMaterial)) {
					GS::UniString attrName;
					currentSegment.Get (Beam::BeamSegment::EndsMaterial, attrName);

					if (!attrName.IsEmpty ()) {
						API_Attribute attrib;
						BNZeroMemory (&attrib, sizeof (API_Attribute));
						attrib.header.typeID = API_MaterialID;
						CHCopyC (attrName.ToCStr (), attrib.header.name);
						err = ACAPI_Attribute_Get (&attrib);

						if (err == NoError) {
							SetAPIOverriddenAttribute (memo.beamSegments[idx].endsMaterial, attrib.header.index);
							ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamSegmentType, GetAPIOverriddenAttributeIndexField (endsMaterial));
						}
					}
				}
				ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamSegmentType, GetAPIOverriddenAttributeBoolField (endsMaterial));

				// The overridden materials are chained
				if (currentSegment.Contains (Beam::BeamSegment::MaterialsChained)) {
					currentSegment.Get (Beam::BeamSegment::MaterialsChained, memo.beamSegments[idx].materialsChained);
					ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamSegmentType, materialsChained);
				}
			}
		}
	}
#pragma endregion

	// Scheme
	if (os.Contains (Beam::nSchemes)) {
		os.Get (Beam::nSchemes, element.beam.nSchemes);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, nSchemes);
	}

	Utility::CreateAllSchemeData (os, element.beam.nSchemes, element, beamMask, &memo);

	// Hole
#pragma region Hole
	GS::ObjectState allHoles;
	UInt32 holesCount = 0;

	if (os.Contains (AssemblySegment::HoleData)) {
		os.Get (AssemblySegment::HoleData, allHoles);
		holesCount = allHoles.GetFieldCount ();
	}

	if (holesCount > 0) {
		memo.beamHoles = reinterpret_cast<API_Beam_Hole**> (BMAllocateHandle (holesCount * sizeof (API_Beam_Hole), ALLOCATE_CLEAR, 0));
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
					(*memo.beamHoles)[idx].holeType = realHoleType;
				}

				if (currentHole.Contains (Beam::holeContourOn))
					currentHole.Get (Beam::holeContourOn, (*memo.beamHoles)[idx].holeContureOn);

				if (currentHole.Contains (Beam::holeId))
					currentHole.Get (Beam::holeId, (*memo.beamHoles)[idx].holeID);

				if (currentHole.Contains (Beam::centerx))
					currentHole.Get (Beam::centerx, (*memo.beamHoles)[idx].centerx);

				if (currentHole.Contains (Beam::centerz))
					currentHole.Get (Beam::centerz, (*memo.beamHoles)[idx].centerz);

				if (currentHole.Contains (Beam::width))
					currentHole.Get (Beam::width, (*memo.beamHoles)[idx].width);

				if (currentHole.Contains (Beam::height))
					currentHole.Get (Beam::height, (*memo.beamHoles)[idx].height);
			}
		}
	}
#pragma endregion

	// Floor Plan and Section - Floor Plan Display

	// Show on Stories - Story visibility
	Utility::CreateVisibility (os, "", element.beam.isAutoOnStoryVisibility, element.beam.visibility);

	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, isAutoOnStoryVisibility);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, visibility.showOnHome);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, visibility.showAllAbove);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, visibility.showAllBelow);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, visibility.showRelAbove);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, visibility.showRelBelow);

	// The display options (Projected, Projected with Overhead, Cut Only, Outlines Only, Overhead All or Symbolic Cut)
	if (os.Contains (Beam::DisplayOptionName)) {
		GS::UniString displayOptionName;
		os.Get (Beam::DisplayOptionName, displayOptionName);

		GS::Optional<API_ElemDisplayOptionsID> type = displayOptionNames.FindValue (displayOptionName);
		if (type.HasValue ()) {
			element.beam.displayOption = type.Get ();
			ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, displayOption);
		}
	}

	// Uncut projection mode (Symbolic, Projected)
	if (os.Contains (Beam::UncutProjectionModeName)) {
		GS::UniString uncutProjectionModeName;
		os.Get (Beam::UncutProjectionModeName, uncutProjectionModeName);

		GS::Optional<API_ElemProjectionModesID> type = projectionModeNames.FindValue (uncutProjectionModeName);
		if (type.HasValue ()) {
			element.beam.uncutProjectionMode = type.Get ();
			ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, uncutProjectionMode);
		}
	}

	// Overhead projection mode (Symbolic, Projected)
	if (os.Contains (Beam::OverheadProjectionModeName)) {
		GS::UniString overheadProjectionModeName;
		os.Get (Beam::OverheadProjectionModeName, overheadProjectionModeName);

		GS::Optional<API_ElemProjectionModesID> type = projectionModeNames.FindValue (overheadProjectionModeName);
		if (type.HasValue ()) {
			element.beam.overheadProjectionMode = type.Get ();
			ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, overheadProjectionMode);
		}
	}

	// Show projection (To Floor Plan Range, To Absolute Display Limit, Entire Element)
	if (os.Contains (Beam::ViewDepthLimitationName)) {
		GS::UniString viewDepthLimitationName;
		os.Get (Beam::ViewDepthLimitationName, viewDepthLimitationName);

		GS::Optional<API_ElemViewDepthLimitationsID> type = viewDepthLimitationNames.FindValue (viewDepthLimitationName);
		if (type.HasValue ()) {
			element.beam.viewDepthLimitation = type.Get ();
			ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, viewDepthLimitation);
		}
	}

	// Floor Plan and Section - Cut Surfaces

	// The pen index of beam contour line
	if (os.Contains (Beam::cutContourLinePen)) {
		os.Get (Beam::cutContourLinePen, element.beam.cutContourLinePen);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, cutContourLinePen);
	}

	// The linetype name of beam contour line
	GS::UniString attributeName;
	if (os.Contains (Beam::CutContourLinetypeName)) {

		os.Get (Beam::CutContourLinetypeName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.beam.cutContourLineType = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, cutContourLineType);
			}
		}
	}

	// Override cut fill and cut fill backgound pens
	if (CommandHelpers::SetCutfillPens(
		os, 
		Beam::OverrideCutFillPenIndex,
		Beam::OverrideCutFillBackgroundPenIndex,
		element.beam,
		beamMask)
		!= NoError)
		return Error;

	// Floor Plan and Section - Outlines

	// Always show outline
	if (os.Contains (Beam::UncutLinePenIndex)) {
		GS::UniString beamVisibleLinesName;
		os.Get (Beam::UncutLinePenIndex, beamVisibleLinesName);

		GS::Optional<API_BeamVisibleLinesID> type = beamVisibleLinesNames.FindValue (beamVisibleLinesName);
		if (type.HasValue ()) {
			element.beam.showContourLines = type.Get ();
			ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, showContourLines);
		}
	}

	// The pen index of beam uncut contour line
	if (os.Contains (Beam::UncutLinePenIndex)) {
		os.Get (Beam::UncutLinePenIndex, element.beam.belowViewLinePen);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, belowViewLinePen);
	}

	// The linetype name of beam uncut contour line
	if (os.Contains (Beam::UncutLinetypeName)) {

		os.Get (Beam::UncutLinetypeName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.beam.belowViewLineType = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, belowViewLineType);
			}
		}
	}

	// The pen index of beam overhead contour line
	if (os.Contains (Beam::OverheadLinePenIndex)) {
		os.Get (Beam::OverheadLinePenIndex, element.beam.aboveViewLinePen);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, aboveViewLinePen);
	}

	// The linetype name of beam overhead contour line
	if (os.Contains (Beam::OverheadLinetypeName)) {

		os.Get (Beam::OverheadLinetypeName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.beam.aboveViewLineType = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, aboveViewLineType);
			}
		}
	}

	// The pen index of beam hidden contour line
	if (os.Contains (Beam::HiddenLinePenIndex)) {
		os.Get (Beam::HiddenLinePenIndex, element.beam.hiddenLinePen);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, hiddenLinePen);
	}

	// The linetype name of beam hidden contour line
	if (os.Contains (Beam::HiddenLinetypeName)) {

		os.Get (Beam::HiddenLinetypeName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.beam.hiddenLineType = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, hiddenLineType);
			}
		}
	}

	// Floor Plan and Section - Symbol

	// Always show outline
	if (os.Contains (Beam::ShowReferenceAxisIndex)) {
		GS::UniString beamVisibleLinesName;
		os.Get (Beam::ShowReferenceAxisIndex, beamVisibleLinesName);

		GS::Optional<API_BeamVisibleLinesID> type = beamVisibleLinesNames.FindValue (beamVisibleLinesName);
		if (type.HasValue ()) {
			element.beam.showReferenceAxis = type.Get ();
			ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, showReferenceAxis);
		}
	}

	// Reference Axis Pen
	if (os.Contains (Beam::refPen)) {
		os.Get (Beam::refPen, element.beam.refPen);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, refPen);
	}

	// Reference Axis Type
	if (os.Contains (Beam::refLtype)) {

		os.Get (Beam::refLtype, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.beam.refLtype = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, refLtype);
			}
		}
	}

	// Floor Plan and Section - Cover Fills
	if (os.Contains (Beam::useCoverFill)) {
		os.Get (Beam::useCoverFill, element.beam.useCoverFill);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, useCoverFill);
	}

	if (os.Contains (Beam::useCoverFillFromSurface)) {
		os.Get (Beam::useCoverFillFromSurface, element.beam.useCoverFillFromSurface);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, useCoverFillFromSurface);
	}

	if (os.Contains (Beam::coverFillForegroundPen)) {
		os.Get (Beam::coverFillForegroundPen, element.beam.coverFillForegroundPen);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, coverFillForegroundPen);
	}

	if (os.Contains (Beam::coverFillBackgroundPen)) {
		os.Get (Beam::coverFillBackgroundPen, element.beam.coverFillBackgroundPen);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, coverFillBackgroundPen);
	}

	// Cover fill type
	if (os.Contains (Beam::coverFillType)) {

		os.Get (Beam::coverFillType, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_FilltypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.beam.coverFillType = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, coverFillType);
			}
		}
	}

	// Cover Fill Transformation
	Utility::CreateCoverFillTransformation (os, element.beam.coverFillOrientationComesFrom3D, element.beam.coverFillTransformationType);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, coverFillOrientationComesFrom3D);
	ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, coverFillTransformationType);

	if (os.Contains (Beam::CoverFillTransformationOrigoX)) {
		os.Get (Beam::CoverFillTransformationOrigoX, element.beam.coverFillTransformation.origo.x);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, coverFillTransformation.origo.x);
	}

	if (os.Contains (Beam::CoverFillTransformationOrigoY)) {
		os.Get (Beam::CoverFillTransformationOrigoY, element.beam.coverFillTransformation.origo.y);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, coverFillTransformation.origo.y);
	}

	if (os.Contains (Beam::CoverFillTransformationXAxisX)) {
		os.Get (Beam::CoverFillTransformationXAxisX, element.beam.coverFillTransformation.xAxis.x);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, coverFillTransformation.xAxis.x);
	}

	if (os.Contains (Beam::CoverFillTransformationXAxisY)) {
		os.Get (Beam::CoverFillTransformationXAxisY, element.beam.coverFillTransformation.xAxis.y);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, coverFillTransformation.xAxis.y);
	}

	if (os.Contains (Beam::CoverFillTransformationYAxisX)) {
		os.Get (Beam::CoverFillTransformationYAxisX, element.beam.coverFillTransformation.yAxis.x);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, coverFillTransformation.yAxis.x);
	}

	if (os.Contains (Beam::CoverFillTransformationYAxisY)) {
		os.Get (Beam::CoverFillTransformationYAxisY, element.beam.coverFillTransformation.yAxis.y);
		ACAPI_ELEMENT_MASK_SET (beamMask, API_BeamType, coverFillTransformation.yAxis.y);
	}

	return NoError;
}


GS::String CreateBeam::GetName () const
{
	return CreateBeamCommandName;
}


} // namespace AddOnCommands
