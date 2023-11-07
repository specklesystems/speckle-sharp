#include "ModelInfo.hpp"
#include "FieldNames.hpp"
using namespace FieldNames;


ModelInfo::Vertex::Vertex (double x, double y, double z) :
	x (x),
	y (y),
	z (z)
{
}


GSErrCode ModelInfo::Vertex::Store (GS::ObjectState& os) const
{
	os.Add (Model::VertexX, x);
	os.Add (Model::VertexY, y);
	os.Add (Model::VertexZ, z);

	return NoError;
}


GSErrCode ModelInfo::Vertex::Restore (const GS::ObjectState& os)
{
	os.Get (Model::VertexX, x);
	os.Get (Model::VertexY, y);
	os.Get (Model::VertexZ, z);

	return NoError;
}


ModelInfo::Material::Material (const UMAT& aumat) :
	name (aumat.GetName ()),
	transparency (aumat.GetTransparency ()),
	ambientColor (aumat.GetSurfaceColor ()),
	emissionColor (aumat.GetEmissionColor ())
{
}


ModelInfo::Material::Material (GS::UniString& name, short transparency, GS_RGBColor& ambientColor, GS_RGBColor& emissionColor) :
	name (name),
	transparency (transparency),
	ambientColor (ambientColor),
	emissionColor (emissionColor)
{
}


GSErrCode ModelInfo::Material::Store (GS::ObjectState& os) const
{
	os.Add (Model::MaterialName, name);
	os.Add (Model::AmbientColor, ambientColor);
	os.Add (Model::EmissionColor, emissionColor);
	os.Add (Model::Transparency, transparency);

	return NoError;
}


GSErrCode ModelInfo::Material::Restore (const GS::ObjectState& os)
{
	os.Get (Model::MaterialName, name);
	os.Get (Model::AmbientColor, ambientColor);
	os.Get (Model::EmissionColor, emissionColor);
	os.Get (Model::Transparency, transparency);

	return NoError;
}


ModelInfo::EdgeId::EdgeId (Int32 vertexId1, Int32 vertexId2) :
	vertexId1 (vertexId1), vertexId2 (vertexId2)
{
}


bool ModelInfo::EdgeId::operator== (ModelInfo::EdgeId otherEdgeId) const
{
	return (vertexId1 == otherEdgeId.vertexId1 && vertexId2 == otherEdgeId.vertexId2) ||
	(vertexId1 == otherEdgeId.vertexId2 && vertexId2 == otherEdgeId.vertexId1);
}


ULong ModelInfo::EdgeId::GenerateHashValue (void) const
{
	return GS::CalculateHashValue (GS::Max(vertexId1, vertexId2), GS::Min(vertexId1, vertexId2));
}


ModelInfo::EdgeData::EdgeData () :
	edgeStatus (HiddenEdge), polygonId1 (InvalidPolygonId), polygonId2 (InvalidPolygonId)
{
}


ModelInfo::EdgeData::EdgeData (EdgeStatus edgeStatus, Int32 polygonId1 /* = InvalidPolygonId */, Int32 polygonId2 /* = InvalidPolygonId */)
	: edgeStatus (edgeStatus), polygonId1 (polygonId1), polygonId2 (polygonId2)
{
}


ModelInfo::Polygon::Polygon (const GS::Array<Int32>& pointIds, UInt32 material) :
	pointIds (pointIds),
	material (material)
{
}


GSErrCode ModelInfo::Polygon::Store (GS::ObjectState& os) const
{
	os.Add (Model::PointIds, pointIds);
	os.Add (Model::Material, material);

	return NoError;
}


GSErrCode ModelInfo::Polygon::Restore (const GS::ObjectState& os)
{
	os.Get (Model::PointIds, pointIds);
	os.Get (Model::Material, material);

	return NoError;
}


void ModelInfo::AddVertex (const Vertex& vertex)
{
	vertices.Push (vertex);
}


void ModelInfo::AddVertex (Vertex&& vertex)
{
	vertices.Push (std::move (vertex));
}


void ModelInfo::AddEdge (const EdgeId& edgeId, const EdgeData& edgeData)
{
	if (edges.ContainsKey (edgeId))
		edges[edgeId] = edgeData;
	else
		edges.Add (edgeId, edgeData);
}


void ModelInfo::AddEdge (EdgeId&& edgeId, EdgeData&& edgeData)
{
	if (edges.ContainsKey (edgeId))
		edges[edgeId] = edgeData;
	else
		edges.Add (edgeId, edgeData);
}


void ModelInfo::AddPolygon (const Polygon& polygon)
{
	polygons.Push (polygon);
}


void ModelInfo::AddPolygon (Polygon&& polygon)
{
	polygons.Push (std::move (polygon));
}


void ModelInfo::AddId (const GS::UniString& id)
{
	ids.Push (id);
}


void ModelInfo::AddId (GS::UniString&& id)
{
	ids.Push (std::move (id));
}


UInt32 ModelInfo::AddMaterial (const UMAT& material)
{
	UIndex idx = materials.FindFirst ([&material] (const ModelInfo::Material& cachedMaterial) { return material.GetName () == cachedMaterial.GetName (); });
	if (idx != MaxUIndex) {
		return idx;
	}

	materials.PushNew (material);
	return materials.GetSize () - 1;
}


GSErrCode ModelInfo::GetMaterial (const UInt32 materialIndex, ModelInfo::Material& material) const
{
	if (materialIndex >= materials.GetSize ())
		return Error;

	material = materials[materialIndex];
	return NoError;
}


GSErrCode ModelInfo::Store (GS::ObjectState& os) const
{
	os.Add (Model::Vertices, vertices);
	
	GS::Array<GS::ObjectState> edgeArray;

	for (auto edge : edges)
	{
		// skip hidden edges
		if (edge.value->edgeStatus == HiddenEdge)
			continue;
		
		GS::ObjectState osEdge;
		
		osEdge.Add (Model::PointId1, edge.key->vertexId1);
		osEdge.Add (Model::PointId2, edge.key->vertexId2);

		if (edge.value->polygonId1 != EdgeData::InvalidPolygonId)
			osEdge.Add (Model::PolygonId1, edge.value->polygonId1);

		if (edge.value->polygonId2 != EdgeData::InvalidPolygonId)
			osEdge.Add (Model::PolygonId2, edge.value->polygonId2);

		GS::UniString edgeStatusName (Model::HiddenEdgeValueName);
		if (edge.value->edgeStatus == SmoothEdge)
			edgeStatusName = Model::SmoothEdgeValueName;
		else if (edge.value->edgeStatus == VisibleEdge)
			edgeStatusName = Model::VisibleEdgeValueName;
	
		osEdge.Add (Model::EdgeStatus, edgeStatusName);
		
		edgeArray.Push (osEdge);
	}

	os.Add (Model::Edges, edgeArray);

	os.Add (Model::Polygons, polygons);
	os.Add (Model::Materials, materials);

	return NoError;
}


GSErrCode ModelInfo::Restore (const GS::ObjectState& os)
{
	os.Get (Model::Ids, ids);
	os.Get (Model::Vertices, vertices);

	GS::Array<GS::ObjectState> edgeArray;
	os.Get (Model::Edges, edgeArray);
	
	for (GS::ObjectState osEdge : edgeArray)
	{
		if (!osEdge.Contains (Model::PointId1) || !osEdge.Contains (Model::PointId2) || !osEdge.Contains (Model::EdgeStatus))
			continue;
		
		Int32 pointId1 (0), pointId2 (0);
		osEdge.Get (Model::PointId1, pointId1);
		osEdge.Get (Model::PointId2, pointId2);

		EdgeId edgeId (pointId1, pointId2);
		
		EdgeStatus edgeStatus (HiddenEdge);
		GS::UniString edgeStatusName;
		osEdge.Get (Model::EdgeStatus, edgeStatusName);
		if (edgeStatusName == Model::SmoothEdgeValueName)
			edgeStatus = SmoothEdge;
		else if (edgeStatusName == Model::VisibleEdgeValueName)
			edgeStatus = VisibleEdge;
	
		EdgeData edgeData (edgeStatus);
		
		edges.Add(edgeId, edgeData);
	}
	
	os.Get (Model::Polygons, polygons);
	os.Get (Model::Materials, materials);

	return NoError;
}
