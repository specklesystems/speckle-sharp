#include "Curve.hpp"
#include "ObjectState.hpp"


using namespace Objects;

static const char*		StartPointFieldName				= "startPoint";
static const char*		EndPointFieldName				= "endPoint";
static const char*		ArcAngleFieldName				= "arcAngle";


Curve::Curve()
{
}


Curve::Curve (const API_Coord& start, const API_Coord& end, double angle)
	: Start (start)
	, End (end)
	, ArcAngle (angle)
{
}


Curve::Curve (const API_Coord3D& start, const API_Coord3D& end, double angle)
	: Start (start)
	, End (end)
	, ArcAngle (angle)
{
}


Curve::Curve(double x1, double y1, double z1, double x2, double y2, double z2, double angle)
	: Start (x1, y1, z1)
	, End (x2, y2, z2)
	, ArcAngle (angle)
{
}


GSErrCode Curve::Restore (const GS::ObjectState& os)
{
	const GS::ObjectState& startPointOS = *os.Get (StartPointFieldName);
	Start.Restore (startPointOS);

	const GS::ObjectState& endPointOS = *os.Get (EndPointFieldName);
	End.Restore (endPointOS);
	
	os.Get (ArcAngleFieldName, ArcAngle);

	return NoError;
}


GSErrCode Curve::Store (GS::ObjectState& os) const
{
	auto& startPointOs = os.AddObject (StartPointFieldName);
	Start.Store (startPointOs);
	
	auto& endPointOs = os.AddObject (EndPointFieldName);
	End.Store (endPointOs);

	os.Add (ArcAngleFieldName, ArcAngle);

	return NoError;
}