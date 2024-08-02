#include "CreateGridElement.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "APIHelper.hpp"
#include "Utility.hpp"
#include "Objects/Level.hpp"
#include "Objects/Point.hpp"
#include "FieldNames.hpp"
#include "GenArc2DData.h"
using namespace FieldNames;

namespace AddOnCommands {


GS::String CreateGridElement::GetFieldName () const
{
	return FieldNames::GridElements;
}


GS::UniString CreateGridElement::GetUndoableCommandName () const
{
	return "CreateSpeckleGridElement";
}


GSErrCode CreateGridElement::GetElementFromObjectState (const GS::ObjectState& os,
	API_Element& element,
	API_Element& elementMask,
	API_ElementMemo& memo,
	GS::UInt64& memoMask,
	API_SubElement** /*marker*/,
	AttributeManager& /*attributeManager*/,
	LibpartImportManager& /*libpartImportManager*/,
	GS::Array<GS::UniString>& log) const
{
	GSErrCode err = NoError;

	Utility::SetElementType (element.header, API_ElemType (API_ObjectID, APIVarId_GridElement));

	err = Utility::GetBaseElementData (element, &memo, nullptr, log);
	if (err != NoError)
		return err;

	Objects::Point3D begin, end;
	GS::UniString markerText;
	bool isArc = false;
	double length = .0;
	double radius = .0;
	double arcAngle = .0;

	if (os.Contains (GridElement::begin) && os.Contains (GridElement::end)) {
		os.Get (GridElement::begin, begin);
		os.Get (GridElement::end, end);
	}

	if (os.Contains (GridElement::markerText)) {
		os.Get (GridElement::markerText, markerText);
	}

	if (os.Contains (GridElement::isArc)) {
		os.Get (GridElement::isArc, isArc);
	}

	if (os.Contains (GridElement::arcAngle)) {
		os.Get (GridElement::arcAngle, arcAngle);
	}

	if (!isArc) {
		// position
		element.object.pos = begin.ToAPI_Coord ();
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ObjectType, pos);

		// angle
		Vector2D beginToEnd (end.x - begin.x, end.y - begin.y);
		element.object.angle = beginToEnd.CalcAngleToReference (Vector2D (1.0, 0));
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ObjectType, angle);

		// length
		length = beginToEnd.GetLength ();

	} else {
		// offset
		GenArc arc = GenArc::CreateCircleArc (Point2D(begin.x,begin.y), Point2D(end.x, end.y), arcAngle);
		Point2D origo = arc.GetOrigo ();
		element.object.pos.x = origo.GetX ();
		element.object.pos.y = origo.GetY ();

		Point2D beginPoint (begin.x, begin.y);
		Point2D endPoint (end.x, end.y);

		Geometry::Transformation2D transformationGlobal = Geometry::Transformation2D::CreateTranslation (Vector2D(origo) * -1.0);
		beginPoint = transformationGlobal.Apply (beginPoint);
		endPoint = transformationGlobal.Apply (endPoint);

		// rotation
		bool mirror = (arcAngle < 0.0);
	
		Vector2D beginVector (beginPoint);
		element.object.angle = beginVector.CalcAngleToReference (Vector2D (mirror ? -1.0 : 1.0, 0));
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ObjectType, angle);

		// mirror
		if (mirror) {
			arcAngle *= -1.0;
			element.object.reflected = true;
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ObjectType, reflected);
		}
		
		// radius
		radius = beginVector.GetLength ();
	}

	GSSize addParCount = BMGetHandleSize (reinterpret_cast<GSHandle>(memo.params)) / sizeof (API_AddParType);
	
	for (Int32 i = 0; i < addParCount; ++i) {
		API_AddParType& parameter = (*memo.params)[i];
		
		if (CHCompareCStrings (parameter.name, "AC_MarkerText_1", CS_CaseSensitive) == 0) {
			GS::ucscpy (parameter.value.uStr, markerText.ToUStr ().Get ());
		} else if (CHCompareCStrings (parameter.name, "AC_LineVisibility_i", CS_CaseSensitive) == 0) {
			parameter.value.real = (int)1;
		} else if (CHCompareCStrings (parameter.name, "AC_StaggerDist", CS_CaseSensitive) == 0) {
			parameter.value.real = .0;
		} else if (CHCompareCStrings (parameter.name, "AC_Type_i", CS_CaseSensitive) == 0) {
			if (isArc) {
				parameter.value.real = (int)2;
			} else {
				parameter.value.real = (int)1;
			}
		} else if (CHCompareCStrings (parameter.name, "AC_Length", CS_CaseSensitive) == 0) {
			if (!isArc) {
				parameter.value.real = length;
			}
		} else if (CHCompareCStrings (parameter.name, "AC_Angle", CS_CaseSensitive) == 0) {
			if (isArc) {
				parameter.value.real = arcAngle;
			}
		} else if (CHCompareCStrings (parameter.name, "AC_Radius", CS_CaseSensitive) == 0) {
			if (isArc) {
				parameter.value.real = radius;
			}
		}
	}

	memoMask = APIMemoMask_AddPars;

	return NoError;
}


GS::String CreateGridElement::GetName () const
{
	return CreateGridElementCommandName;
}


}
 // namespace AddOnCommands
