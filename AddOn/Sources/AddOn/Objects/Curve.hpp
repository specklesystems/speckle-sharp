#ifndef SPEC_CURVE_HPP
#define SPEC_CURVE_HPP

#if !defined (ACExtension)
#define ACExtension
#endif

#include "Point.hpp"


namespace Objects {


class Curve {
public:
	Point3D 	Start;
	Point3D 	End;
	double 		ArcAngle;

	Curve ();
	Curve (const API_Coord& start, const API_Coord& end, double angle = 0);
	Curve (const API_Coord3D& start, const API_Coord3D& end, double angle = 0);
	Curve (double x1, double y1, double z1, double x2, double y2, double z2, double angle = 0);

	Point3D GetStartPoint () { return Start; }
	Point3D GetEndPoint () { return End; }
	double GetArcAngle () { return ArcAngle; }

	GSErrCode		Restore (const GS::ObjectState& os);
	GSErrCode		Store (GS::ObjectState& os) const;
};


}

#endif