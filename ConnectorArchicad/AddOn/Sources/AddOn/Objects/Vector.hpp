#ifndef OBJECTS_VECTOR_HPP
#define OBJECTS_VECTOR_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"

namespace Objects
{

class Vector3D {
public:
	double x;
	double y;
	double z;

	Vector3D ();
	Vector3D (double x, double y, double z);
	Vector3D (const API_Vector& coord, double z = 0.0);
	Vector3D (const API_Vector3D& coord);
	Vector3D (const Vector3D& other);

	const API_Vector ToAPI_Vector () const;
	const API_Vector3D ToAPI_Vector3D () const;

	bool operator==(const Vector3D& rhs) const;
	Vector3D& operator=(const Vector3D& other);

	GSErrCode Restore (const GS::ObjectState& os);
	GSErrCode Store (GS::ObjectState& os) const;
};

}

#endif
