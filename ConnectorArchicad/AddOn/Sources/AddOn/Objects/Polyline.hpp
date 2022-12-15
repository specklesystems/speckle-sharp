#ifndef OBJECTS_POLYLINE_HPP
#define OBJECTS_POLYLINE_HPP


#include "APIEnvir.h"
#include "ACAPinc.h"
#include "Point.hpp"


namespace Objects {


class PolylineSegment {
public:
	Point3D 	StartPoint;
	Point3D 	EndPoint;
	double 		ArcAngle;

	PolylineSegment () = default;
	PolylineSegment (const Point3D& start, const Point3D& end, double angle = 0);

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
	Polyline (const GS::Array<PolylineSegment>& polylineSegments);

	int							VertexCount () const;
	int							ArcCount () const;
	const Point3D* PointAt (int index) const;
	const PolylineSegment* ArcAt (int index) const;
	bool						IsClosed () const;

	GSErrCode		Restore (const GS::ObjectState& os);
	GSErrCode		Store (GS::ObjectState& os) const;
};


class ElementShape {
private:
	Polyline mContourPoly;
	GS::Array<Polyline> mHoles;

public:
	ElementShape () = default;
	ElementShape (const API_Polygon& outlinePoly, const API_ElementMemo& memo, double level = 0);

	void SetToMemo (API_ElementMemo& memo);

	int SubpolyCount () const;
	int VertexCount () const;
	int ArcCount () const;

	double Level () const;

	GSErrCode		Restore (const GS::ObjectState& os);
	GSErrCode		Store (GS::ObjectState& os) const;
};

}

#endif