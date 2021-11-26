#ifndef OBJECTS_POLYLINE_HPP
#define OBJECTS_POLYLINE_HPP


#include "APIEnvir.h"
#include "ACAPinc.h"
#include "Point.hpp"


namespace Objects {


class PolylineSegment {
public:
	Point3D 	Start;
	Point3D 	End;
	double 		ArcAngle;

	PolylineSegment () = default;
	PolylineSegment (const API_Coord& start, const API_Coord& end, double angle = 0);
	PolylineSegment (const API_Coord3D& start, const API_Coord3D& end, double angle = 0);
	PolylineSegment (double x1, double y1, double z1, double x2, double y2, double z2, double angle = 0);

	GSErrCode		Restore (const GS::ObjectState& os);
	GSErrCode		Store (GS::ObjectState& os) const;
};


class Polyline {
private:
	GS::Array<PolylineSegment> mPolylineSegments;
	GS::Array<Point3D> mVertices;

private:
	void FillVertices ();

public:
	Polyline () = default;
	Polyline (const GS::Array<PolylineSegment>& PolylineSegments);

	int							VertexCount () const;
	int							ArcCount () const;
	const Point3D*				PointAt (int index) const;
	const PolylineSegment*		ArcAt (int index) const;
	bool						IsClosed () const;

	GSErrCode		Restore (const GS::ObjectState& os);
	GSErrCode		Store (GS::ObjectState& os) const;
};


}

#endif