#include "CreateDirectShape.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "ModelInfo.hpp"
#include "FieldNames.hpp"


namespace AddOnCommands {


static GSErrCode FindAndDeleteOldElement (const API_Guid& elementId)
{
	API_Elem_Head head {};
	head.guid = elementId;

	GSErrCode err = ACAPI_Element_GetHeader (&head);
	if (err == APIERR_BADID) {
		return NoError;
	}

	if (err != NoError) {
		return APIERR_CANCEL;
	}

	if (head.typeID != API_MorphID) {
		return APIERR_CANCEL;
	}

	return ACAPI_Element_Delete ({ elementId });
}


static GS::Optional<API_Guid> CreateElement (const API_Guid& elementId, const GS::Array<ModelInfo::Vertex>& vertices, const GS::Array<ModelInfo::Polygon> polygons)
{
	GSErrCode err = FindAndDeleteOldElement (elementId);
	if (err != NoError) {
		return GS::NoValue;
	}

	API_Element element = {};
	element.header.typeID = API_MorphID;
	err = ACAPI_Element_GetDefaults (&element, nullptr);
	if (err != NoError) {
		return GS::NoValue;
	}
	element.header.guid = elementId;

	void* bodyData = nullptr;
	ACAPI_Body_Create (nullptr, nullptr, &bodyData);
	if (bodyData == nullptr) {
		return GS::NoValue;
	}

	GS::Array<UInt32> bodyVertices;
	for (UInt32 i = 0; i < vertices.GetSize (); i++) {
		UInt32 bodyVertex = 0;
		ACAPI_Body_AddVertex (bodyData, API_Coord3D { vertices[i].GetX (), vertices[i].GetY (), vertices[i].GetZ () }, bodyVertex);
		bodyVertices.Push (bodyVertex);
	}

	for (const auto& polygon : polygons) {
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

		ACAPI_Body_AddPolygon (bodyData, polygonEdges, 0 , API_OverriddenAttribute {}, bodyPolygon);
	}

	API_ElementMemo memo = {};
	ACAPI_Body_Finish (bodyData, &memo.morphBody, &memo.morphMaterialMapTable);
	ACAPI_Body_Dispose (&bodyData);

	// create the morph element
	err = ACAPI_Element_Create (&element, &memo);
	if (err != NoError) {
		return GS::NoValue;
	}

	ACAPI_DisposeElemMemoHdls (&memo);

	return element.header.guid;
}


static GS::Optional<API_Guid> CreateElement (const GS::ObjectState& elementModelOs)
{
	try {
		GS::UniString id;
		elementModelOs.Get (ElementIdFieldName, id);

		const GS::ObjectState* modelOs = elementModelOs.Get (Model::ModelFieldName);
		if (modelOs == nullptr) {
			return GS::NoValue;
		}

		GS::Array<ModelInfo::Vertex> vertices;
		modelOs->Get (Model::VerteciesFieldName, vertices);

		GS::Array<ModelInfo::Polygon> polygons;
		modelOs->Get (Model::PolygonsFieldName, polygons);

		return CreateElement (APIGuidFromString (id.ToCStr ()), vertices, polygons);
	} catch (...) {
		return GS::NoValue;
	}
}


GS::String CreateDirectShape::GetName () const
{
	return CreateDirectShapesCommandName;
}
	
		
GS::ObjectState CreateDirectShape::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::Array<GS::UniString> elementIds;

	GS::Array<GS::ObjectState> models;
	parameters.Get (ModelsFieldName, models);
	
	ACAPI_CallUndoableCommand ("CreateSpeckleMorphs", [&]() -> GSErrCode {

		for (const auto& model : models) {
			const auto result = CreateElement (model);
			if (result.HasValue ()) {
				elementIds.Push (APIGuidToString (result.Get ()));
			}
		}

		return NoError;
	});

	return GS::ObjectState (ElementIdsFieldName, elementIds);
}



}