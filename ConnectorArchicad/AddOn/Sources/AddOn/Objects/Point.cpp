#include "Point.hpp"
#include "ObjectState.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"

using namespace Objects;


Point3D::Point3D ()
	: x (0.0), y (0.0), z (0.0)
{
}

Point3D::Point3D (double x, double y, double z)
	: x (x), y (y), z (z)
{
}

Point3D::Point3D (const API_Coord& coord, double z)
	: x (coord.x), y (coord.y), z (z)
{
}

Point3D::Point3D (const API_Coord3D& coord)
	: x (coord.x), y (coord.y), z (coord.z)
{
}

Point3D::Point3D (const Point3D& other)
	: x (other.x), y (other.y), z (other.z)
{
}

const API_Coord Point3D::ToAPI_Coord () const
{
	API_Coord coord;
	coord.x = x;
	coord.y = y;

	return coord;
}

const API_Coord3D Point3D::ToAPI_Coord3D () const
{
	API_Coord3D coord;
	coord.x = x;
	coord.y = y;
	coord.z = z;

	return coord;
}

bool Point3D::operator==(const Point3D& rhs) const
{
	if (fabs (x - rhs.x) < EPS && fabs (y - rhs.y) < EPS && fabs (z - rhs.z) < EPS)
		return true;
	else
		return false;
}

Point3D& Point3D::operator=(const Point3D& other)
{
	if (this == &other) {
		return *this;
	}
	x = other.x;
	y = other.y;
	z = other.z;

	return *this;
}

GSErrCode Point3D::Restore (const GS::ObjectState& os)
{
	os.Get (FieldNames::Point::X, x);
	os.Get (FieldNames::Point::Y, y);
	os.Get (FieldNames::Point::Z, z);
	os.Get (FieldNames::Point::Units, units);

	return NoError;
}

GSErrCode Point3D::Store (GS::ObjectState& os) const
{
	os.Add (FieldNames::Point::X, x);
	os.Add (FieldNames::Point::Y, y);
	os.Add (FieldNames::Point::Z, z);
	os.Add (FieldNames::Point::Units, units);

	return NoError;
}