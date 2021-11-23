#include "GetModelForElements.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Sight.hpp"
#include "SchemaDefinitions\SchemaDefinitionBuilder.hpp"


namespace AddOnCommands {


static UInt32			MaximumSupportedPolygonPoints	= 4;
static const char*		VerteciesFieldName				= "vertecies";
static const char*		VertexXFieldName				= "x";
static const char*		VertexYFieldName				= "y";
static const char*		VertexZFieldName				= "z";
static const char*		PolygonsFieldName				= "polygons";
static const char*		PointIdsFieldName				= "pointIds";
static const char*		ModelFieldName					= "model";
static const char*		ModelsFieldName					= "models";
static const char*		BodiesFieldName					= "bodies";
static const char*		ElementIdFieldName				= "elementId";
static const char*		ElementIdsFieldName				= "elementIds";


class Model3DInfo {
public:
	void AddVertex (double x, double y, double z)
	{
		vertecis.PushNew (x, y, z);
	}

	void AddPolygon (const GS::Array<Int32>& pointIds)
	{
		polygons.PushNew (pointIds);
	}

	GSErrCode Store (GS::ObjectState& os) const
	{
		os.Add (VerteciesFieldName, vertecis);
		os.Add (PolygonsFieldName, polygons);

		return NoError;
	}

private:
	class Vertex {
	public:
		Vertex (double x, double y, double z) : x (x), y (y), z (z)
		{
		}

		GSErrCode Store (GS::ObjectState& os) const
		{
			os.Add (VertexXFieldName, x);
			os.Add (VertexYFieldName, y);
			os.Add (VertexZFieldName, z);

			return NoError;
		}

	private:
		double x;
		double y;
		double z;
	};

	class Polygon {
	public:
		Polygon (const GS::Array<Int32>& pointIds) : pointIds (pointIds)
		{
		}

		GSErrCode Store (GS::ObjectState& os) const
		{
			os.Add (PointIdsFieldName, pointIds);

			return NoError;
		}

	private:
		GS::Array<Int32> pointIds;
	};

	GS::Array<Vertex> vertecis;
	GS::Array<Polygon> polygons;
};


static GS::Array<Int32> GetModel3DInfoPolygon (const Modeler::MeshBody& body, Int32 polygonIdx, Int32 convexPolygonIdx)
{
	GS::Array<Int32> polygonPoints;
	for (Int32 convexPolygonVertexIdx = 0; convexPolygonVertexIdx < body.GetConvexPolygonVertexCount (polygonIdx, convexPolygonIdx); ++convexPolygonVertexIdx) {
		polygonPoints.Push (body.GetConvexPolygonVertexIndex (polygonIdx, convexPolygonIdx, convexPolygonVertexIdx));
	}

	return polygonPoints;
}


static Model3DInfo GetModel3DInfoBody (const Modeler::MeshBody& body, const TRANMAT& transformation)
{
	Model3DInfo modelInfo;

	for (UInt32 vertexIdx = 0; vertexIdx < body.GetVertexCount (); ++vertexIdx) {
		const auto coord = body.GetVertexPoint (vertexIdx, transformation);
		modelInfo.AddVertex (coord.x, coord.y, coord.z);
	}

	for (UInt32 polygonIdx = 0; polygonIdx < body.GetPolygonCount (); ++polygonIdx) {
		for (Int32 convexPolygonIdx = 0; convexPolygonIdx < body.GetConvexPolygonCount (polygonIdx); ++convexPolygonIdx) {
			GS::Array<Int32> polygonPointIds = GetModel3DInfoPolygon (body, polygonIdx, convexPolygonIdx);
			if (polygonPointIds.IsEmpty ()) {
				continue;
			}

			if (polygonPointIds.GetSize () > MaximumSupportedPolygonPoints) {
				for (UInt32 i = 1; i < polygonPointIds.GetSize () - 1; ++i) {
					modelInfo.AddPolygon (GS::Array<Int32> { polygonPointIds[0], polygonPointIds[i], polygonPointIds[i+1] });
				}

				continue;
			}

			modelInfo.AddPolygon (polygonPointIds);
		}
	}

	return modelInfo;
}


static GS::Array<Model3DInfo> GetModel3DInfoForElement (const Modeler::Elem& elem)
{
	const auto& trafo = elem.GetConstTrafo ();

	GS::Array<Model3DInfo> bodies;
	for (const auto& body : elem.TessellatedBodies ()) {
		bodies.Push (GetModel3DInfoBody (body, trafo));
	}

	return bodies;
}


static GS::Optional<GS::ObjectState> StoreModelOfElement (const Modeler::Model3DViewer& modelViewer, const API_Guid& elementId)
{
	const auto modelElement = modelViewer.GetConstElemPtr (APIGuid2GSGuid (elementId));
	if (modelElement == nullptr) {
		return GS::NoValue;
	}

	const auto elementModelInfos = GetModel3DInfoForElement (*modelElement);
	if (elementModelInfos.IsEmpty ()) {
		return GS::NoValue;
	}

	return GS::ObjectState { BodiesFieldName, elementModelInfos };
}


static GS::ObjectState StoreModelOfElements (const GS::Array<API_Guid>& elementIds)
{
	GSErrCode err = ACAPI_Automate (APIDo_ShowAllIn3DID);
	if (err != NoError) {
		return {};
	}

	Modeler::Sight* sight = nullptr;
	err = ACAPI_3D_GetCurrentWindowSight ((void**) &sight);
	if (err != NoError || sight == nullptr) {
		return {};
	}

	const Modeler::Model3DPtr model = sight->GetMainModelPtr ();;
	if (model == nullptr) {
		return {};
	}

	const Modeler::Model3DViewer modelViewer (model);

	GS::ObjectState result;
	const auto modelInserter = result.AddList<GS::ObjectState> (ModelsFieldName);
	for (const auto& elementId : elementIds) {
		const auto model = StoreModelOfElement (modelViewer, elementId);
		if (model.IsEmpty ()) {
			continue;
		}

		modelInserter (GS::ObjectState { ElementIdFieldName, APIGuidToString (elementId), ModelFieldName, model.Get () });
	}

	return result;
}


GS::String GetModelForElements::GetNamespace () const
{
	return CommandNamespace;
}


GS::String GetModelForElements::GetName () const
{
	return GetModelForElementsCommandName;
}
	
		
GS::Optional<GS::UniString> GetModelForElements::GetSchemaDefinitions () const
{
	Json::SchemaDefinitionBuilder builder { GS::Array<GS::UniString> { Json::SchemaDefintionProvider::ElementIdsSchema () } };
	return builder.Build ();
}


GS::Optional<GS::UniString>	GetModelForElements::GetInputParametersSchema () const
{
	return R"(
		{
			"type": "object",
			"properties" : {
				"elementIds": { "$ref": "#/definitions/ElementIds" }
			},
			"additionalProperties" : false,
			"required" : [ "elementIds" ]
		}
	)";
}


GS::Optional<GS::UniString> GetModelForElements::GetResponseSchema () const
{
	return GS::NoValue; 
}


API_AddOnCommandExecutionPolicy GetModelForElements::GetExecutionPolicy () const 
{
	return API_AddOnCommandExecutionPolicy::ScheduleForExecutionOnMainThread; 
}


GS::ObjectState GetModelForElements::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::Array<GS::UniString> ids;
	parameters.Get (ElementIdsFieldName, ids);

	return StoreModelOfElements (ids.Transform<API_Guid> ([] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); }));
}


void GetModelForElements::OnResponseValidationFailed (const GS::ObjectState& /*response*/) const
{
}


}