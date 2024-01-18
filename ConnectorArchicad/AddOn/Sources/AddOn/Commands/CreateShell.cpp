#include "CreateShell.hpp"
#include "APIMigrationHelper.hpp"
#include "CommandHelpers.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Polyline.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
#include "AngleData.h"
#include "Vector.hpp"
using namespace FieldNames;


namespace AddOnCommands {


GS::String CreateShell::GetFieldName () const
{
	return FieldNames::Shells;
}


GS::UniString CreateShell::GetUndoableCommandName () const
{
	return "CreateSpeckleShell";
}


GSErrCode CreateShell::GetElementFromObjectState (const GS::ObjectState& os,
	API_Element& element,
	API_Element& elementMask,
	API_ElementMemo& memo,
	GS::UInt64& memoMask,
	API_SubElement** /*marker = nullptr*/,
	AttributeManager& /*attributeManager*/,
	LibpartImportManager& /*libpartImportManager*/,
	GS::Array<GS::UniString>& log) const
{
	GSErrCode err = NoError;

	Utility::SetElementType (element.header, API_ShellID);
	err = Utility::GetBaseElementData (element, &memo, nullptr, log);
	if (err != NoError)
		return err;

	err = GetElementBaseFromObjectState (os, element, elementMask);
	if (err != NoError)
		return err;

	// The structure of the shell
	if (os.Contains (Shell::ShellClassName)) {
		GS::UniString shellClassName;
		os.Get (Shell::ShellClassName, shellClassName);

		GS::Optional<API_ShellClassID> type = shellClassNames.FindValue (shellClassName);
		if (type.HasValue ()) {
			element.shell.shellClass = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellClass);
		}
	}

	// Base plane transformation matrix
	if (os.Contains (Shell::BasePlane)) {
		GS::ObjectState transformOs;
		os.Get (Shell::BasePlane, transformOs);

		Utility::CreateTransform (transformOs, element.shell.basePlane);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, basePlane);
	}

	if (os.Contains (Shell::Flipped)) {
		os.Get (Shell::Flipped, element.shell.isFlipped);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, isFlipped);
	}

	if (os.Contains (Shell::ShellContourData)) {
		os.Get (Shell::HasContour, element.shell.hasContour);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, hasContour);

		os.Get (Shell::NumHoles, element.shell.numHoles);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, numHoles);

		UInt32 countShellContour = element.shell.numHoles + (element.shell.hasContour ? 1 : 0);

		if (countShellContour > 0) {

			memo.shellContours = (API_ShellContourData*) BMAllocatePtr (countShellContour * sizeof (API_ShellContourData), ALLOCATE_CLEAR, 0);

			GS::ObjectState shellContours;
			os.Get (Shell::ShellContourData, shellContours);

			for (UInt32 idx = 0; idx < countShellContour; idx++) {

				GS::ObjectState shellContour;
				shellContours.Get (GS::String::SPrintf (Shell::ShellContourName, idx + 1), shellContour);

				Objects::ElementShape shellContourPoly;
				shellContour.Get (Shell::ShellContourPoly, shellContourPoly);

				memo.shellContours[idx].poly.nSubPolys = shellContourPoly.SubpolyCount ();
				memo.shellContours[idx].poly.nCoords = shellContourPoly.VertexCount ();
				memo.shellContours[idx].poly.nArcs = shellContourPoly.ArcCount ();

				shellContourPoly.SetToMemo (memo, Objects::ElementShape::MemoShellContour, idx);

				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellContourData, poly.nSubPolys);
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellContourData, poly.nCoords);
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellContourData, poly.nArcs);

				if (shellContour.Contains (Shell::ShellContourPlane)) {
					GS::ObjectState transformOs;
					shellContour.Get (Shell::ShellContourPlane, transformOs);

					Utility::CreateTransform (transformOs, memo.shellContours[idx].plane);
					ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellContourData, plane);
				}

				shellContour.Get (Shell::ShellContourHeight, memo.shellContours[idx].height);
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellContourData, height);

				shellContour.Get (Shell::ShellContourHeight, memo.shellContours[idx].id);
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellContourData, id);
			}
		}
	}

	// Default edge type
	;	if (os.Contains (Shell::DefaultEdgeType)) {

		GS::UniString defaultEdgeTypeName;
		os.Get (Shell::DefaultEdgeType, defaultEdgeTypeName);

		GS::Optional<API_ShellBaseContourEdgeTypeID> type = shellBaseContourEdgeTypeNames.FindValue (defaultEdgeTypeName);
		if (type.HasValue ()) {
			element.shell.defEdgeType = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, defEdgeType);
		}
	}

	// Geometry and positioning
	memoMask = APIMemoMask_Polygon | APIMemoMask_SideMaterials | APIMemoMask_EdgeTrims;

	// The shape of the shell
	GS::UniString attributeName;
	Objects::ElementShape shellShape;
	Objects::ElementShape shellShape1;
	Objects::ElementShape shellShape2;
	Objects::Point3D startPoint;
	Objects::Vector3D extrusionVector;
	Objects::Vector3D shapeDirection;
	Objects::Vector3D distortionVector;
	GS::ObjectState transformAxisBaseOs;
	GS::ObjectState transformPlaneOs1;
	GS::ObjectState transformPlaneOs2;

	switch (element.shell.shellClass) {
	case API_ExtrudedShellID:

		if (os.Contains (Shell::SlantAngle)) {
			os.Get (Shell::SlantAngle, element.shell.u.extrudedShell.slantAngle);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.slantAngle);
		}

		if (os.Contains (Shell::ShapePlaneTilt)) {
			os.Get (Shell::ShapePlaneTilt, element.shell.u.extrudedShell.shapePlaneTilt);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.shapePlaneTilt);
		}

		if (os.Contains (Shell::BegPlaneTilt)) {
			os.Get (Shell::BegPlaneTilt, element.shell.u.extrudedShell.begPlaneTilt);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.begPlaneTilt);
		}

		if (os.Contains (Shell::EndPlaneTilt)) {
			os.Get (Shell::EndPlaneTilt, element.shell.u.extrudedShell.endPlaneTilt);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.endPlaneTilt);
		}

		if (os.Contains (ElementBase::Shape)) {
			os.Get (ElementBase::Shape, shellShape);
			element.shell.u.extrudedShell.shellShape.nSubPolys = shellShape.SubpolyCount ();
			element.shell.u.extrudedShell.shellShape.nCoords = shellShape.VertexCount ();
			element.shell.u.extrudedShell.shellShape.nArcs = shellShape.ArcCount ();

			shellShape.SetToMemo (memo, Objects::ElementShape::MemoShellPolygon1);

			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.shellShape.nSubPolys);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.shellShape.nCoords);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.shellShape.nArcs);
		}

		if (os.Contains (Shell::BegC)) {
			os.Get (Shell::BegC, startPoint);
			element.shell.u.extrudedShell.begC = startPoint.ToAPI_Coord3D ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.begC);
		}

		if (os.Contains (Shell::ExtrusionVector)) {
			os.Get (Shell::ExtrusionVector, extrusionVector);
			element.shell.u.extrudedShell.extrusionVector = extrusionVector.ToAPI_Vector3D ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.extrusionVector);
		}

		if (os.Contains (Shell::ShapeDirection)) {
			os.Get (Shell::ShapeDirection, shapeDirection);
			element.shell.u.extrudedShell.shapeDirection = shapeDirection.ToAPI_Vector ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.shapeDirection);
		}

		// Beg shape edge
		if (os.Contains (Shell::BegShapeEdge)) {
			GS::ObjectState begShapeEdgeOs;
			os.Get (Shell::BegShapeEdge, begShapeEdgeOs);

			// Edge trim side type
			GS::UniString begShapeEdgeTrimSideTypeName;
			begShapeEdgeOs.Get (Shell::BegShapeEdgeTrimSideType, begShapeEdgeTrimSideTypeName);

			GS::Optional<API_EdgeTrimID> type = edgeAngleTypeNames.FindValue (begShapeEdgeTrimSideTypeName);
			if (type.HasValue ()) {
				element.shell.u.extrudedShell.begShapeEdgeData.edgeTrim.sideType = type.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.begShapeEdgeData.edgeTrim.sideType);
			}

			// Edge trim side angle
			begShapeEdgeOs.Get (Shell::BegShapeEdgeTrimSideAngle, element.shell.u.extrudedShell.begShapeEdgeData.edgeTrim.sideAngle);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.begShapeEdgeData.edgeTrim.sideAngle);

			// Overridden side material
			if (begShapeEdgeOs.Contains (Shell::BegShapeEdgeSideMaterial)) {

				begShapeEdgeOs.Get (Shell::BegShapeEdgeSideMaterial, attributeName);

				if (!attributeName.IsEmpty ()) {
					API_Attribute attribute;
					BNZeroMemory (&attribute, sizeof (API_Attribute));
					attribute.header.typeID = API_MaterialID;
					CHCopyC (attributeName.ToCStr (), attribute.header.name);

					if (NoError == ACAPI_Attribute_Get (&attribute)) {
						SetAPIOverriddenAttribute (element.shell.u.extrudedShell.begShapeEdgeData.sideMaterial, attribute.header.index);
						ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeIndexField (u.extrudedShell.begShapeEdgeData.sideMaterial));
					}
				}
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeBoolField (u.extrudedShell.begShapeEdgeData.sideMaterial));
			}

			// Edge type
			GS::UniString begShapeEdgeTypeName;
			begShapeEdgeOs.Get (Shell::BegShapeEdgeType, begShapeEdgeTypeName);

			GS::Optional<API_ShellBaseContourEdgeTypeID> type2 = shellBaseContourEdgeTypeNames.FindValue (begShapeEdgeTypeName);
			if (type2.HasValue ()) {
				element.shell.u.extrudedShell.begShapeEdgeData.edgeType = type2.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.begShapeEdgeData.edgeType);
			}
		}

		// End shape edge
		if (os.Contains (Shell::EndShapeEdge)) {
			GS::ObjectState endShapeEdgeOs;
			os.Get (Shell::EndShapeEdge, endShapeEdgeOs);

			// Edge trim side type
			GS::UniString endShapeEdgeTrimSideTypeName;
			endShapeEdgeOs.Get (Shell::EndShapeEdgeTrimSideType, endShapeEdgeTrimSideTypeName);

			GS::Optional<API_EdgeTrimID> type = edgeAngleTypeNames.FindValue (endShapeEdgeTrimSideTypeName);
			if (type.HasValue ()) {
				element.shell.u.extrudedShell.endShapeEdgeData.edgeTrim.sideType = type.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.endShapeEdgeData.edgeTrim.sideType);
			}

			// Edge trim side angle
			endShapeEdgeOs.Get (Shell::EndShapeEdgeTrimSideAngle, element.shell.u.extrudedShell.endShapeEdgeData.edgeTrim.sideAngle);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.endShapeEdgeData.edgeTrim.sideAngle);

			// Overridden side material
			if (endShapeEdgeOs.Contains (Shell::EndShapeEdgeSideMaterial)) {

				endShapeEdgeOs.Get (Shell::EndShapeEdgeSideMaterial, attributeName);

				if (!attributeName.IsEmpty ()) {
					API_Attribute attribute;
					BNZeroMemory (&attribute, sizeof (API_Attribute));
					attribute.header.typeID = API_MaterialID;
					CHCopyC (attributeName.ToCStr (), attribute.header.name);

					if (NoError == ACAPI_Attribute_Get (&attribute)) {
						SetAPIOverriddenAttribute (element.shell.u.extrudedShell.endShapeEdgeData.sideMaterial, attribute.header.index);
						ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeIndexField(u.extrudedShell.endShapeEdgeData.sideMaterial));
					}
				}
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeBoolField (u.extrudedShell.endShapeEdgeData.sideMaterial));
			}

			// Edge type
			GS::UniString endShapeEdgeTypeName;
			endShapeEdgeOs.Get (Shell::EndShapeEdgeType, endShapeEdgeTypeName);

			GS::Optional<API_ShellBaseContourEdgeTypeID> type2 = shellBaseContourEdgeTypeNames.FindValue (endShapeEdgeTypeName);
			if (type2.HasValue ()) {
				element.shell.u.extrudedShell.endShapeEdgeData.edgeType = type2.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.endShapeEdgeData.edgeType);
			}
		}

		// Extruded edge 1
		if (os.Contains (Shell::ExtrudedEdge1)) {
			GS::ObjectState extrudedEdgeOs1;
			os.Get (Shell::ExtrudedEdge1, extrudedEdgeOs1);

			// Edge trim side type
			GS::UniString extrudedEdgeTrimSideTypeName;
			extrudedEdgeOs1.Get (Shell::ExtrudedEdgeTrimSideType1, extrudedEdgeTrimSideTypeName);

			GS::Optional<API_EdgeTrimID> type = edgeAngleTypeNames.FindValue (extrudedEdgeTrimSideTypeName);
			if (type.HasValue ()) {
				element.shell.u.extrudedShell.extrudedEdgeDatas[0].edgeTrim.sideType = type.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.extrudedEdgeDatas[0].edgeTrim.sideType);
			}

			// Edge trim side angle
			extrudedEdgeOs1.Get (Shell::ExtrudedEdgeTrimSideAngle1, element.shell.u.extrudedShell.extrudedEdgeDatas[0].edgeTrim.sideAngle);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.extrudedEdgeDatas[0].edgeTrim.sideAngle);

			// Overridden side material
			if (extrudedEdgeOs1.Contains (Shell::ExtrudedEdgeSideMaterial1)) {

				extrudedEdgeOs1.Get (Shell::ExtrudedEdgeSideMaterial1, attributeName);

				if (!attributeName.IsEmpty ()) {
					API_Attribute attribute;
					BNZeroMemory (&attribute, sizeof (API_Attribute));
					attribute.header.typeID = API_MaterialID;
					CHCopyC (attributeName.ToCStr (), attribute.header.name);

					if (NoError == ACAPI_Attribute_Get (&attribute)) {
						SetAPIOverriddenAttribute (element.shell.u.extrudedShell.extrudedEdgeDatas[0].sideMaterial, attribute.header.index);
						ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeIndexField (u.extrudedShell.extrudedEdgeDatas[0].sideMaterial));
					}
				}
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeBoolField (u.extrudedShell.extrudedEdgeDatas[0].sideMaterial));
			}

			// Edge type
			GS::UniString extrudedEdgeTypeName;
			extrudedEdgeOs1.Get (Shell::ExtrudedEdgeType1, extrudedEdgeTypeName);

			GS::Optional<API_ShellBaseContourEdgeTypeID> type2 = shellBaseContourEdgeTypeNames.FindValue (extrudedEdgeTypeName);
			if (type2.HasValue ()) {
				element.shell.u.extrudedShell.extrudedEdgeDatas[0].edgeType = type2.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.extrudedEdgeDatas[0].edgeType);
			}
		}

		// Extruded edge 2
		if (os.Contains (Shell::ExtrudedEdge2)) {
			GS::ObjectState extrudedEdgeOs2;
			os.Get (Shell::ExtrudedEdge2, extrudedEdgeOs2);

			// Edge trim side type
			GS::UniString extrudedEdgeTrimSideTypeName;
			extrudedEdgeOs2.Get (Shell::ExtrudedEdgeTrimSideType2, extrudedEdgeTrimSideTypeName);

			GS::Optional<API_EdgeTrimID> type = edgeAngleTypeNames.FindValue (extrudedEdgeTrimSideTypeName);
			if (type.HasValue ()) {
				element.shell.u.extrudedShell.extrudedEdgeDatas[1].edgeTrim.sideType = type.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.extrudedEdgeDatas[1].edgeTrim.sideType);
			}

			// Edge trim side angle
			extrudedEdgeOs2.Get (Shell::ExtrudedEdgeTrimSideAngle1, element.shell.u.extrudedShell.extrudedEdgeDatas[1].edgeTrim.sideAngle);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.extrudedEdgeDatas[1].edgeTrim.sideAngle);

			// Overridden side material
			if (extrudedEdgeOs2.Contains (Shell::ExtrudedEdgeSideMaterial1)) {

				extrudedEdgeOs2.Get (Shell::ExtrudedEdgeSideMaterial1, attributeName);

				if (!attributeName.IsEmpty ()) {
					API_Attribute attribute;
					BNZeroMemory (&attribute, sizeof (API_Attribute));
					attribute.header.typeID = API_MaterialID;
					CHCopyC (attributeName.ToCStr (), attribute.header.name);

					if (NoError == ACAPI_Attribute_Get (&attribute)) {
						SetAPIOverriddenAttribute (element.shell.u.extrudedShell.extrudedEdgeDatas[1].sideMaterial, attribute.header.index);
						ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeIndexField (u.extrudedShell.extrudedEdgeDatas[1].sideMaterial));
					}
				}
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeBoolField (u.extrudedShell.extrudedEdgeDatas[1].sideMaterial));
			}

			// Edge type
			GS::UniString extrudedEdgeTypeName;
			extrudedEdgeOs2.Get (Shell::ExtrudedEdgeType1, extrudedEdgeTypeName);

			GS::Optional<API_ShellBaseContourEdgeTypeID> type2 = shellBaseContourEdgeTypeNames.FindValue (extrudedEdgeTypeName);
			if (type2.HasValue ()) {
				element.shell.u.extrudedShell.extrudedEdgeDatas[1].edgeType = type2.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.extrudedShell.extrudedEdgeDatas[1].edgeType);
			}
		}

		break;
	case API_RevolvedShellID:

		if (os.Contains (Shell::SlantAngle)) {
			os.Get (Shell::SlantAngle, element.shell.u.revolvedShell.slantAngle);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.slantAngle);
		}

		if (os.Contains (Shell::RevolutionAngle)) {
			os.Get (Shell::RevolutionAngle, element.shell.u.revolvedShell.revolutionAngle);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.revolutionAngle);
		}

		if (os.Contains (Shell::DistortionAngle)) {
			os.Get (Shell::DistortionAngle, element.shell.u.revolvedShell.distortionAngle);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.distortionAngle);
		}

		if (os.Contains (Shell::SegmentedSurfaces)) {
			os.Get (Shell::SegmentedSurfaces, element.shell.u.revolvedShell.segmentedSurfaces);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.segmentedSurfaces);
		}

		if (os.Contains (ElementBase::Shape)) {
			os.Get (ElementBase::Shape, shellShape);
			element.shell.u.revolvedShell.shellShape.nSubPolys = shellShape.SubpolyCount ();
			element.shell.u.revolvedShell.shellShape.nCoords = shellShape.VertexCount ();
			element.shell.u.revolvedShell.shellShape.nArcs = shellShape.ArcCount ();

			shellShape.SetToMemo (memo, Objects::ElementShape::MemoShellPolygon1);

			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.shellShape.nSubPolys);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.shellShape.nCoords);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.shellShape.nArcs);
		}

		if (os.Contains (Shell::AxisBase)) {
			os.Get (Shell::AxisBase, transformAxisBaseOs);

			Utility::CreateTransform (transformAxisBaseOs, element.shell.u.revolvedShell.axisBase);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.axisBase);
		}

		if (os.Contains (Shell::DistortionVector)) {
			os.Get (Shell::DistortionVector, distortionVector);
			element.shell.u.revolvedShell.distortionVector = distortionVector.ToAPI_Vector ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.distortionVector);
		}

		if (os.Contains (Shell::BegAngle)) {
			os.Get (Shell::BegAngle, element.shell.u.revolvedShell.begAngle);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.begAngle);
		}

		// Beg shape edge
		if (os.Contains (Shell::BegShapeEdge)) {
			GS::ObjectState begShapeEdgeOs;
			os.Get (Shell::BegShapeEdge, begShapeEdgeOs);

			// Edge trim side type
			GS::UniString begShapeEdgeTrimSideTypeName;
			begShapeEdgeOs.Get (Shell::BegShapeEdgeTrimSideType, begShapeEdgeTrimSideTypeName);

			GS::Optional<API_EdgeTrimID> type = edgeAngleTypeNames.FindValue (begShapeEdgeTrimSideTypeName);
			if (type.HasValue ()) {
				element.shell.u.revolvedShell.begShapeEdgeData.edgeTrim.sideType = type.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.begShapeEdgeData.edgeTrim.sideType);
			}

			// Edge trim side angle
			begShapeEdgeOs.Get (Shell::BegShapeEdgeTrimSideAngle, element.shell.u.revolvedShell.begShapeEdgeData.edgeTrim.sideAngle);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.begShapeEdgeData.edgeTrim.sideAngle);

			// Overridden side material
			if (begShapeEdgeOs.Contains (Shell::BegShapeEdgeSideMaterial)) {

				begShapeEdgeOs.Get (Shell::BegShapeEdgeSideMaterial, attributeName);

				if (!attributeName.IsEmpty ()) {
					API_Attribute attribute;
					BNZeroMemory (&attribute, sizeof (API_Attribute));
					attribute.header.typeID = API_MaterialID;
					CHCopyC (attributeName.ToCStr (), attribute.header.name);

					if (NoError == ACAPI_Attribute_Get (&attribute)) {
						SetAPIOverriddenAttribute (element.shell.u.revolvedShell.begShapeEdgeData.sideMaterial, attribute.header.index);
						ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeIndexField (u.revolvedShell.begShapeEdgeData.sideMaterial));
					}
				}
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeBoolField (u.revolvedShell.begShapeEdgeData.sideMaterial));
			}

			// Edge type
			GS::UniString begShapeEdgeTypeName;
			begShapeEdgeOs.Get (Shell::BegShapeEdgeType, begShapeEdgeTypeName);

			GS::Optional<API_ShellBaseContourEdgeTypeID> type2 = shellBaseContourEdgeTypeNames.FindValue (begShapeEdgeTypeName);
			if (type2.HasValue ()) {
				element.shell.u.revolvedShell.begShapeEdgeData.edgeType = type2.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.begShapeEdgeData.edgeType);
			}
		}

		// End shape edge
		if (os.Contains (Shell::EndShapeEdge)) {
			GS::ObjectState endShapeEdgeOs;
			os.Get (Shell::EndShapeEdge, endShapeEdgeOs);

			// Edge trim side type
			GS::UniString endShapeEdgeTrimSideTypeName;
			endShapeEdgeOs.Get (Shell::EndShapeEdgeTrimSideType, endShapeEdgeTrimSideTypeName);

			GS::Optional<API_EdgeTrimID> type = edgeAngleTypeNames.FindValue (endShapeEdgeTrimSideTypeName);
			if (type.HasValue ()) {
				element.shell.u.revolvedShell.endShapeEdgeData.edgeTrim.sideType = type.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.endShapeEdgeData.edgeTrim.sideType);
			}

			// Edge trim side angle
			endShapeEdgeOs.Get (Shell::EndShapeEdgeTrimSideAngle, element.shell.u.revolvedShell.endShapeEdgeData.edgeTrim.sideAngle);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.endShapeEdgeData.edgeTrim.sideAngle);

			// Overridden side material
			if (endShapeEdgeOs.Contains (Shell::EndShapeEdgeSideMaterial)) {

				endShapeEdgeOs.Get (Shell::EndShapeEdgeSideMaterial, attributeName);

				if (!attributeName.IsEmpty ()) {
					API_Attribute attribute;
					BNZeroMemory (&attribute, sizeof (API_Attribute));
					attribute.header.typeID = API_MaterialID;
					CHCopyC (attributeName.ToCStr (), attribute.header.name);

					if (NoError == ACAPI_Attribute_Get (&attribute)) {
						SetAPIOverriddenAttribute (element.shell.u.revolvedShell.endShapeEdgeData.sideMaterial, attribute.header.index);
						ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeIndexField (u.revolvedShell.endShapeEdgeData.sideMaterial));
					}
				}
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeBoolField (u.revolvedShell.endShapeEdgeData.sideMaterial));
			}

			// Edge type
			GS::UniString endShapeEdgeTypeName;
			endShapeEdgeOs.Get (Shell::EndShapeEdgeType, endShapeEdgeTypeName);

			GS::Optional<API_ShellBaseContourEdgeTypeID> type2 = shellBaseContourEdgeTypeNames.FindValue (endShapeEdgeTypeName);
			if (type2.HasValue ()) {
				element.shell.u.revolvedShell.endShapeEdgeData.edgeType = type2.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.endShapeEdgeData.edgeType);
			}
		}

		// Revolved edge 1
		if (os.Contains (Shell::RevolvedEdge1)) {
			GS::ObjectState revolvedEdgeOs1;
			os.Get (Shell::RevolvedEdge1, revolvedEdgeOs1);

			// Edge trim side type
			GS::UniString revolvedEdgeTrimSideTypeName;
			revolvedEdgeOs1.Get (Shell::RevolvedEdgeTrimSideType1, revolvedEdgeTrimSideTypeName);

			GS::Optional<API_EdgeTrimID> type = edgeAngleTypeNames.FindValue (revolvedEdgeTrimSideTypeName);
			if (type.HasValue ()) {
				element.shell.u.revolvedShell.revolvedEdgeDatas[0].edgeTrim.sideType = type.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.revolvedEdgeDatas[0].edgeTrim.sideType);
			}

			// Edge trim side angle
			revolvedEdgeOs1.Get (Shell::RevolvedEdgeTrimSideAngle1, element.shell.u.revolvedShell.revolvedEdgeDatas[0].edgeTrim.sideAngle);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.revolvedEdgeDatas[0].edgeTrim.sideAngle);

			// Overridden side material
			if (revolvedEdgeOs1.Contains (Shell::RevolvedEdgeSideMaterial1)) {

				revolvedEdgeOs1.Get (Shell::RevolvedEdgeSideMaterial1, attributeName);

				if (!attributeName.IsEmpty ()) {
					API_Attribute attribute;
					BNZeroMemory (&attribute, sizeof (API_Attribute));
					attribute.header.typeID = API_MaterialID;
					CHCopyC (attributeName.ToCStr (), attribute.header.name);

					if (NoError == ACAPI_Attribute_Get (&attribute)) {
						SetAPIOverriddenAttribute (element.shell.u.revolvedShell.revolvedEdgeDatas[0].sideMaterial, attribute.header.index);
						ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeIndexField (u.revolvedShell.revolvedEdgeDatas[0].sideMaterial));
					}
				}
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeBoolField (u.revolvedShell.revolvedEdgeDatas[0].sideMaterial));
			}

			// Edge type
			GS::UniString revolvedEdgeTypeName;
			revolvedEdgeOs1.Get (Shell::RevolvedEdgeType1, revolvedEdgeTypeName);

			GS::Optional<API_ShellBaseContourEdgeTypeID> type2 = shellBaseContourEdgeTypeNames.FindValue (revolvedEdgeTypeName);
			if (type2.HasValue ()) {
				element.shell.u.revolvedShell.revolvedEdgeDatas[0].edgeType = type2.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.revolvedEdgeDatas[0].edgeType);
			}
		}

		// Revolved edge 2
		if (os.Contains (Shell::RevolvedEdge2)) {
			GS::ObjectState revolvedEdgeOs2;
			os.Get (Shell::RevolvedEdge2, revolvedEdgeOs2);

			// Edge trim side type
			GS::UniString revolvedEdgeTrimSideTypeName;
			revolvedEdgeOs2.Get (Shell::RevolvedEdgeTrimSideType2, revolvedEdgeTrimSideTypeName);

			GS::Optional<API_EdgeTrimID> type = edgeAngleTypeNames.FindValue (revolvedEdgeTrimSideTypeName);
			if (type.HasValue ()) {
				element.shell.u.revolvedShell.revolvedEdgeDatas[1].edgeTrim.sideType = type.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.revolvedEdgeDatas[1].edgeTrim.sideType);
			}

			// Edge trim side angle
			revolvedEdgeOs2.Get (Shell::RevolvedEdgeTrimSideAngle1, element.shell.u.revolvedShell.revolvedEdgeDatas[1].edgeTrim.sideAngle);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.revolvedEdgeDatas[1].edgeTrim.sideAngle);

			// Overridden side material
			if (revolvedEdgeOs2.Contains (Shell::RevolvedEdgeSideMaterial1)) {

				revolvedEdgeOs2.Get (Shell::RevolvedEdgeSideMaterial1, attributeName);

				if (!attributeName.IsEmpty ()) {
					API_Attribute attribute;
					BNZeroMemory (&attribute, sizeof (API_Attribute));
					attribute.header.typeID = API_MaterialID;
					CHCopyC (attributeName.ToCStr (), attribute.header.name);

					if (NoError == ACAPI_Attribute_Get (&attribute)) {
						SetAPIOverriddenAttribute (element.shell.u.revolvedShell.revolvedEdgeDatas[1].sideMaterial, attribute.header.index);
						ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeIndexField (u.revolvedShell.revolvedEdgeDatas[1].sideMaterial));
					}
				}
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeBoolField (u.revolvedShell.revolvedEdgeDatas[1].sideMaterial));
			}

			// Edge type
			GS::UniString revolvedEdgeTypeName;
			revolvedEdgeOs2.Get (Shell::RevolvedEdgeType1, revolvedEdgeTypeName);

			GS::Optional<API_ShellBaseContourEdgeTypeID> type2 = shellBaseContourEdgeTypeNames.FindValue (revolvedEdgeTypeName);
			if (type2.HasValue ()) {
				element.shell.u.revolvedShell.revolvedEdgeDatas[1].edgeType = type2.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.revolvedShell.revolvedEdgeDatas[1].edgeType);
			}
		}

		break;
	case API_RuledShellID:

		if (os.Contains (ElementBase::Shape1)) {
			os.Get (ElementBase::Shape1, shellShape1);
			element.shell.u.ruledShell.shellShape1.nSubPolys = shellShape1.SubpolyCount ();
			element.shell.u.ruledShell.shellShape1.nCoords = shellShape1.VertexCount ();
			element.shell.u.ruledShell.shellShape1.nArcs = shellShape1.ArcCount ();

			shellShape1.SetToMemo (memo, Objects::ElementShape::MemoShellPolygon1);

			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.shellShape1.nSubPolys);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.shellShape1.nCoords);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.shellShape1.nArcs);
		}

		if (os.Contains (Shell::Plane1)) {
			os.Get (Shell::Plane1, transformPlaneOs1);

			Utility::CreateTransform (transformPlaneOs1, element.shell.u.ruledShell.plane1);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.plane1);
		}

		if (os.Contains (ElementBase::Shape2)) {
			os.Get (ElementBase::Shape2, shellShape2);
			element.shell.u.ruledShell.shellShape2.nSubPolys = shellShape2.SubpolyCount ();
			element.shell.u.ruledShell.shellShape2.nCoords = shellShape2.VertexCount ();
			element.shell.u.ruledShell.shellShape2.nArcs = shellShape2.ArcCount ();

			shellShape2.SetToMemo (memo, Objects::ElementShape::MemoShellPolygon2);

			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.shellShape2.nSubPolys);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.shellShape2.nCoords);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.shellShape2.nArcs);
		}

		if (os.Contains (Shell::Plane2)) {
			os.Get (Shell::Plane2, transformPlaneOs2);

			Utility::CreateTransform (transformPlaneOs2, element.shell.u.ruledShell.plane2);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.plane2);
		}

		// Beg shape edge
		if (os.Contains (Shell::BegShapeEdge)) {
			GS::ObjectState begShapeEdgeOs;
			os.Get (Shell::BegShapeEdge, begShapeEdgeOs);

			// Edge trim side type
			GS::UniString begShapeEdgeTrimSideTypeName;
			begShapeEdgeOs.Get (Shell::BegShapeEdgeTrimSideType, begShapeEdgeTrimSideTypeName);

			GS::Optional<API_EdgeTrimID> type = edgeAngleTypeNames.FindValue (begShapeEdgeTrimSideTypeName);
			if (type.HasValue ()) {
				element.shell.u.ruledShell.begShapeEdgeData.edgeTrim.sideType = type.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.begShapeEdgeData.edgeTrim.sideType);
			}

			// Edge trim side angle
			begShapeEdgeOs.Get (Shell::BegShapeEdgeTrimSideAngle, element.shell.u.ruledShell.begShapeEdgeData.edgeTrim.sideAngle);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.begShapeEdgeData.edgeTrim.sideAngle);

			// Overridden side material
			if (begShapeEdgeOs.Contains (Shell::BegShapeEdgeSideMaterial)) {

				begShapeEdgeOs.Get (Shell::BegShapeEdgeSideMaterial, attributeName);

				if (!attributeName.IsEmpty ()) {
					API_Attribute attribute;
					BNZeroMemory (&attribute, sizeof (API_Attribute));
					attribute.header.typeID = API_MaterialID;
					CHCopyC (attributeName.ToCStr (), attribute.header.name);

					if (NoError == ACAPI_Attribute_Get (&attribute)) {
						SetAPIOverriddenAttribute (element.shell.u.ruledShell.begShapeEdgeData.sideMaterial, attribute.header.index);
						ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeIndexField (u.ruledShell.begShapeEdgeData.sideMaterial));
					}
				}
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeBoolField (u.ruledShell.begShapeEdgeData.sideMaterial));
			}

			// Edge type
			GS::UniString begShapeEdgeTypeName;
			begShapeEdgeOs.Get (Shell::BegShapeEdgeType, begShapeEdgeTypeName);

			GS::Optional<API_ShellBaseContourEdgeTypeID> type2 = shellBaseContourEdgeTypeNames.FindValue (begShapeEdgeTypeName);
			if (type2.HasValue ()) {
				element.shell.u.ruledShell.begShapeEdgeData.edgeType = type2.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.begShapeEdgeData.edgeType);
			}
		}

		// End shape edge
		if (os.Contains (Shell::EndShapeEdge)) {
			GS::ObjectState endShapeEdgeOs;
			os.Get (Shell::EndShapeEdge, endShapeEdgeOs);

			// Edge trim side type
			GS::UniString endShapeEdgeTrimSideTypeName;
			endShapeEdgeOs.Get (Shell::EndShapeEdgeTrimSideType, endShapeEdgeTrimSideTypeName);

			GS::Optional<API_EdgeTrimID> type = edgeAngleTypeNames.FindValue (endShapeEdgeTrimSideTypeName);
			if (type.HasValue ()) {
				element.shell.u.ruledShell.endShapeEdgeData.edgeTrim.sideType = type.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.endShapeEdgeData.edgeTrim.sideType);
			}

			// Edge trim side angle
			endShapeEdgeOs.Get (Shell::EndShapeEdgeTrimSideAngle, element.shell.u.ruledShell.endShapeEdgeData.edgeTrim.sideAngle);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.endShapeEdgeData.edgeTrim.sideAngle);

			// Overridden side material
			if (endShapeEdgeOs.Contains (Shell::EndShapeEdgeSideMaterial)) {

				endShapeEdgeOs.Get (Shell::EndShapeEdgeSideMaterial, attributeName);

				if (!attributeName.IsEmpty ()) {
					API_Attribute attribute;
					BNZeroMemory (&attribute, sizeof (API_Attribute));
					attribute.header.typeID = API_MaterialID;
					CHCopyC (attributeName.ToCStr (), attribute.header.name);

					if (NoError == ACAPI_Attribute_Get (&attribute)) {
						SetAPIOverriddenAttribute (element.shell.u.ruledShell.endShapeEdgeData.sideMaterial, attribute.header.index);
						ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeIndexField (u.ruledShell.endShapeEdgeData.sideMaterial));
					}
				}
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeBoolField (u.ruledShell.endShapeEdgeData.sideMaterial));
			}

			// Edge type
			GS::UniString endShapeEdgeTypeName;
			endShapeEdgeOs.Get (Shell::EndShapeEdgeType, endShapeEdgeTypeName);

			GS::Optional<API_ShellBaseContourEdgeTypeID> type2 = shellBaseContourEdgeTypeNames.FindValue (endShapeEdgeTypeName);
			if (type2.HasValue ()) {
				element.shell.u.ruledShell.endShapeEdgeData.edgeType = type2.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.endShapeEdgeData.edgeType);
			}
		}

		// Ruled edge 1
		if (os.Contains (Shell::RuledEdge1)) {
			GS::ObjectState ruledEdgeOs1;
			os.Get (Shell::RuledEdge1, ruledEdgeOs1);

			// Edge trim side type
			GS::UniString ruledEdgeTrimSideTypeName;
			ruledEdgeOs1.Get (Shell::RuledEdgeTrimSideType1, ruledEdgeTrimSideTypeName);

			GS::Optional<API_EdgeTrimID> type = edgeAngleTypeNames.FindValue (ruledEdgeTrimSideTypeName);
			if (type.HasValue ()) {
				element.shell.u.ruledShell.ruledEdgeDatas[0].edgeTrim.sideType = type.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.ruledEdgeDatas[0].edgeTrim.sideType);
			}

			// Edge trim side angle
			ruledEdgeOs1.Get (Shell::RuledEdgeTrimSideAngle1, element.shell.u.ruledShell.ruledEdgeDatas[0].edgeTrim.sideAngle);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.ruledEdgeDatas[0].edgeTrim.sideAngle);

			// Overridden side material
			if (ruledEdgeOs1.Contains (Shell::RuledEdgeSideMaterial1)) {

				ruledEdgeOs1.Get (Shell::RuledEdgeSideMaterial1, attributeName);

				if (!attributeName.IsEmpty ()) {
					API_Attribute attribute;
					BNZeroMemory (&attribute, sizeof (API_Attribute));
					attribute.header.typeID = API_MaterialID;
					CHCopyC (attributeName.ToCStr (), attribute.header.name);

					if (NoError == ACAPI_Attribute_Get (&attribute)) {
						SetAPIOverriddenAttribute (element.shell.u.ruledShell.ruledEdgeDatas[0].sideMaterial, attribute.header.index);
						ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeIndexField (u.ruledShell.ruledEdgeDatas[0].sideMaterial));
					}
				}
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeBoolField (u.ruledShell.ruledEdgeDatas[0].sideMaterial));
			}

			// Edge type
			GS::UniString ruledEdgeTypeName;
			ruledEdgeOs1.Get (Shell::RuledEdgeType1, ruledEdgeTypeName);

			GS::Optional<API_ShellBaseContourEdgeTypeID> type2 = shellBaseContourEdgeTypeNames.FindValue (ruledEdgeTypeName);
			if (type2.HasValue ()) {
				element.shell.u.ruledShell.ruledEdgeDatas[0].edgeType = type2.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.ruledEdgeDatas[0].edgeType);
			}
		}

		// Ruled edge 2
		if (os.Contains (Shell::RuledEdge2)) {
			GS::ObjectState ruledEdgeOs2;
			os.Get (Shell::RuledEdge2, ruledEdgeOs2);

			// Edge trim side type
			GS::UniString ruledEdgeTrimSideTypeName;
			ruledEdgeOs2.Get (Shell::RuledEdgeTrimSideType2, ruledEdgeTrimSideTypeName);

			GS::Optional<API_EdgeTrimID> type = edgeAngleTypeNames.FindValue (ruledEdgeTrimSideTypeName);
			if (type.HasValue ()) {
				element.shell.u.ruledShell.ruledEdgeDatas[1].edgeTrim.sideType = type.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.ruledEdgeDatas[1].edgeTrim.sideType);
			}

			// Edge trim side angle
			ruledEdgeOs2.Get (Shell::RuledEdgeTrimSideAngle2, element.shell.u.ruledShell.ruledEdgeDatas[1].edgeTrim.sideAngle);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.ruledEdgeDatas[1].edgeTrim.sideAngle);

			// Overridden side material
			if (ruledEdgeOs2.Contains (Shell::RuledEdgeSideMaterial2)) {

				ruledEdgeOs2.Get (Shell::RuledEdgeSideMaterial2, attributeName);

				if (!attributeName.IsEmpty ()) {
					API_Attribute attribute;
					BNZeroMemory (&attribute, sizeof (API_Attribute));
					attribute.header.typeID = API_MaterialID;
					CHCopyC (attributeName.ToCStr (), attribute.header.name);

					if (NoError == ACAPI_Attribute_Get (&attribute)) {
						SetAPIOverriddenAttribute (element.shell.u.ruledShell.ruledEdgeDatas[1].sideMaterial, attribute.header.index);
						ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeIndexField (u.ruledShell.ruledEdgeDatas[1].sideMaterial));
					}
				}
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeBoolField (u.ruledShell.ruledEdgeDatas[1].sideMaterial));
			}

			// Edge type
			GS::UniString ruledEdgeTypeName;
			ruledEdgeOs2.Get (Shell::RuledEdgeType2, ruledEdgeTypeName);

			GS::Optional<API_ShellBaseContourEdgeTypeID> type2 = shellBaseContourEdgeTypeNames.FindValue (ruledEdgeTypeName);
			if (type2.HasValue ()) {
				element.shell.u.ruledShell.ruledEdgeDatas[1].edgeType = type2.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.ruledEdgeDatas[1].edgeType);
			}
		}

		// Morphing rule
		if (os.Contains (Shell::MorphingRuleName)) {

			GS::UniString morphingRuleName;
			os.Get (Shell::MorphingRuleName, morphingRuleName);

			GS::Optional<API_MorphingRuleID> type = morphingRuleNames.FindValue (morphingRuleName);
			if (type.HasValue ()) {
				element.shell.u.ruledShell.morphingRule = type.Get ();
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, u.ruledShell.morphingRule);
			}
		}

		break;
	default:
		break;
	}

	// The floor index and level of the shell
	if (os.Contains (ElementBase::Level)) {
		GetStoryFromObjectState (os, shellShape.Level (), element.header.floorInd, element.shell.shellBase.level);
	}
	else {
		Utility::SetStoryLevelAndFloor (shellShape.Level (), element.header.floorInd, element.shell.shellBase.level);
	}
		ACAPI_ELEMENT_MASK_SET (elementMask, API_Elem_Head, floorInd);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.level);

	// The thickness of the shell
	if (os.Contains (Shell::Thickness)) {
		os.Get (Shell::Thickness, element.shell.shellBase.thickness);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.thickness);
	}

	// The structure of the shell
	if (os.Contains (Shell::Structure)) {
		GS::UniString structureName;
		os.Get (Shell::Structure, structureName);

		GS::Optional<API_ModelElemStructureType> type = structureTypeNames.FindValue (structureName);
		if (type.HasValue ())
			element.shell.shellBase.modelElemStructureType = type.Get ();

		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.modelElemStructureType);
	}

	// The building material name of the shell.shellBase
	if (os.Contains (Shell::BuildingMaterialName) &&
	  element.shell.shellBase.modelElemStructureType == API_BasicStructure) {

		os.Get (Shell::BuildingMaterialName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_BuildingMaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.shell.shellBase.buildingMaterial = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.buildingMaterial);
			}
		}
	}

	// The composite name of the shell.shellBase
	if (os.Contains (Shell::CompositeName) &&
	  element.shell.shellBase.modelElemStructureType == API_CompositeStructure) {

		os.Get (Shell::CompositeName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_CompWallID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.shell.shellBase.composite = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.composite);
			}
		}
	}

	// The edge type of the shell
	if (os.Contains (Shell::EdgeAngleType)) {
		GS::UniString edgeAngleType;
		os.Get (Shell::EdgeAngleType, edgeAngleType);

		GS::Optional<API_EdgeTrimID> type = edgeAngleTypeNames.FindValue (edgeAngleType);
		if (type.HasValue ()) {
			element.shell.shellBase.edgeTrim.sideType = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.edgeTrim.sideType);
		}
	}

	// The edge angle of the shell
	if (os.Contains (Shell::EdgeAngle)) {
		os.Get (Shell::EdgeAngle, element.shell.shellBase.edgeTrim.sideAngle);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.edgeTrim.sideAngle);
	}

	// Floor Plan and Section - Floor Plan Display

	// Show on Stories - Story visibility
	bool isAutoOnStoryVisibility = false;
	Utility::CreateVisibility (os, VisibilityContData, isAutoOnStoryVisibility, element.shell.shellBase.visibilityCont);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.visibilityCont.showOnHome);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.visibilityCont.showAllAbove);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.visibilityCont.showAllBelow);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.visibilityCont.showRelAbove);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.visibilityCont.showRelBelow);

	Utility::CreateVisibility (os, VisibilityFillData, isAutoOnStoryVisibility, element.shell.shellBase.visibilityFill);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.visibilityFill.showOnHome);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.visibilityFill.showAllAbove);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.visibilityFill.showAllBelow);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.visibilityFill.showRelAbove);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.visibilityFill.showRelBelow);

	// The display options (Projected, Projected with Overhead, Cut Only, Outlines Only, Overhead All or Symbolic Cut)
	if (os.Contains (Shell::DisplayOptionName)) {
		GS::UniString displayOptionName;
		os.Get (Shell::DisplayOptionName, displayOptionName);

		GS::Optional<API_ElemDisplayOptionsID> type = displayOptionNames.FindValue (displayOptionName);
		if (type.HasValue ()) {
			element.shell.shellBase.displayOption = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.displayOption);
		}
	}

	// Show projection (To Floor Plan Range, To Absolute Display Limit, Entire Element)
	if (os.Contains (Shell::ViewDepthLimitationName)) {
		GS::UniString viewDepthLimitationName;
		os.Get (Shell::ViewDepthLimitationName, viewDepthLimitationName);

		GS::Optional<API_ElemViewDepthLimitationsID> type = viewDepthLimitationNames.FindValue (viewDepthLimitationName);
		if (type.HasValue ())
			element.shell.shellBase.viewDepthLimitation = type.Get ();

		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.viewDepthLimitation);
	}

	// Floor Plan and Section - Cut Surfaces

	// The pen index and linetype name of shell section line
	if (os.Contains (Shell::SectContPen)) {
		os.Get (Shell::SectContPen, element.shell.shellBase.sectContPen);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.sectContPen);
	}

	if (os.Contains (Shell::SectContLtype)) {

		os.Get (Shell::SectContLtype, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.shell.shellBase.sectContLtype = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.sectContLtype);
	}

	// Override cut fill and cut fill backgound pens
	if (CommandHelpers::SetCutfillPens(
		os,
		Shell::CutFillPen,
		Shell::CutFillBackgroundPen,
		element.shell.shellBase,
		elementMask)
		!= NoError)
		return Error;

	// Outlines

	// The pen index and linetype name of shell contour line
	if (os.Contains (Shell::ContourPen)) {
		os.Get (Shell::ContourPen, element.shell.shellBase.pen);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.pen);
	}

	if (os.Contains (Shell::ContourLineType)) {

		os.Get (Shell::ContourLineType, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.shell.shellBase.ltypeInd = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.ltypeInd);
			}
		}
	}

	// The pen index and linetype name of slab hidden contour line
	if (os.Contains (Shell::OverheadLinePen)) {
		os.Get (Shell::OverheadLinePen, element.shell.shellBase.aboveViewLinePen);

		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.aboveViewLinePen);
	}

	if (os.Contains (Shell::OverheadLinetype)) {

		os.Get (Shell::OverheadLinetype, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.shell.shellBase.aboveViewLineType = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.aboveViewLineType);
			}
		}
	}

	// Floor Plan and Section - Cover Fills
	if (os.Contains (Shell::UseFloorFill)) {
		os.Get (Shell::UseFloorFill, element.shell.shellBase.useFloorFill);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.useFloorFill);
	}

	if (os.Contains (Shell::Use3DHatching)) {
		os.Get (Shell::Use3DHatching, element.shell.shellBase.use3DHatching);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.use3DHatching);
	}

	if (os.Contains (Shell::UseFillLocBaseLine)) {
		os.Get (Shell::UseFillLocBaseLine, element.shell.shellBase.useFillLocBaseLine);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.useFillLocBaseLine);
	}

	if (os.Contains (Shell::UseSlantedFill)) {
		os.Get (Shell::UseSlantedFill, element.shell.shellBase.useSlantedFill);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.useSlantedFill);
	}

	if (os.Contains (Shell::FloorFillPen)) {
		os.Get (Shell::FloorFillPen, element.shell.shellBase.floorFillPen);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.floorFillPen);
	}

	if (os.Contains (Shell::FloorFillBGPen)) {
		os.Get (Shell::FloorFillBGPen, element.shell.shellBase.floorFillBGPen);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.floorFillBGPen);
	}

	// Cover fill type
	if (os.Contains (Shell::FloorFillName)) {

		os.Get (Shell::FloorFillName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_FilltypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.shell.shellBase.floorFillInd = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.floorFillInd);
			}
		}
	}

	// Cover Fill Transformation
	Utility::CreateHatchOrientation (os, element.shell.shellBase.hatchOrientation.type);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.hatchOrientation.type);

	if (os.Contains (Shell::HatchOrientationOrigoX)) {
		os.Get (Shell::HatchOrientationOrigoX, element.shell.shellBase.hatchOrientation.origo.x);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.hatchOrientation.origo.x);
	}

	if (os.Contains (Shell::HatchOrientationOrigoY)) {
		os.Get (Shell::HatchOrientationOrigoY, element.shell.shellBase.hatchOrientation.origo.y);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.hatchOrientation.origo.y);
	}

	if (os.Contains (Shell::HatchOrientationXAxisX)) {
		os.Get (Shell::HatchOrientationXAxisX, element.shell.shellBase.hatchOrientation.matrix00);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.hatchOrientation.matrix00);
	}

	if (os.Contains (Shell::HatchOrientationXAxisY)) {
		os.Get (Shell::HatchOrientationXAxisY, element.shell.shellBase.hatchOrientation.matrix10);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.hatchOrientation.matrix10);
	}

	if (os.Contains (Shell::HatchOrientationYAxisX)) {
		os.Get (Shell::HatchOrientationYAxisX, element.shell.shellBase.hatchOrientation.matrix01);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.hatchOrientation.matrix01);
	}

	if (os.Contains (Shell::HatchOrientationYAxisY)) {
		os.Get (Shell::HatchOrientationYAxisY, element.shell.shellBase.hatchOrientation.matrix11);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.hatchOrientation.matrix11);
	}

	// Model

	// Overridden materials
	ResetAPIOverriddenAttribute (element.shell.shellBase.topMat);
	if (os.Contains (Shell::TopMat)) {

		os.Get (Shell::TopMat, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				SetAPIOverriddenAttribute (element.shell.shellBase.topMat, attribute.header.index);
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeIndexField (shellBase.topMat));
			}
		}
	}
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeBoolField (shellBase.topMat));

	ResetAPIOverriddenAttribute (element.shell.shellBase.sidMat);
	if (os.Contains (Shell::SideMat)) {

		os.Get (Shell::SideMat, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				SetAPIOverriddenAttribute (element.shell.shellBase.sidMat, attribute.header.index);
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeIndexField (shellBase.sidMat));
			}
		}
	}
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeBoolField (shellBase.sidMat));

	ResetAPIOverriddenAttribute (element.shell.shellBase.botMat);
	if (os.Contains (Shell::BotMat)) {

		os.Get (Shell::BotMat, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				SetAPIOverriddenAttribute (element.shell.shellBase.botMat, attribute.header.index);
				ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeIndexField (shellBase.botMat));
			}
		}
	}
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, GetAPIOverriddenAttributeBoolField (shellBase.botMat));

	// The overridden materials are chained
	if (os.Contains (Shell::MaterialsChained)) {
		os.Get (Shell::MaterialsChained, element.shell.shellBase.materialsChained);

		ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.materialsChained);
	}

	// Trimming Body (Editable, Contours Down, Pivot Lines Down, Upwards Extrusion or Downwards Extrusion)
	if (os.Contains (Shell::TrimmingBodyName)) {
		GS::UniString trimmingBodyName;
		os.Get (Shell::TrimmingBodyName, trimmingBodyName);

		GS::Optional<API_ShellBaseCutBodyTypeID> type = shellBaseCutBodyTypeNames.FindValue (trimmingBodyName);
		if (type.HasValue ()) {
			element.shell.shellBase.cutBodyType = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ShellType, shellBase.cutBodyType);
		}
	}

	return NoError;
}


GS::String CreateShell::GetName () const
{
	return CreateShellCommandName;
}


} // namespace AddOnCommands
