#include "GetModelForElements.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Sight.hpp"
#include "SchemaDefinitionBuilder.hpp"


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
static const char*		ElementIdFieldName				= "elementId";
static const char*		ElementIdsFieldName				= "elementIds";


class Model3DInfo {
public:
	void AddVertex (double x, double y, double z)
	{
		vertices.PushNew (x, y, z);
	}

	void AddPolygon (const GS::Array<Int32>& pointIds)
	{
		polygons.PushNew (pointIds);
	}

	GSErrCode Store (GS::ObjectState& os) const
	{
		os.Add (VerteciesFieldName, vertices);
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

	GS::Array<Vertex> vertices;
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


static GS::Array<API_Guid> GetCurtainWallSubElements (const API_Guid& elementId)
{
	GS::Array<API_Guid> elementIds;

	API_ElementMemo memo{};
	ACAPI_Element_GetMemo (elementId, &memo, APIMemoMask_CWallFrames | APIMemoMask_CWallPanels | APIMemoMask_CWallJunctions | APIMemoMask_CWallAccessories);

	GSSize nFrames = BMGetPtrSize (reinterpret_cast<GSPtr>(memo.cWallFrames)) / sizeof (API_CWFrameType);
	for (Int32 idx = 0; idx < nFrames; ++idx) {
		elementIds.Push (memo.cWallFrames[idx].head.guid);
	}

	GSSize nPanels = BMGetPtrSize (reinterpret_cast<GSPtr>(memo.cWallPanels)) / sizeof (API_CWPanelType);
	for (Int32 idx = 0; idx < nPanels; ++idx) {
		elementIds.Push (memo.cWallPanels[idx].head.guid);
	}

	GSSize nJunctions = BMGetPtrSize (reinterpret_cast<GSPtr>(memo.cWallJunctions)) / sizeof (API_CWJunctionType);
	for (Int32 idx = 0; idx < nJunctions; ++idx) {
		elementIds.Push (memo.cWallJunctions[idx].head.guid);
	}

	GSSize nAccessories = BMGetPtrSize (reinterpret_cast<GSPtr>(memo.cWallAccessories)) / sizeof (API_CWAccessoryType);
	for (Int32 idx = 0; idx < nAccessories; ++idx) {
		elementIds.Push (memo.cWallAccessories[idx].head.guid);
	}

	return elementIds;
}


static GS::Array<API_Guid> GetBeamSubElements (const API_Guid& elementId)
{
	GS::Array<API_Guid> elementIds;

	API_ElementMemo memo {};
	ACAPI_Element_GetMemo (elementId, &memo, APIMemoMask_BeamSegment);

	GSSize nSegments = BMGetPtrSize (reinterpret_cast<GSPtr>(memo.beamSegments)) / sizeof (API_BeamSegmentType);
	for (Int32 idx = 0; idx < nSegments; ++idx) {
		elementIds.Push (memo.beamSegments[idx].head.guid);
	}

	return elementIds;
}


static GS::Array<API_Guid> GetColumnSubElements (const API_Guid& elementId)
{
	GS::Array<API_Guid> elementIds;

	API_ElementMemo memo{};
	ACAPI_Element_GetMemo (elementId, &memo, APIMemoMask_ColumnSegment);

	GSSize nSegments = BMGetPtrSize (reinterpret_cast<GSPtr>(memo.columnSegments)) / sizeof (API_ColumnSegmentType);
	for (Int32 idx = 0; idx < nSegments; ++idx) {
		elementIds.Push (memo.columnSegments[idx].head.guid);
	}

	return elementIds;
}


static GS::Array<API_Guid> CheckForSubelements (const API_Guid& elementId)
{
	API_Elem_Head header {};
	header.guid = elementId;

	const GSErrCode err = ACAPI_Element_GetHeader (&header);
	if (err != NoError) {
		return GS::Array<API_Guid> ();
	}

	switch (header.typeID) {
		case API_CurtainWallID:					return GetCurtainWallSubElements (elementId);
		case API_BeamID:						return GetBeamSubElements (elementId);
		case API_ColumnID:						return GetColumnSubElements (elementId);
		default:								return GS::Array<API_Guid> { elementId };
	}
}


static GS::Array<Model3DInfo> CalculateModelOfElement (const Modeler::Model3DViewer& modelViewer, const API_Guid& elementId)
{
	GS::Array<Model3DInfo> modelInfos;
	GS::Array<API_Guid> elementIds = CheckForSubelements (elementId);
	for (const auto& id : elementIds) {
		const auto modelElement = modelViewer.GetConstElemPtr (APIGuid2GSGuid (id));
		if (modelElement == nullptr) {
			continue;
		}

		modelInfos.Append (GetModel3DInfoForElement (*modelElement));
	}

	return modelInfos;
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
		const auto model = CalculateModelOfElement (modelViewer, elementId);
		if (model.IsEmpty ()) {
			continue;
		}

		modelInserter (GS::ObjectState { ElementIdFieldName, APIGuidToString (elementId), ModelFieldName, model });
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
	Json::SchemaDefinitionBuilder builder;
	builder.Add (Json::SchemaDefinitionProvider::ElementIdsSchema ());
	builder.Add (Json::SchemaDefinitionProvider::Point3DSchema ());
	builder.Add (Json::SchemaDefinitionProvider::PolygonSchema ());
	builder.Add (Json::SchemaDefinitionProvider::ElementModelSchema ());

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
	return R"(
		{
			"type": "object",
			"properties" : {
				"models": {
					"type": "array",
					"items": { "$ref": "#/definitions/ElementModel" }
				}
			},
			"additionalProperties" : false,
			"required" : [ "models" ]
		}
	)";
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