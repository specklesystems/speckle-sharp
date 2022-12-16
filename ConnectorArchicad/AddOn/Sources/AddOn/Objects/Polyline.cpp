#include "Polyline.hpp"
#include "ObjectState.hpp"


using namespace Objects;

static const char* StartPointFieldName = "startPoint";
static const char* EndPointFieldName = "endPoint";
static const char* ArcAngleFieldName = "arcAngle";
static const char* PolylineSegmentsFieldName = "polylineSegments";
static const char* ContourPolyFieldName = "contourPolyline";
static const char* HolePolylinesFieldName = "holePolylines";


PolylineSegment::PolylineSegment (const Point3D& start, const Point3D& end, double angle)
	: StartPoint (start)
	, EndPoint (end)
	, ArcAngle (angle)
{
}


GSErrCode PolylineSegment::Restore (const GS::ObjectState& os)
{
	os.Get (StartPointFieldName, StartPoint);
	os.Get (EndPointFieldName, EndPoint);
	os.Get (ArcAngleFieldName, ArcAngle);

	return NoError;
}


GSErrCode PolylineSegment::Store (GS::ObjectState& os) const
{
	os.Add (StartPointFieldName, StartPoint);
	os.Add (EndPointFieldName, EndPoint);
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
		Point3D sPoint = segment.StartPoint;
		Point3D ePoint = segment.EndPoint;
		bool sFound = false;
		bool eFound = false;
		for (UInt32 i = 1; i < mVertices.GetSize (); i++) {
			if (mVertices[i] == sPoint)
				sFound = true;
			if (mVertices[i] == ePoint)
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
		return &(mVertices[index]);
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

	return first.StartPoint == last.EndPoint;
}


GSErrCode Polyline::Restore (const GS::ObjectState& os)
{
	os.Get (PolylineSegmentsFieldName, mPolylineSegments);

	FillVertices ();

	return NoError;
}


GSErrCode Polyline::Store (GS::ObjectState& os) const
{
	os.Add (PolylineSegmentsFieldName, mPolylineSegments);

	return NoError;
}


ElementShape::ElementShape (const API_Polygon& outlinePoly, const API_ElementMemo& memo, double level)
{
	int nPolyArcs = outlinePoly.nArcs;
	int nSubPolys = outlinePoly.nSubPolys;

	API_Coord** coords = memo.coords;
	Int32** pends = memo.pends;
	API_PolyArc** parcs = memo.parcs;

	Int32 sIndex = 1;
	for (int i = 1; i <= nSubPolys; i++) {

		GS::Array<PolylineSegment> segments;

		for (int j = sIndex; j < (*(pends))[i]; j++) {

			API_Coord sPoint = (*(coords))[j];
			API_Coord ePoint = (*(coords))[j + 1];

			Point3D startPoint = Point3D (sPoint.x, sPoint.y, level);
			Point3D endPoint = Point3D (ePoint.x, ePoint.y, level);
			double arcAngle = 0;

			for (int k = 0; k < nPolyArcs; k++) {
				if ((*(parcs))[k].begIndex == j) {
					arcAngle = (*(parcs))[k].arcAngle;
					break;
				}
			}

			segments.Push (PolylineSegment (startPoint, endPoint, arcAngle));
		}

		if (i == 1) {
			mContourPoly = Polyline (segments);
		} else {
			mHoles.Push (Polyline (segments));
		}

		sIndex = (*(pends))[i] + 1;
	}
}


int ElementShape::SubpolyCount () const
{
	return (int) mHoles.GetSize () + 1;
}


int ElementShape::VertexCount () const
{
	int count = mContourPoly.VertexCount ();
	for (UInt32 i = 0; i < mHoles.GetSize (); i++) {
		count += mHoles[i].VertexCount ();
	}
	return count;
}


int ElementShape::ArcCount () const
{
	int count = mContourPoly.ArcCount ();
	for (UInt32 i = 0; i < mHoles.GetSize (); i++) {
		count += mHoles[i].ArcCount ();
	}
	return count;
}


double ElementShape::Level () const
{
	double z = 0;

	const Objects::Point3D* firstPoint = mContourPoly.PointAt (0);
	if (firstPoint != nullptr)
		z = firstPoint->Z;

	return z;
}


void ElementShape::SetToMemo (API_ElementMemo& memo)
{
	BMhKill ((GSHandle*) &memo.coords);
	BMhKill ((GSHandle*) &memo.pends);
	BMhKill ((GSHandle*) &memo.parcs);
	BMhKill ((GSHandle*) &memo.edgeIDs);
	BMhKill ((GSHandle*) &memo.vertexIDs);
	BMhKill ((GSHandle*) &memo.contourIDs);

	GS::Array<Polyline> polylines;

	polylines.Push (mContourPoly);
	polylines.Append (mHoles);

	GS::Int32 nSubPolys = GS::Int32 (polylines.GetSize ());
	GS::Int32 nCoords;
	GS::Int32 nArcs;

	memo.pends = (Int32**) BMAllocateHandle ((nSubPolys + 1) * sizeof (Int32), ALLOCATE_CLEAR, 0);
	(*(memo.pends))[0] = 0;
	int count = 0;
	for (UInt32 j = 0; j < polylines.GetSize (); j++) {
		(*(memo.pends))[j + 1] = (*(memo.pends))[j] + polylines[j].VertexCount ();
		count += polylines[j].ArcCount ();
	}

	nCoords = GS::Int32 ((*(memo.pends))[polylines.GetSize ()]);
	nArcs = GS::Int32 (count);

	memo.coords = reinterpret_cast<API_Coord**> (BMAllocateHandle ((nCoords + 1) * sizeof (API_Coord), ALLOCATE_CLEAR, 0));
	memo.vertexIDs = reinterpret_cast<UInt32**> (BMAllocateHandle ((nCoords + 1) * sizeof (Int32), ALLOCATE_CLEAR, 0));
	memo.parcs = reinterpret_cast<API_PolyArc**> (BMAllocateHandle (nArcs * sizeof (API_PolyArc), ALLOCATE_CLEAR, 0));
	((UInt32*) *memo.vertexIDs)[0] = nCoords;
	UInt32 coIndex = 1;
	UInt32 vId = 1;
	for (UInt32 j = 0; j < polylines.GetSize (); j++) {
		for (UInt32 k = 0; k < (UInt32) polylines[j].VertexCount (); ++k) {
			const Point3D* point = polylines[j].PointAt (k);
			if (point != nullptr) {
				(*(memo.coords))[coIndex].x = point->X;
				(*(memo.coords))[coIndex].y = point->Y;
				bool fId = false;
				for (UInt32 m = 1; m < coIndex; m++) {
					if ((*(memo.coords))[m].x == point->X && (*(memo.coords))[m].y == point->Y) {
						(*(memo.vertexIDs))[coIndex] = (*(memo.vertexIDs))[m];
						fId = true;
						break;
					}
				}
				if (!fId) {
					(*(memo.vertexIDs))[coIndex] = vId;
					vId++;
				}
			}
			coIndex++;
		}
	}
	(*(memo.vertexIDs))[0] = vId - 1;
	UInt32 iArc = 0;
	UInt32 offset = 0;
	for (UInt32 l = 0; l < polylines.GetSize (); l++) {
		for (UInt32 n = 0; n < (UInt32) polylines[l].ArcCount (); n++) {
			const PolylineSegment* arc = polylines[l].ArcAt (n);
			if (arc != nullptr) {
				UInt32 beg = 0, end = 0;
				for (UInt32 k = 0; k < (UInt32) polylines[l].VertexCount () - 1; ++k) {
					const Point3D* point = polylines[l].PointAt (k);
					if (beg == 0 && point != nullptr && *(point) == arc->StartPoint)
						beg = k + 1;
					point = polylines[l].PointAt (k + 1);
					if (end == 0 && point != nullptr && *(point) == arc->EndPoint)
						end = k + 2;
				}
				if (beg != 0 && end != 0) {
					(*memo.parcs)[iArc].begIndex = beg + offset;
					(*memo.parcs)[iArc].endIndex = end + offset;
					(*memo.parcs)[iArc].arcAngle = arc->ArcAngle;
					++iArc;
				}
			}
		}
		offset += GS::Int32 (polylines[l].VertexCount ());
	}
}


GSErrCode ElementShape::Restore (const GS::ObjectState& os)
{
	os.Get (ContourPolyFieldName, mContourPoly);

	if (os.Contains (HolePolylinesFieldName)) {
		os.Get (HolePolylinesFieldName, mHoles);
	}

	return NoError;
}


GSErrCode ElementShape::Store (GS::ObjectState& os) const
{
	os.Add (ContourPolyFieldName, mContourPoly);

	if (!mHoles.IsEmpty ())
		os.Add (HolePolylinesFieldName, mHoles);

	return NoError;
}