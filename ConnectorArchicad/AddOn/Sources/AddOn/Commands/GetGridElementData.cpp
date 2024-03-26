#include "GetGridElementData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Level.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
using namespace FieldNames;


namespace AddOnCommands {


GS::String GetGridElementData::GetFieldName () const
{
	return GridElements;
}


API_ElemTypeID GetGridElementData::GetElemTypeID () const
{
	return API_ObjectID;
}


GS::ErrCode	GetGridElementData::SerializeElementType (const API_Element& element,
	const API_ElementMemo& memo,
	GS::ObjectState& os) const
{
	GS::UniString markerText;
	double angle = element.object.angle;
	double length = .0;
	int type = 0;
	double arcAngle = .0;
	double radius = .0;

	{
		GSSize addParCount = BMGetHandleSize (reinterpret_cast<GSHandle>(memo.params)) / sizeof (API_AddParType);
		
		for (Int32 i = 0; i < addParCount; ++i) {
			API_AddParType& parameter = (*memo.params)[i];
			if (CHCompareCStrings (parameter.name, "AC_Length", CS_CaseSensitive) == 0) {
				length = parameter.value.real;
			} else if (CHCompareCStrings (parameter.name, "AC_MarkerText_1", CS_CaseSensitive) == 0) {
				markerText = parameter.value.uStr;
			} else if (CHCompareCStrings (parameter.name, "AC_Type_i", CS_CaseSensitive) == 0) {
				type = (int)parameter.value.real;
			} else if (CHCompareCStrings (parameter.name, "AC_Angle", CS_CaseSensitive) == 0) {
				arcAngle = parameter.value.real;
			} else if (CHCompareCStrings (parameter.name, "AC_Radius", CS_CaseSensitive) == 0) {
				radius = parameter.value.real;
			}
		}
	}

	bool isArc = (type == 2);

	Vector2D beginVector, endVector;
	if (!isArc) {
		beginVector = Vector2D (.0, .0);
		endVector = Vector2D (length, .0);
	} else {
		beginVector = Vector2D (radius, .0);
		Geometry::Transformation2D rotationEnd = Geometry::Transformation2D::CreateOrigoRotation (arcAngle);

		endVector = rotationEnd.Apply (beginVector);
	}

	{
		// mirror
		if (element.object.reflected) {
			Geometry::Transformation2D rotationMirrorY = Geometry::Transformation2D::CreateMirrorY ();
			beginVector = rotationMirrorY.Apply (beginVector);
			endVector = rotationMirrorY.Apply (endVector);
			
			arcAngle *= -1;
		}

		// rotation
		Geometry::Transformation2D rotationGlobal = Geometry::Transformation2D::CreateOrigoRotation (angle);
		beginVector = rotationGlobal.Apply (beginVector);
		endVector = rotationGlobal.Apply (endVector);
		
		// offset
		Geometry::Transformation2D transformationGlobal = Geometry::Transformation2D::CreateTranslation (Vector2D (element.object.pos.x, element.object.pos.y));
		Point2D beginPoint = transformationGlobal.Apply (Point2D (beginVector));
		Point2D endPoint = transformationGlobal.Apply (Point2D (endVector));
		
		double z = Utility::GetStoryLevel (element.object.head.floorInd) + element.object.level;
		os.Add (GridElement::begin, Objects::Point3D (beginPoint.x, beginPoint.y, z));
		os.Add (GridElement::end, Objects::Point3D (endPoint.x, endPoint.y, z));
	}

	os.Add (GridElement::markerText, markerText);
	os.Add (GridElement::isArc, isArc);
	os.Add (GridElement::arcAngle, arcAngle);

	return NoError;
}


GS::String GetGridElementData::GetName () const
{
	return GetGridElementCommandName;
}


} // namespace AddOnCommands
