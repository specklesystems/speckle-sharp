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
	os.Add (Model::Polygons, polygons);
	os.Add (Model::Materials, materials);

	return NoError;
}


GSErrCode ModelInfo::Restore (const GS::ObjectState& os)
{
	os.Get (Model::Ids, ids);
	os.Get (Model::Vertices, vertices);
	os.Get (Model::Polygons, polygons);
	os.Get (Model::Materials, materials);
	os.Get (Model::Edges, edges);

	return NoError;
}
