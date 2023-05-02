#include "Polyline.hpp"
#include "ObjectState.hpp"
#include "FieldNames.hpp"


using namespace Objects;

static const char* StartPointFieldName = "startPoint";
static const char* EndPointFieldName = "endPoint";
static const char* ArcAngleFieldName = "arcAngle";
static const char* BodyFlagFieldName = "bodyFlag";
static const char* PolylineSegmentsFieldName = "polylineSegments";
static const char* ContourPolyFieldName = "contourPolyline";
static const char* HolePolylinesFieldName = "holePolylines";


PolylineSegment::PolylineSegment (const Point3D& start, const Point3D& end, double angle, GS::Optional<bool> bodyFlag /*= GS::NoValue*/)
	: startPoint (start)
	, endPoint (end)
	, arcAngle (angle)
	, bodyFlag (bodyFlag)
{
}


GSErrCode PolylineSegment::Restore (const GS::ObjectState& os)
{
	os.Get (StartPointFieldName, startPoint);
	os.Get (EndPointFieldName, endPoint);
	os.Get (ArcAngleFieldName, arcAngle);

	if (os.Contains (BodyFlagFieldName)) {
		bool bodyFlagIn = false;
		os.Get (BodyFlagFieldName, bodyFlagIn);
		bodyFlag = bodyFlagIn;
	}

	return NoError;
}


GSErrCode PolylineSegment::Store (GS::ObjectState& os) const
{
	os.Add (StartPointFieldName, startPoint);
	os.Add (EndPointFieldName, endPoint);
	os.Add (ArcAngleFieldName, arcAngle);
	if (bodyFlag.HasValue ())
		os.Add (BodyFlagFieldName, bodyFlag.Get ());

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
		Point3D sPoint = segment.startPoint;
		Point3D ePoint = segment.endPoint;
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
		if (segment.arcAngle != 0) {
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


const PolylineSegment* Polyline::SegmentAt (int index) const
{
	if (index < (int) mPolylineSegments.GetSize () && index > -1)
		return &(mPolylineSegments[index]);

	return nullptr;
}


const PolylineSegment* Polyline::ArcAt (int index) const
{
	int count = 0;

	if (index < ArcCount () && index > -1) {
		for (UInt32 i = 0; i < mPolylineSegments.GetSize (); i++) {
			if (mPolylineSegments[i].arcAngle != 0) {
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

	return first.startPoint == last.endPoint;
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


ElementShape::ElementShape (const API_Polygon& outlinePoly, const API_ElementMemo& memo, MemoPolygonType memoPolygonType, double level, UInt32 idx /*= 0*/)
{
	int nPolyArcs = outlinePoly.nArcs;
	int nSubPolys = outlinePoly.nSubPolys;

	API_Coord** coords = memo.coords;
	Int32** pends = memo.pends;
	API_PolyArc** parcs = memo.parcs;
	bool** bodyFlags = nullptr;

	if (memoPolygonType == MemoAdditionalPolygon) {
		coords = memo.additionalPolyCoords;
		pends = memo.additionalPolyPends;
		parcs = memo.additionalPolyParcs;
	} else if (memoPolygonType == MemoShellPolygon1) {
		coords = memo.shellShapes[0].coords;
		pends = memo.shellShapes[0].pends;
		parcs = memo.shellShapes[0].parcs;
		bodyFlags = memo.shellShapes[0].bodyFlags;
	} else if (memoPolygonType == MemoShellPolygon2) {
		coords = memo.shellShapes[1].coords;
		pends = memo.shellShapes[1].pends;
		parcs = memo.shellShapes[1].parcs;
		bodyFlags = memo.shellShapes[1].bodyFlags;
	} else if (memoPolygonType == MemoShellContour) {
		if (memo.shellContours == nullptr)
			return;
		coords = memo.shellContours[idx].coords;
		pends = memo.shellContours[idx].pends;
		parcs = memo.shellContours[idx].parcs;
	}

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

			if (bodyFlags != nullptr) {
				// segment's start point defines the segment's bodyFlag
				segments.Push (PolylineSegment (startPoint, endPoint, arcAngle, (*bodyFlags)[j]));
			} else {
				segments.Push (PolylineSegment (startPoint, endPoint, arcAngle));
			}
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
		z = firstPoint->z;

	return z;
}


void ElementShape::SetToMemo (API_ElementMemo& memo, MemoPolygonType memoPolygonType, UInt32 idx /*= 0*/)
{
	API_Coord*** coords = &memo.coords;
	Int32*** pends = &memo.pends;
	API_PolyArc*** parcs = &memo.parcs;
	UInt32*** vertexIDs = &memo.vertexIDs;
	UInt32*** edgeIDs = &memo.edgeIDs;
	UInt32*** contourIDs = &memo.contourIDs;
	bool*** bodyFlags = nullptr;

	if (memoPolygonType == MemoAdditionalPolygon) {
		coords = &memo.additionalPolyCoords;
		pends = &memo.additionalPolyPends;
		parcs = &memo.additionalPolyParcs;
		vertexIDs = &memo.additionalPolyVertexIDs;
		edgeIDs = &memo.additionalPolyEdgeIDs;
		contourIDs = &memo.additionalPolyContourIDs;
	} else if (memoPolygonType == MemoShellPolygon1) {
		coords = &memo.shellShapes[0].coords;
		pends = &memo.shellShapes[0].pends;
		parcs = &memo.shellShapes[0].parcs;
		vertexIDs = &memo.shellShapes[0].vertexIDs;
		edgeIDs = &memo.shellShapes[0].edgeIDs;
		bodyFlags = &memo.shellShapes[0].bodyFlags;
	} else if (memoPolygonType == MemoShellPolygon2) {
		coords = &memo.shellShapes[1].coords;
		pends = &memo.shellShapes[1].pends;
		parcs = &memo.shellShapes[1].parcs;
		vertexIDs = &memo.shellShapes[1].vertexIDs;
		edgeIDs = &memo.shellShapes[1].edgeIDs;
		bodyFlags = &memo.shellShapes[1].bodyFlags;
	} else if (memoPolygonType == MemoShellContour) {
		coords = &memo.shellContours[idx].coords;
		pends = &memo.shellContours[idx].pends;
		parcs = &memo.shellContours[idx].parcs;
		vertexIDs = &memo.shellContours[idx].vertexIDs;
		edgeIDs = &memo.shellContours[idx].edgeIDs;
		contourIDs = &memo.shellContours[idx].contourIDs;
	}

	BMhKill ((GSHandle*) coords);
	BMhKill ((GSHandle*) pends);
	BMhKill ((GSHandle*) parcs);
	BMhKill ((GSHandle*) edgeIDs);
	BMhKill ((GSHandle*) vertexIDs);
	BMhKill ((GSHandle*) contourIDs);
	BMhKill ((GSHandle*) bodyFlags);

	GS::Array<Polyline> polylines;

	polylines.Push (mContourPoly);
	polylines.Append (mHoles);

	GS::Int32 nSubPolys = GS::Int32 (polylines.GetSize ());
	GS::Int32 nCoords = 0;
	GS::Int32 nArcs = 0;

	*pends = (Int32**) BMAllocateHandle ((nSubPolys + 1) * sizeof (Int32), ALLOCATE_CLEAR, 0);
	(*(*pends))[0] = 0;
	int count = 0;
	for (UInt32 j = 0; j < polylines.GetSize (); j++) {
		(*(*pends))[j + 1] = (*(*pends))[j] + polylines[j].VertexCount ();
		count += polylines[j].ArcCount ();
	}

	nCoords = GS::Int32 ((*(*pends))[polylines.GetSize ()]);
	nArcs = GS::Int32 (count);

	*coords = reinterpret_cast<API_Coord**> (BMAllocateHandle ((nCoords + 1) * sizeof (API_Coord), ALLOCATE_CLEAR, 0));
	*vertexIDs = reinterpret_cast<UInt32**> (BMAllocateHandle ((nCoords + 1) * sizeof (Int32), ALLOCATE_CLEAR, 0));
	*parcs = reinterpret_cast<API_PolyArc**> (BMAllocateHandle (nArcs * sizeof (API_PolyArc), ALLOCATE_CLEAR, 0));
	if (bodyFlags != nullptr)
		*bodyFlags = reinterpret_cast<bool**> (BMAllocateHandle ((nCoords + 1) * sizeof (bool), ALLOCATE_CLEAR, 0));

	((UInt32*) **vertexIDs)[0] = nCoords;
	UInt32 coIndex = 1;
	UInt32 vId = 1;
	for (UInt32 j = 0; j < polylines.GetSize (); j++) {
		for (UInt32 k = 0; k < (UInt32) polylines[j].VertexCount (); ++k) {
			const Point3D* point = polylines[j].PointAt (k);
			if (point != nullptr) {
				(*(*coords))[coIndex].x = point->x;
				(*(*coords))[coIndex].y = point->y;
				bool fId = false;
				for (UInt32 m = 1; m < coIndex; m++) {
					if ((*(*coords))[m].x == point->x && (*(*coords))[m].y == point->y) {
						(*(*vertexIDs))[coIndex] = (*(*vertexIDs))[m];
						fId = true;
						break;
					}
				}
				if (!fId) {
					(*(*vertexIDs))[coIndex] = vId;
					vId++;
				}
			}

			if (bodyFlags != nullptr) {
				// set the start point's body flag
				const PolylineSegment* segment = polylines[j].SegmentAt (k);
				if (segment != nullptr && segment->bodyFlag.HasValue () && segment->bodyFlag.Get () == true) {
					(*(*bodyFlags))[coIndex] = true;
				}
			}

			coIndex++;
		}
	}
	(*(*vertexIDs))[0] = vId - 1;

	UInt32 iArc = 0;
	UInt32 offset = 0;
	for (UInt32 l = 0; l < polylines.GetSize (); l++) {
		for (UInt32 n = 0; n < (UInt32) polylines[l].ArcCount (); n++) {
			const PolylineSegment* arc = polylines[l].ArcAt (n);
			if (arc != nullptr) {
				UInt32 beg = 0, end = 0;
				for (UInt32 k = 0; k < (UInt32) polylines[l].VertexCount () - 1; ++k) {
					const Point3D* point = polylines[l].PointAt (k);
					if (beg == 0 && point != nullptr && *(point) == arc->startPoint)
						beg = k + 1;
					point = polylines[l].PointAt (k + 1);
					if (end == 0 && point != nullptr && *(point) == arc->endPoint)
						end = k + 2;
				}
				if (beg != 0 && end != 0) {
					(**parcs)[iArc].begIndex = beg + offset;
					(**parcs)[iArc].endIndex = end + offset;
					(**parcs)[iArc].arcAngle = arc->arcAngle;
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
