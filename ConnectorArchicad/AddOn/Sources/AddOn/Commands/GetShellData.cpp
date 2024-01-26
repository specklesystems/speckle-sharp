#include "GetShellData.hpp"
#include "APIMigrationHelper.hpp"
#include "CommandHelpers.hpp"
#include "APIMigrationHelper.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Level.hpp"
#include "Objects/Polyline.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
#include "Vector.hpp"
using namespace FieldNames;

namespace AddOnCommands {



GS::String GetShellData::GetFieldName () const
{
	return FieldNames::Shells;
}


API_ElemTypeID GetShellData::GetElemTypeID() const
{
	return API_ShellID;
}


GS::ErrCode	GetShellData::SerializeElementType (const API_Element& element,
	const API_ElementMemo& memo,
	GS::ObjectState& os) const
{
	// Geometry and positioning
	// The story of the shell
	API_StoryType story = Utility::GetStory (element.shell.head.floorInd);
	os.Add (ElementBase::Level, Objects::Level (story));

	// The shape of the shell
	double level = Utility::GetStoryLevel (element.shell.head.floorInd) + element.shell.shellBase.level;

	// Base plane transformation matrix
	GS::ObjectState transformOs;
	Utility::GetTransform (element.shell.basePlane, transformOs);
	os.Add (Shell::BasePlane, transformOs);

	os.Add (Shell::Flipped, element.shell.isFlipped);

	// Shell contour and hole polygons
	os.Add (Shell::HasContour, element.shell.hasContour);
	os.Add (Shell::NumHoles, element.shell.numHoles);

	API_Attribute attribute;

	UInt32 countShellContour = element.shell.numHoles + (element.shell.hasContour ? 1 : 0);
	if (countShellContour > 0) {
		GS::ObjectState allContourOs;

		for (UInt32 idx = 0; idx < countShellContour; idx++) {

			GS::ObjectState currentContourOs;
			GS::ObjectState allEdgeOs;
			UInt32 countEdge = memo.shellContours[idx].poly.nCoords;
			for (UInt32 iEdge = 0; iEdge < countEdge; iEdge++) {
				GS::ObjectState currentEdgeOs;

				currentEdgeOs.Add (Shell::ShellContourSideTypeName, edgeAngleTypeNames.Get (memo.shellContours[idx].edgeData[iEdge].edgeTrim.sideType));
				currentEdgeOs.Add (Shell::ShellContourSideAngle, memo.shellContours[idx].edgeData[iEdge].edgeTrim.sideAngle);
				if (IsAPIOverriddenAttributeOverridden (memo.shellContours[idx].edgeData[iEdge].sideMaterial)) {
					BNZeroMemory (&attribute, sizeof (API_Attribute));
					attribute.header.typeID = API_MaterialID;
					attribute.header.index = GetAPIOverriddenAttribute (memo.shellContours[idx].edgeData[iEdge].sideMaterial);

					if (NoError == ACAPI_Attribute_Get (&attribute))
						currentEdgeOs.Add (Shell::ShellContourEdgeSideMaterial, GS::UniString{attribute.header.name});
				}
				currentEdgeOs.Add (Shell::ShellContourEdgeTypeName, shellBaseContourEdgeTypeNames.Get (memo.shellContours[idx].edgeData[iEdge].edgeType));
				allEdgeOs.Add (GS::String::SPrintf (Shell::ShellContourEdgeName, iEdge + 1), currentEdgeOs);
			}

			currentContourOs.Add (Shell::ShellContourEdgeData, allEdgeOs);
			currentContourOs.Add (Shell::ShellContourPoly, Objects::ElementShape (memo.shellContours[idx].poly, memo, Objects::ElementShape::MemoShellContour, level, idx));

			GS::ObjectState transformOs;
			Utility::GetTransform (memo.shellContours[idx].plane, transformOs);
			currentContourOs.Add (Shell::ShellContourPlane, transformOs);

			currentContourOs.Add (Shell::ShellContourHeight, memo.shellContours[idx].height);
			currentContourOs.Add (Shell::ShellContourID, memo.shellContours[idx].id);
			allContourOs.Add (GS::String::SPrintf (Shell::ShellContourName, idx + 1), currentContourOs);
		}
		os.Add (Shell::ShellContourData, allContourOs);


	}
	os.Add (Shell::DefaultEdgeType, shellBaseContourEdgeTypeNames.Get (element.shell.defEdgeType));

	GS::ObjectState begShapeEdgeOs;
	GS::ObjectState endShapeEdgeOs;
	GS::ObjectState extrudedEdgeOs1;
	GS::ObjectState extrudedEdgeOs2;
	GS::ObjectState revolvedEdgeOs1;
	GS::ObjectState revolvedEdgeOs2;
	GS::ObjectState ruledEdgeOs1;
	GS::ObjectState ruledEdgeOs2;
	GS::ObjectState transformAxisBaseOs;
	GS::ObjectState transformPlaneOs1;
	GS::ObjectState transformPlaneOs2;

	switch (element.shell.shellClass) {
	case API_ExtrudedShellID:

		os.Add (Shell::ShellClassName, shellClassNames.Get (API_ExtrudedShellID));
		os.Add (Shell::SlantAngle, element.shell.u.extrudedShell.slantAngle);
		os.Add (Shell::ShapePlaneTilt, element.shell.u.extrudedShell.shapePlaneTilt);
		os.Add (Shell::BegPlaneTilt, element.shell.u.extrudedShell.begPlaneTilt);
		os.Add (Shell::EndPlaneTilt, element.shell.u.extrudedShell.endPlaneTilt);
		os.Add (ElementBase::Shape, Objects::ElementShape (element.shell.u.extrudedShell.shellShape, memo, Objects::ElementShape::MemoShellPolygon1, level));
		os.Add (Shell::BegC, Objects::Point3D (element.shell.u.extrudedShell.begC));
		os.Add (Shell::ExtrusionVector, Objects::Vector3D (element.shell.u.extrudedShell.extrusionVector));
		os.Add (Shell::ShapeDirection, Objects::Vector3D (element.shell.u.extrudedShell.shapeDirection));

		// Beg shape edge
		begShapeEdgeOs.Add (Shell::BegShapeEdgeTrimSideType, edgeAngleTypeNames.Get (element.shell.u.extrudedShell.begShapeEdgeData.edgeTrim.sideType));
		begShapeEdgeOs.Add (Shell::BegShapeEdgeTrimSideAngle, element.shell.u.extrudedShell.begShapeEdgeData.edgeTrim.sideAngle);
		if (IsAPIOverriddenAttributeOverridden (element.shell.u.extrudedShell.begShapeEdgeData.sideMaterial)) {
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			attribute.header.index = GetAPIOverriddenAttribute (element.shell.u.extrudedShell.begShapeEdgeData.sideMaterial);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				begShapeEdgeOs.Add (Shell::BegShapeEdgeSideMaterial, GS::UniString{attribute.header.name});
		}
		begShapeEdgeOs.Add (Shell::BegShapeEdgeType, shellBaseContourEdgeTypeNames.Get (element.shell.u.extrudedShell.begShapeEdgeData.edgeType));

		os.Add (Shell::BegShapeEdge, begShapeEdgeOs);

		// End shape edge
		endShapeEdgeOs.Add (Shell::EndShapeEdgeTrimSideType, edgeAngleTypeNames.Get (element.shell.u.extrudedShell.endShapeEdgeData.edgeTrim.sideType));
		endShapeEdgeOs.Add (Shell::EndShapeEdgeTrimSideAngle, element.shell.u.extrudedShell.endShapeEdgeData.edgeTrim.sideAngle);
		if (IsAPIOverriddenAttributeOverridden (element.shell.u.extrudedShell.endShapeEdgeData.sideMaterial)){
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			attribute.header.index = GetAPIOverriddenAttribute (element.shell.u.extrudedShell.endShapeEdgeData.sideMaterial);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				endShapeEdgeOs.Add (Shell::EndShapeEdgeSideMaterial, GS::UniString{attribute.header.name});
		}
		endShapeEdgeOs.Add (Shell::EndShapeEdgeType, shellBaseContourEdgeTypeNames.Get (element.shell.u.extrudedShell.endShapeEdgeData.edgeType));

		os.Add (Shell::EndShapeEdge, endShapeEdgeOs);

		// Side shape edge 1
		extrudedEdgeOs1.Add (Shell::ExtrudedEdgeTrimSideType1, edgeAngleTypeNames.Get (element.shell.u.extrudedShell.extrudedEdgeDatas[0].edgeTrim.sideType));
		extrudedEdgeOs1.Add (Shell::ExtrudedEdgeTrimSideAngle1, element.shell.u.extrudedShell.extrudedEdgeDatas[0].edgeTrim.sideAngle);
		if (IsAPIOverriddenAttributeOverridden (element.shell.u.extrudedShell.extrudedEdgeDatas[0].sideMaterial)) {
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			attribute.header.index = GetAPIOverriddenAttribute (element.shell.u.extrudedShell.extrudedEdgeDatas[0].sideMaterial);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				extrudedEdgeOs1.Add (Shell::ExtrudedEdgeSideMaterial1, GS::UniString{attribute.header.name});
		}
		extrudedEdgeOs1.Add (Shell::ExtrudedEdgeType1, shellBaseContourEdgeTypeNames.Get (element.shell.u.extrudedShell.extrudedEdgeDatas[0].edgeType));

		os.Add (Shell::ExtrudedEdge1, extrudedEdgeOs1);

		// Side shape edge 2
		extrudedEdgeOs2.Add (Shell::ExtrudedEdgeTrimSideType2, edgeAngleTypeNames.Get (element.shell.u.extrudedShell.extrudedEdgeDatas[1].edgeTrim.sideType));
		extrudedEdgeOs2.Add (Shell::ExtrudedEdgeTrimSideAngle2, element.shell.u.extrudedShell.extrudedEdgeDatas[1].edgeTrim.sideAngle);
		if (IsAPIOverriddenAttributeOverridden (element.shell.u.extrudedShell.extrudedEdgeDatas[1].sideMaterial)) {
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			attribute.header.index = GetAPIOverriddenAttribute (element.shell.u.extrudedShell.extrudedEdgeDatas[1].sideMaterial);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				extrudedEdgeOs2.Add (Shell::ExtrudedEdgeSideMaterial2, GS::UniString{attribute.header.name});
		}
		extrudedEdgeOs2.Add (Shell::ExtrudedEdgeType2, shellBaseContourEdgeTypeNames.Get (element.shell.u.extrudedShell.extrudedEdgeDatas[1].edgeType));

		os.Add (Shell::ExtrudedEdge2, extrudedEdgeOs2);

		break;
	case API_RevolvedShellID:

		os.Add (Shell::ShellClassName, shellClassNames.Get (API_RevolvedShellID));
		os.Add (Shell::SlantAngle, element.shell.u.revolvedShell.slantAngle);
		os.Add (Shell::RevolutionAngle, element.shell.u.revolvedShell.revolutionAngle);
		os.Add (Shell::DistortionAngle, element.shell.u.revolvedShell.distortionAngle);
		os.Add (Shell::SegmentedSurfaces, element.shell.u.revolvedShell.segmentedSurfaces);
		os.Add (ElementBase::Shape, Objects::ElementShape (element.shell.u.revolvedShell.shellShape, memo, Objects::ElementShape::MemoShellPolygon1, level));

		Utility::GetTransform (element.shell.u.revolvedShell.axisBase, transformAxisBaseOs);
		os.Add (Shell::AxisBase, transformAxisBaseOs);

		os.Add (Shell::DistortionVector, Objects::Vector3D (element.shell.u.revolvedShell.distortionVector));
		os.Add (Shell::BegAngle, element.shell.u.revolvedShell.begAngle);

		// Beg shape edge
		begShapeEdgeOs.Add (Shell::BegShapeEdgeTrimSideType, edgeAngleTypeNames.Get (element.shell.u.revolvedShell.begShapeEdgeData.edgeTrim.sideType));
		begShapeEdgeOs.Add (Shell::BegShapeEdgeTrimSideAngle, element.shell.u.revolvedShell.begShapeEdgeData.edgeTrim.sideAngle);
		if (IsAPIOverriddenAttributeOverridden (element.shell.u.revolvedShell.begShapeEdgeData.sideMaterial)) {
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			attribute.header.index = GetAPIOverriddenAttribute (element.shell.u.revolvedShell.begShapeEdgeData.sideMaterial);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				begShapeEdgeOs.Add (Shell::BegShapeEdgeSideMaterial, GS::UniString{attribute.header.name});
		}
		begShapeEdgeOs.Add (Shell::BegShapeEdgeType, shellBaseContourEdgeTypeNames.Get (element.shell.u.revolvedShell.begShapeEdgeData.edgeType));

		os.Add (Shell::BegShapeEdge, begShapeEdgeOs);

		// End shape edge
		endShapeEdgeOs.Add (Shell::EndShapeEdgeTrimSideType, edgeAngleTypeNames.Get (element.shell.u.revolvedShell.endShapeEdgeData.edgeTrim.sideType));
		endShapeEdgeOs.Add (Shell::EndShapeEdgeTrimSideAngle, element.shell.u.revolvedShell.endShapeEdgeData.edgeTrim.sideAngle);
		if (IsAPIOverriddenAttributeOverridden (element.shell.u.revolvedShell.endShapeEdgeData.sideMaterial)) {
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			attribute.header.index = GetAPIOverriddenAttribute (element.shell.u.revolvedShell.endShapeEdgeData.sideMaterial);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				endShapeEdgeOs.Add (Shell::EndShapeEdgeSideMaterial, GS::UniString{attribute.header.name});
		}
		endShapeEdgeOs.Add (Shell::EndShapeEdgeType, shellBaseContourEdgeTypeNames.Get (element.shell.u.revolvedShell.endShapeEdgeData.edgeType));

		os.Add (Shell::EndShapeEdge, endShapeEdgeOs);

		// Revolved edge 1
		revolvedEdgeOs1.Add (Shell::RevolvedEdgeTrimSideType1, edgeAngleTypeNames.Get (element.shell.u.revolvedShell.revolvedEdgeDatas[0].edgeTrim.sideType));
		revolvedEdgeOs1.Add (Shell::RevolvedEdgeTrimSideAngle1, element.shell.u.revolvedShell.revolvedEdgeDatas[0].edgeTrim.sideAngle);
		if (IsAPIOverriddenAttributeOverridden (element.shell.u.revolvedShell.revolvedEdgeDatas[0].sideMaterial)) {
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			attribute.header.index = GetAPIOverriddenAttribute (element.shell.u.revolvedShell.revolvedEdgeDatas[0].sideMaterial);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				revolvedEdgeOs1.Add (Shell::RevolvedEdgeSideMaterial1, GS::UniString{attribute.header.name});
		}
		revolvedEdgeOs1.Add (Shell::RevolvedEdgeType1, shellBaseContourEdgeTypeNames.Get (element.shell.u.revolvedShell.revolvedEdgeDatas[0].edgeType));

		os.Add (Shell::RevolvedEdge1, revolvedEdgeOs1);

		// Revolved edge 2
		revolvedEdgeOs2.Add (Shell::RevolvedEdgeTrimSideType2, edgeAngleTypeNames.Get (element.shell.u.revolvedShell.revolvedEdgeDatas[0].edgeTrim.sideType));
		revolvedEdgeOs2.Add (Shell::RevolvedEdgeTrimSideAngle2, element.shell.u.revolvedShell.revolvedEdgeDatas[0].edgeTrim.sideAngle);
		if (IsAPIOverriddenAttributeOverridden (element.shell.u.revolvedShell.revolvedEdgeDatas[0].sideMaterial)) {
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			attribute.header.index = GetAPIOverriddenAttribute (element.shell.u.revolvedShell.revolvedEdgeDatas[0].sideMaterial);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				revolvedEdgeOs2.Add (Shell::RevolvedEdgeSideMaterial2, GS::UniString{attribute.header.name});
		}
		revolvedEdgeOs2.Add (Shell::RevolvedEdgeType2, shellBaseContourEdgeTypeNames.Get (element.shell.u.revolvedShell.revolvedEdgeDatas[0].edgeType));

		os.Add (Shell::RevolvedEdge2, revolvedEdgeOs2);

		break;
	case API_RuledShellID:

		os.Add (Shell::ShellClassName, shellClassNames.Get (API_RuledShellID));
		os.Add (ElementBase::Shape1, Objects::ElementShape (element.shell.u.ruledShell.shellShape1, memo, Objects::ElementShape::MemoShellPolygon1, level));

		Utility::GetTransform (element.shell.u.ruledShell.plane1, transformPlaneOs1);
		os.Add (Shell::Plane1, transformPlaneOs1);

		os.Add (ElementBase::Shape2, Objects::ElementShape (element.shell.u.ruledShell.shellShape2, memo, Objects::ElementShape::MemoShellPolygon2, level));

		Utility::GetTransform (element.shell.u.ruledShell.plane2, transformPlaneOs2);
		os.Add (Shell::Plane2, transformPlaneOs2);

		// Beg shape edge
		begShapeEdgeOs.Add (Shell::BegShapeEdgeTrimSideType, edgeAngleTypeNames.Get (element.shell.u.ruledShell.begShapeEdgeData.edgeTrim.sideType));
		begShapeEdgeOs.Add (Shell::BegShapeEdgeTrimSideAngle, element.shell.u.ruledShell.begShapeEdgeData.edgeTrim.sideAngle);
		if (IsAPIOverriddenAttributeOverridden (element.shell.u.ruledShell.begShapeEdgeData.sideMaterial)) {
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			attribute.header.index = GetAPIOverriddenAttribute (element.shell.u.ruledShell.begShapeEdgeData.sideMaterial);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				begShapeEdgeOs.Add (Shell::BegShapeEdgeSideMaterial, GS::UniString{attribute.header.name});
		}
		begShapeEdgeOs.Add (Shell::BegShapeEdgeType, shellBaseContourEdgeTypeNames.Get (element.shell.u.ruledShell.begShapeEdgeData.edgeType));

		os.Add (Shell::BegShapeEdge, begShapeEdgeOs);

		// End shape edge
		endShapeEdgeOs.Add (Shell::EndShapeEdgeTrimSideType, edgeAngleTypeNames.Get (element.shell.u.ruledShell.endShapeEdgeData.edgeTrim.sideType));
		endShapeEdgeOs.Add (Shell::EndShapeEdgeTrimSideAngle, element.shell.u.ruledShell.endShapeEdgeData.edgeTrim.sideAngle);
		if (IsAPIOverriddenAttributeOverridden (element.shell.u.ruledShell.endShapeEdgeData.sideMaterial)) {
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			attribute.header.index = GetAPIOverriddenAttribute (element.shell.u.ruledShell.endShapeEdgeData.sideMaterial);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				endShapeEdgeOs.Add (Shell::EndShapeEdgeSideMaterial, GS::UniString{attribute.header.name});
		}
		endShapeEdgeOs.Add (Shell::EndShapeEdgeType, shellBaseContourEdgeTypeNames.Get (element.shell.u.ruledShell.endShapeEdgeData.edgeType));

		os.Add (Shell::EndShapeEdge, endShapeEdgeOs);

		// Ruled edge 1
		ruledEdgeOs1.Add (Shell::RuledEdgeTrimSideType1, edgeAngleTypeNames.Get (element.shell.u.ruledShell.ruledEdgeDatas[0].edgeTrim.sideType));
		ruledEdgeOs1.Add (Shell::RuledEdgeTrimSideAngle1, element.shell.u.ruledShell.ruledEdgeDatas[0].edgeTrim.sideAngle);
		if (IsAPIOverriddenAttributeOverridden (element.shell.u.ruledShell.ruledEdgeDatas[0].sideMaterial)) {
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			attribute.header.index = GetAPIOverriddenAttribute (element.shell.u.ruledShell.ruledEdgeDatas[0].sideMaterial);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				ruledEdgeOs1.Add (Shell::RuledEdgeSideMaterial1, GS::UniString{attribute.header.name});
		}
		ruledEdgeOs1.Add (Shell::RuledEdgeType1, shellBaseContourEdgeTypeNames.Get (element.shell.u.ruledShell.ruledEdgeDatas[0].edgeType));

		os.Add (Shell::RuledEdge1, ruledEdgeOs1);

		// Ruled edge 2
		ruledEdgeOs2.Add (Shell::RuledEdgeTrimSideType2, edgeAngleTypeNames.Get (element.shell.u.ruledShell.ruledEdgeDatas[0].edgeTrim.sideType));
		ruledEdgeOs2.Add (Shell::RuledEdgeTrimSideAngle2, element.shell.u.ruledShell.ruledEdgeDatas[0].edgeTrim.sideAngle);
		if (IsAPIOverriddenAttributeOverridden (element.shell.u.ruledShell.ruledEdgeDatas[0].sideMaterial)) {
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			attribute.header.index = GetAPIOverriddenAttribute (element.shell.u.ruledShell.ruledEdgeDatas[0].sideMaterial);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				ruledEdgeOs2.Add (Shell::RuledEdgeSideMaterial2, GS::UniString{attribute.header.name});
		}
		ruledEdgeOs2.Add (Shell::RuledEdgeType2, shellBaseContourEdgeTypeNames.Get (element.shell.u.ruledShell.ruledEdgeDatas[0].edgeType));

		os.Add (Shell::RuledEdge2, ruledEdgeOs2);

		// Morphing rule
		os.Add (Shell::MorphingRuleName, morphingRuleNames.Get (element.shell.u.ruledShell.morphingRule));

		break;
	default:
		break;
	}

	// The thickness of the shell
	os.Add (Shell::Thickness, element.shell.shellBase.thickness);

	// The structure type of the shell (basic or composite)
	os.Add (Shell::Structure, structureTypeNames.Get (element.shell.shellBase.modelElemStructureType));

	// The building material name or composite name of the shell
	switch (element.shell.shellBase.modelElemStructureType) {
	case API_BasicStructure:
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_BuildingMaterialID;
		attribute.header.index = element.shell.shellBase.buildingMaterial;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			os.Add (Shell::BuildingMaterialName, GS::UniString{attribute.header.name});
		break;
	case API_CompositeStructure:
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_CompWallID;
		attribute.header.index = element.shell.shellBase.composite;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			os.Add (Shell::CompositeName, GS::UniString{attribute.header.name});
		break;
	default:
		break;
	}

	// The edge type of the shell
	os.Add (Shell::EdgeAngleType, edgeAngleTypeNames.Get (element.shell.shellBase.edgeTrim.sideType));

	// The edge angle of the shell
	os.Add (Shell::EdgeAngle, element.shell.shellBase.edgeTrim.sideAngle);

	// Floor Plan and Section - Floor Plan Display

	// Show on Stories - Story visibility
	{
		GS::UniString visibilityFillString;
		Utility::GetPredefinedVisibility (false, element.shell.shellBase.visibilityFill, visibilityFillString);

		GS::UniString visibilityContString;
		Utility::GetPredefinedVisibility (false, element.shell.shellBase.visibilityCont, visibilityContString);

		if (visibilityFillString == visibilityContString && visibilityFillString != CustomStoriesValueName) {
			os.Add (ShowOnStories, visibilityContString);
		} else {
			os.Add (ShowOnStories, CustomStoriesValueName);

			Utility::GetVisibility (false, element.shell.shellBase.visibilityFill, os, VisibilityFillData, true);
			Utility::GetVisibility (false, element.shell.shellBase.visibilityCont, os, VisibilityContData, true);
		}
	}

	// The display options (Projected, Projected with Overhead, Cut Only, Outlines Only, Overhead All or Symbolic Cut)
	os.Add (Shell::DisplayOptionName, displayOptionNames.Get (element.shell.shellBase.displayOption));

	// Show projection (To Floor Plan Range, To Absolute Display Limit, Entire Element)
	os.Add (Shell::ViewDepthLimitationName, viewDepthLimitationNames.Get (element.shell.shellBase.viewDepthLimitation));

	// Floor Plan and Section - Cut Surfaces

	// The pen index and linetype name of beam section line
	API_Attribute attrib;
	os.Add (Shell::SectContPen, element.shell.shellBase.sectContPen);

	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = element.shell.shellBase.sectContLtype;

	if (NoError == ACAPI_Attribute_Get (&attrib))
		os.Add (Shell::SectContLtype, GS::UniString{attrib.header.name});

	// Override cut fill pen and background cut fill pen
	CommandHelpers::GetCutfillPens (element.shell.shellBase, os, Shell::CutFillPen, Shell::CutFillBackgroundPen);

	// Outlines

	// The pen index and linetype name of shell contour line
	os.Add (Shell::ContourPen, element.shell.shellBase.pen);

	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = element.shell.shellBase.ltypeInd;

	if (NoError == ACAPI_Attribute_Get (&attrib))
		os.Add (Shell::ContourLineType, GS::UniString{attrib.header.name});

	// The pen index and linetype name of shell above contour line
	os.Add (Shell::OverheadLinePen, element.shell.shellBase.aboveViewLinePen);

	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = element.shell.shellBase.aboveViewLineType;

	if (NoError == ACAPI_Attribute_Get (&attrib))
		os.Add (Shell::OverheadLinetype, GS::UniString{attrib.header.name});

	// Floor Plan and Section - Cover Fills
	os.Add (Shell::UseFloorFill, element.shell.shellBase.useFloorFill);
	if (element.shell.shellBase.useFloorFill) {
		os.Add (Shell::Use3DHatching, element.shell.shellBase.use3DHatching);
		os.Add (Shell::UseFillLocBaseLine, element.shell.shellBase.useFillLocBaseLine);
		os.Add (Shell::UseSlantedFill, element.shell.shellBase.useSlantedFill);
		os.Add (Shell::FloorFillPen, element.shell.shellBase.floorFillPen);
		os.Add (Shell::FloorFillBGPen, element.shell.shellBase.floorFillBGPen);

		// Cover fill type
		if (!element.shell.shellBase.use3DHatching) {

			BNZeroMemory (&attrib, sizeof (API_Attribute));
			attrib.header.typeID = API_FilltypeID;
			attrib.header.index = element.shell.shellBase.floorFillInd;

			if (NoError == ACAPI_Attribute_Get (&attrib))
				os.Add (Shell::FloorFillName, GS::UniString{attrib.header.name});
		}

		// Hatch Orientation
		Utility::GetHatchOrientation (element.shell.shellBase.hatchOrientation.type, os);

		if (element.shell.shellBase.hatchOrientation.type == API_HatchRotated || element.shell.shellBase.hatchOrientation.type == API_HatchDistorted) {
			os.Add (Shell::HatchOrientationOrigoX, element.shell.shellBase.hatchOrientation.origo.x);
			os.Add (Shell::HatchOrientationOrigoY, element.shell.shellBase.hatchOrientation.origo.y);
			os.Add (Shell::HatchOrientationXAxisX, element.shell.shellBase.hatchOrientation.matrix00);
			os.Add (Shell::HatchOrientationXAxisY, element.shell.shellBase.hatchOrientation.matrix10);
			os.Add (Shell::HatchOrientationYAxisX, element.shell.shellBase.hatchOrientation.matrix01);
			os.Add (Shell::HatchOrientationYAxisY, element.shell.shellBase.hatchOrientation.matrix11);
		}
	}

	// Model

	// Overridden materials
	int countOverriddenMaterial = 0;
	if (IsAPIOverriddenAttributeOverridden (element.shell.shellBase.topMat)) {
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_MaterialID;
		attribute.header.index = GetAPIOverriddenAttribute (element.shell.shellBase.topMat);

		if (NoError == ACAPI_Attribute_Get (&attribute))
			countOverriddenMaterial = countOverriddenMaterial + 1;

		os.Add (Shell::TopMat, GS::UniString{attribute.header.name});
	}

	if (IsAPIOverriddenAttributeOverridden (element.shell.shellBase.sidMat)) {
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_MaterialID;
		attribute.header.index = GetAPIOverriddenAttribute (element.shell.shellBase.sidMat);

		if (NoError == ACAPI_Attribute_Get (&attribute))
			countOverriddenMaterial = countOverriddenMaterial + 1;

		os.Add (Shell::SideMat, GS::UniString{attribute.header.name});
	}

	if (IsAPIOverriddenAttributeOverridden (element.shell.shellBase.botMat)) {
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_MaterialID;
		attribute.header.index = GetAPIOverriddenAttribute (element.shell.shellBase.botMat);

		if (NoError == ACAPI_Attribute_Get (&attribute))
			countOverriddenMaterial = countOverriddenMaterial + 1;

		os.Add (Shell::BotMat, GS::UniString{attribute.header.name});
	}

	// The overridden materials are chained
	if (countOverriddenMaterial > 1) {
		os.Add (Shell::MaterialsChained, element.shell.shellBase.materialsChained);
	}

	// Trimming Body (Editable, Contours Down, Pivot Lines Down, Upwards Extrusion or Downwards Extrusion)
	os.Add (Shell::TrimmingBodyName, shellBaseCutBodyTypeNames.Get (element.shell.shellBase.cutBodyType));

	return NoError;
}


GS::String GetShellData::GetName () const
{
	return GetShellDataCommandName
}


}
