#include "Polyline.hpp"
#include "ObjectState.hpp"


using namespace Objects;

static const char*		StartPointFieldName				= "startPoint";
static const char*		EndPointFieldName				= "endPoint";
static const char*		ArcAngleFieldName				= "arcAngle";
static const char*		PolylineSegmentsFieldName		= "polylineSegments";


PolylineSegment::PolylineSegment (const API_Coord& start, const API_Coord& end, double angle)
	: Start (start)
	, End (end)
	, ArcAngle (angle)
{
}


PolylineSegment::PolylineSegment (const API_Coord3D& start, const API_Coord3D& end, double angle)
	: Start (start)
	, End (end)
	, ArcAngle (angle)
{
}


PolylineSegment::PolylineSegment (double x1, double y1, double z1, double x2, double y2, double z2, double angle)
	: Start (x1, y1, z1)
	, End (x2, y2, z2)
	, ArcAngle (angle)
{
}


GSErrCode PolylineSegment::Restore (const GS::ObjectState& os)
{
	const GS::ObjectState& startPointOS = *os.Get (StartPointFieldName);
	Start.Restore (startPointOS);

	const GS::ObjectState& endPointOS = *os.Get (EndPointFieldName);
	End.Restore (endPointOS);
	
	os.Get (ArcAngleFieldName, ArcAngle);

	return NoError;
}


GSErrCode PolylineSegment::Store (GS::ObjectState& os) const
{
	auto& startPointOs = os.AddObject (StartPointFieldName);
	Start.Store (startPointOs);
	
	auto& endPointOs = os.AddObject (EndPointFieldName);
	End.Store (endPointOs);

	os.Add (ArcAngleFieldName, ArcAngle);

	return NoError;
}


Polyline::Polyline (const GS::Array<PolylineSegment>& curveSegments)
	: mPolylineSegments (curveSegments)
{
	FillVertices ();
}


void Polyline::FillVertices ()
{
	mVertices.Clear ();
	for (const PolylineSegment& segment : mPolylineSegments) {
		Point3D sPoint = segment.Start;
		Point3D ePoint = segment.End;
		bool sFound = false;
		bool eFound = false;
		for (const Point3D& point : mVertices) {
			if (point == sPoint)
				sFound = true;
			if (point == ePoint)
				eFound = true;
		}
		if (!sFound)
			mVertices.Push (sPoint);
		if (!eFound)
			mVertices.Push (ePoint);
	}
}


int Polyline::VertexCount () const
{
	return (int) mVertices.GetSize ();
}


int Polyline::ArcCount () const
{
	int count = 0;
	for (const PolylineSegment& segment : mPolylineSegments) {
		if (segment.ArcAngle != 0) {
			count++;
		}
	}
	return count;
}


const Point3D* Polyline::PointAt (int index) const
{
	if (index < VertexCount () && index > -1) {
		return &(mVertices [index]);
	}
	return nullptr;
}


const PolylineSegment* Polyline::ArcAt (int index) const
{
	int count = 0;

	if (index < ArcCount () && index > -1) {
		for (UInt32 i = 0; i < mPolylineSegments.GetSize (); i++) {
			if (mPolylineSegments[i].ArcAngle != 0) {
				if (count == index) {
					return &(mPolylineSegments[i]);
				}
				count++;
			}
		}
	}
	return nullptr;
}


bool Polyline::IsClosed () const
{
	PolylineSegment first = mPolylineSegments.GetFirst ();
	PolylineSegment last = mPolylineSegments.GetLast ();

	return first.Start == last.End;
}

		
GSErrCode Polyline::Restore (const GS::ObjectState& os)
{
	GS::Array<GS::ObjectState> curvesOS;
	os.Get (PolylineSegmentsFieldName, curvesOS);
	for (GS::ObjectState curveOS : curvesOS) {
		PolylineSegment segment;
		segment.Restore (curveOS);
		mPolylineSegments.Push (segment);
	}

	FillVertices ();

	return NoError;
}


GSErrCode Polyline::Store (GS::ObjectState& os) const
{
	const auto& listAdder = os.AddList<GS::ObjectState> (PolylineSegmentsFieldName);
	for (const PolylineSegment& segment : mPolylineSegments) {
		GS::ObjectState segmentOs;
		segment.Store (segmentOs);
		listAdder(segmentOs);
	}

	return NoError;
}