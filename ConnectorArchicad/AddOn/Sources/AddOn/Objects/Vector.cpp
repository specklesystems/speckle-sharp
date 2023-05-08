#include "Vector.hpp"
#include "ObjectState.hpp"
#include "RealNumber.h"

using namespace Objects;

static const char* XFieldName = "x";
static const char* YFieldName = "y";
static const char* ZFieldName = "z";

Vector3D::Vector3D ()
	: x (0.0), y (0.0), z (0.0)
{
}


Vector3D::Vector3D (double x, double y, double z)
	: x (x), y (y), z (z)
{
}


Vector3D::Vector3D (const API_Vector& coord, double z)
	: x (coord.x), y (coord.y), z (z)
{
}


Vector3D::Vector3D (const API_Vector3D& coord)
	: x (coord.x), y (coord.y), z (coord.z)
{
}


Vector3D::Vector3D (const Vector3D& other)
	: x (other.x), y (other.y), z (other.z)
{
}


const API_Vector Vector3D::ToAPI_Vector () const
{
	API_Coord coord;
	coord.x = x;
	coord.y = y;

	return coord;
}


const API_Vector3D Vector3D::ToAPI_Vector3D () const
{
	API_Coord3D coord;
	coord.x = x;
	coord.y = y;
	coord.z = z;

	return coord;
}


bool Vector3D::operator==(const Vector3D& rhs) const
{
	if (fabs (x - rhs.x) < EPS && fabs (y - rhs.y) < EPS && fabs (z - rhs.z) < EPS)
		return true;
	else
		return false;
}


Vector3D& Vector3D::operator=(const Vector3D& other)
{
	if (this == &other)
		return *this;

	x = other.x;
	y = other.y;
	z = other.z;

	return *this;
}


GSErrCode Vector3D::Restore (const GS::ObjectState& os)
{
	os.Get (XFieldName, x);
	os.Get (YFieldName, y);
	os.Get (ZFieldName, z);

	return NoError;
}


GSErrCode Vector3D::Store (GS::ObjectState& os) const
{
	os.Add (XFieldName, x);
	os.Add (YFieldName, y);
	os.Add (ZFieldName, z);

	return NoError;
}
