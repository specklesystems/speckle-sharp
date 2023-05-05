#ifndef OBJECTS_POINT_HPP
#define OBJECTS_POINT_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"

namespace Objects
{

class Point3D {
public:
	double x;
	double y;
	double z;
	GS::UniString units = "m";

	Point3D ();
	Point3D (double x, double y, double z);
	Point3D (const API_Coord& coord, double z = 0.0);
	Point3D (const API_Coord3D& coord);
	Point3D (const Point3D& other);

	const API_Coord ToAPI_Coord () const;
	const API_Coord3D ToAPI_Coord3D () const;

	bool operator==(const Point3D& rhs) const;
	Point3D& operator=(const Point3D& other);

	GSErrCode Restore (const GS::ObjectState& os);
	GSErrCode Store (GS::ObjectState& os) const;
};

}

#endif