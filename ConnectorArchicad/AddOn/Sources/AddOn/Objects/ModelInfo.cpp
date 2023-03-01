#include "ModelInfo.hpp"
#include "FieldNames.hpp"


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
	os.Add (Model::MaterialNameFieldName, name);
	os.Add (Model::AmbientColorFieldName, ambientColor);
	os.Add (Model::EmissionColorFieldName, emissionColor);
	os.Add (Model::TransparencyFieldName, transparency);

	return NoError;
}


GSErrCode ModelInfo::Material::Restore (const GS::ObjectState& os)
{
	os.Get (Model::MaterialNameFieldName, name);
	os.Get (Model::AmbientColorFieldName, ambientColor);
	os.Get (Model::EmissionColorFieldName, emissionColor);
	os.Get (Model::TransparencyFieldName, transparency);

	return NoError;
}


ModelInfo::Polygon::Polygon (const GS::Array<Int32>& pointIds, UInt32 material) :
	pointIds (pointIds),
	material (material)
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
	os.Get (Model::MaterialFieldName, material);

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
	os.Add (Model::VerticesFieldName, vertices);
	os.Add (Model::PolygonsFieldName, polygons);
	os.Add (Model::MaterialsFieldName, materials);

	return NoError;
}


GSErrCode ModelInfo::Restore (const GS::ObjectState& os)
{
	os.Get (Model::IdsFieldName, ids);
	os.Get (Model::VerticesFieldName, vertices);
	os.Get (Model::PolygonsFieldName, polygons);
	os.Get (Model::MaterialsFieldName, materials);
	os.Get (Model::EdgesFieldName, edges);

	return NoError;
}
