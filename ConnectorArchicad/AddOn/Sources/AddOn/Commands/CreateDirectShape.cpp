#include "CreateDirectShape.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "ModelInfo.hpp"
#include "FieldNames.hpp"
#include "OnExit.hpp"
#include "AttributeManager.hpp"
using namespace FieldNames;


namespace AddOnCommands
{


GS::String CreateDirectShape::GetFieldName () const
{
	return FieldNames::DirectShapes;
}


GS::UniString CreateDirectShape::GetUndoableCommandName () const
{
	return "CreateSpeckleDirectShape";
}


GSErrCode CreateDirectShape::GetElementFromObjectState (const GS::ObjectState& os,
	API_Element& element,
	API_Element& /*elementMask*/,
	API_ElementMemo& memo,
	GS::UInt64& /*memoMask*/,
	API_SubElement** /*marker*/,
	AttributeManager& attributeManager,
	LibpartImportManager& /*libpartImportManager*/,
	GS::Array<GS::UniString>& log) const
{
	GSErrCode err = NoError;

	Utility::SetElementType (element.header, API_MorphID);
	err = Utility::GetBaseElementData (element, &memo, nullptr, log);
	if (err != NoError)
		return err;

	// get the mesh
	ModelInfo modelInfo;
	os.Get (Model::Model, modelInfo);

	void* bodyData = nullptr;
	ACAPI_Body_Create (nullptr, nullptr, &bodyData);
	if (bodyData == nullptr)
		return Error;

	const GS::Array<ModelInfo::Vertex>& vertices = modelInfo.GetVertices ();
	GS::Array<UInt32> bodyVertices;
	for (UInt32 i = 0; i < vertices.GetSize (); i++) {
		UInt32 bodyVertex = 0;
		ACAPI_Body_AddVertex (bodyData, API_Coord3D{vertices[i].GetX (), vertices[i].GetY (), vertices[i].GetZ ()}, bodyVertex);
		bodyVertices.Push (bodyVertex);
	}

	for (const auto& polygon : modelInfo.GetPolygons ()) {
		UInt32 bodyPolygon = 0;
		Int32 bodyEdge = 0;

		GS::Array<Int32> polygonEdges;
		const GS::Array<Int32>& pointIds = polygon.GetPointIds ();
		for (UInt32 i = 0; i < pointIds.GetSize (); i++) {
			Int32 start = i;
			Int32 end = i == pointIds.GetSize () - 1 ? 0 : i + 1;

			ACAPI_Body_AddEdge (bodyData, bodyVertices[pointIds[start]], bodyVertices[pointIds[end]], bodyEdge);
			polygonEdges.Push (bodyEdge);
		}

		API_OverriddenAttribute overrideMaterial;
		ModelInfo::Material material;
		if (NoError == modelInfo.GetMaterial (polygon.GetMaterial (), material)) {
			API_Attribute materialAttribute;
			err = attributeManager.GetMaterial (material, materialAttribute);
			if (NoError == err) {
				overrideMaterial.attributeIndex = materialAttribute.header.index;
				overrideMaterial.overridden = true;
			}
		}

		ACAPI_Body_AddPolygon (bodyData, polygonEdges, 0, overrideMaterial, bodyPolygon);
	}

	ACAPI_Body_Finish (bodyData, &memo.morphBody, &memo.morphMaterialMapTable);
	ACAPI_Body_Dispose (&bodyData);
	
	return NoError;
}


GS::String CreateDirectShape::GetName () const
{
	return CreateDirectShapeCommandName;
}


} // namespace AddOnCommands
