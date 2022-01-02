#include "ModelInfo.hpp"
#include "FieldNames.hpp"


namespace Objects {


ModelInfo::Vertex::Vertex (double x, double y, double z) : 
	x (x),
	y (y),
	z (z)
{
}


GSErrCode ModelInfo::Vertex::Store (GS::ObjectState& os) const
{
	os.Add (Model::VertexXFieldName, x);
	os.Add (Model::VertexYFieldName, y);
	os.Add (Model::VertexZFieldName, z);

	return NoError;
}


GSErrCode ModelInfo::Vertex::Restore (const GS::ObjectState& os)
{
	os.Get (Model::VertexXFieldName, x);
	os.Get (Model::VertexYFieldName, y);
	os.Get (Model::VertexZFieldName, z);

	return NoError;
}


ModelInfo::Material::Material (const UMAT& aumat) :
	transparency (aumat.GetTransparency ()),
	ambientColor (aumat.GetSurfaceColor ()),
	emissionColor (aumat.GetEmissionColor ())
{
}


GSErrCode ModelInfo::Material::Store (GS::ObjectState& os) const
{
	os.Add (Model::AmbientColorFieldName, ambientColor);
	os.Add (Model::EmissionColorFieldName, emissionColor);
	os.Add (Model::TransparencyieldName, transparency);

	return NoError;
}


ModelInfo::Polygon::Polygon (const GS::Array<Int32>& pointIds, const UMAT& aumat) : 
	pointIds (pointIds),
	material (aumat)
{
}


GSErrCode ModelInfo::Polygon::Store (GS::ObjectState& os) const
{
	os.Add (Model::PointIdsFieldName, pointIds);
	os.Add (Model::MaterialFieldName, material);

	return NoError;
}


GSErrCode ModelInfo::Polygon::Restore (const GS::ObjectState& os)
{
	os.Get (Model::PointIdsFieldName, pointIds);

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


void ModelInfo::AddPolygon (const Polygon& polygon)
{
	polygons.Push (polygon);
}


void ModelInfo::AddPolygon (Polygon&& polygon)
{
	polygons.Push (std::move (polygon));
}


GSErrCode ModelInfo::Store (GS::ObjectState& os) const
{
	os.Add (Model::VerteciesFieldName, vertices);
	os.Add (Model::PolygonsFieldName, polygons);

	return NoError;
}


}