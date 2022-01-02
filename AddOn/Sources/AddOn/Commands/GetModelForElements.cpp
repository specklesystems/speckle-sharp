#include "GetModelForElements.hpp"
#include "ResourceIds.hpp"
#include "Sight.hpp"
#include "ModelInfo.hpp"
#include "FieldNames.hpp"


namespace AddOnCommands {


static UInt32 MaximumSupportedPolygonPoints	= 4;


static GS::Array<Int32> GetPolygonFromBody (const Modeler::MeshBody& body, Int32 polygonIdx, Int32 convexPolygonIdx, UInt32 offset)
{
	GS::Array<Int32> polygonPoints;
	for (Int32 convexPolygonVertexIdx = 0; convexPolygonVertexIdx < body.GetConvexPolygonVertexCount (polygonIdx, convexPolygonIdx); ++convexPolygonVertexIdx) {
		polygonPoints.Push (body.GetConvexPolygonVertexIndex (polygonIdx, convexPolygonIdx, convexPolygonVertexIdx) + offset);
	}

	return polygonPoints;
}


static GS::Array<Objects::ModelInfo::Polygon> GetPolygonsFromBody (const Modeler::MeshBody& body, const Modeler::Attributes::Viewer& attributes, UInt32 offset)
{
	GS::Array<Objects::ModelInfo::Polygon> result;

	for (UInt32 polygonIdx = 0; polygonIdx < body.GetPolygonCount (); ++polygonIdx) {

		const GSAttributeIndex matIdx = body.GetConstPolygonAttributes (polygonIdx).GetMaterialIndex ();
		const UMAT* aumat = attributes.GetConstMaterialPtr (matIdx);
		if (DBERROR (aumat == nullptr)) {
			continue;
		}

		for (Int32 convexPolygonIdx = 0; convexPolygonIdx < body.GetConvexPolygonCount (polygonIdx); ++convexPolygonIdx) {
			GS::Array<Int32> polygonPointIds = GetPolygonFromBody (body, polygonIdx, convexPolygonIdx, offset);
			if (polygonPointIds.IsEmpty ()) {
				continue;
			}

			if (polygonPointIds.GetSize () > MaximumSupportedPolygonPoints) {
				for (UInt32 i = 1; i < polygonPointIds.GetSize () - 1; ++i) {
					result.PushNew (GS::Array<Int32> { polygonPointIds[0], polygonPointIds[i], polygonPointIds[i + 1] }, *aumat);
				}

				continue;
			}

			result.PushNew (polygonPointIds, *aumat);
		}
	}

	return result;
}


static void GetModelInfoForElement (const Modeler::Elem& elem, const Modeler::Attributes::Viewer& attributes, Objects::ModelInfo& modelInfo)
{
	const auto& transformation = elem.GetConstTrafo ();
	for (const auto& body : elem.TessellatedBodies ()) {
		UInt32 offset = modelInfo.GetVertices ().GetSize ();

		for (UInt32 vertexIdx = 0; vertexIdx < body.GetVertexCount (); ++vertexIdx) {
			const auto coord = body.GetVertexPoint (vertexIdx, transformation);
			modelInfo.AddVertex (Objects::ModelInfo::Vertex (coord.x, coord.y, coord.z));
		}

		const auto polygons = GetPolygonsFromBody (body, attributes, offset);
		for (const auto& polygon : polygons) {
			modelInfo.AddPolygon (polygon);
		}
	}
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


static Objects::ModelInfo CalculateModelOfElement (const Modeler::Model3DViewer& modelViewer, const API_Guid& elementId)
{
	Objects::ModelInfo modelInfo;
	const Modeler::Attributes::Viewer& attributes (modelViewer.GetConstAttributesPtr ());

	GS::Array<API_Guid> elementIds = CheckForSubelements (elementId);
	for (const auto& id : elementIds) {
		const auto modelElement = modelViewer.GetConstElemPtr (APIGuid2GSGuid (id));
		if (modelElement == nullptr) {
			continue;
		}

		GetModelInfoForElement (*modelElement, attributes, modelInfo);
	}

	return modelInfo;
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
		modelInserter (GS::ObjectState { ElementIdFieldName, APIGuidToString (elementId), Model::ModelFieldName, CalculateModelOfElement (modelViewer, elementId) });
	}

	return result;
}


GS::String GetModelForElements::GetName () const
{
	return GetModelForElementsCommandName;
}


GS::ObjectState GetModelForElements::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::Array<GS::UniString> ids;
	parameters.Get (ElementIdsFieldName, ids);

	return StoreModelOfElements (ids.Transform<API_Guid> ([] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); }));
}


}