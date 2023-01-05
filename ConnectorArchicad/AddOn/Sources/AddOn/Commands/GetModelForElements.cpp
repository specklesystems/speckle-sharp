#include "GetModelForElements.hpp"
#include "ResourceIds.hpp"
#include "Sight.hpp"
#include "ModelInfo.hpp"
#include "FieldNames.hpp"


namespace AddOnCommands {


static UInt32 MaximumSupportedPolygonPoints = 4;


static GS::Array<Int32> GetPolygonFromBody (const Modeler::MeshBody& body, Int32 polygonIdx, Int32 convexPolygonIdx, UInt32 vetrexOffset)
{
	GS::Array<Int32> polygonPoints;
	for (Int32 convexPolygonVertexIdx = 0; convexPolygonVertexIdx < body.GetConvexPolygonVertexCount (polygonIdx, convexPolygonIdx); ++convexPolygonVertexIdx) {
		polygonPoints.Push (body.GetConvexPolygonVertexIndex (polygonIdx, convexPolygonIdx, convexPolygonVertexIdx) + vetrexOffset);
	}

	return polygonPoints;
}


static void CollectPolygonsFromBody (const Modeler::MeshBody& body, const Modeler::Attributes::Viewer& attributes, UInt32 vetrexOffset, ModelInfo& modelInfo)
{
	for (UInt32 polygonIdx = 0; polygonIdx < body.GetPolygonCount (); ++polygonIdx) {

		const GSAttributeIndex matIdx = body.GetConstPolygonAttributes (polygonIdx).GetMaterialIndex ();
		const UMAT* aumat = attributes.GetConstMaterialPtr (matIdx);
		if (DBERROR (aumat == nullptr)) {
			continue;
		}

		UInt32 materialIdx = modelInfo.AddMaterial (*aumat);

		for (Int32 convexPolygonIdx = 0; convexPolygonIdx < body.GetConvexPolygonCount (polygonIdx); ++convexPolygonIdx) {
			GS::Array<Int32> polygonPointIds = GetPolygonFromBody (body, polygonIdx, convexPolygonIdx, vetrexOffset);
			if (polygonPointIds.IsEmpty ()) {
				continue;
			}

			if (polygonPointIds.GetSize () > MaximumSupportedPolygonPoints) {
				for (UInt32 i = 1; i < polygonPointIds.GetSize () - 1; ++i) {
					modelInfo.AddPolygon (ModelInfo::Polygon (GS::Array<Int32> { polygonPointIds[0], polygonPointIds[i], polygonPointIds[i + 1] }, materialIdx));
				}

				continue;
			}

			modelInfo.AddPolygon (ModelInfo::Polygon (polygonPointIds, materialIdx));
		}
	}
}


static void GetModelInfoForElement (const Modeler::Elem& elem, const Modeler::Attributes::Viewer& attributes, ModelInfo& modelInfo)
{
	const auto& transformation = elem.GetConstTrafo ();
	for (const auto& body : elem.TessellatedBodies ()) {
		UInt32 vetrexOffset = modelInfo.GetVertices ().GetSize ();

		for (UInt32 vertexIdx = 0; vertexIdx < body.GetVertexCount (); ++vertexIdx) {
			const auto coord = body.GetVertexPoint (vertexIdx, transformation);
			modelInfo.AddVertex (ModelInfo::Vertex (coord.x, coord.y, coord.z));
		}

		CollectPolygonsFromBody (body, attributes, vetrexOffset, modelInfo);
	}
}


static GS::Array<API_Guid> GetCurtainWallSubElements (const API_Guid& applicationId)
{
	GS::Array<API_Guid> applicationIds;

	API_ElementMemo memo{};
	ACAPI_Element_GetMemo (applicationId, &memo, APIMemoMask_CWallFrames | APIMemoMask_CWallPanels | APIMemoMask_CWallJunctions | APIMemoMask_CWallAccessories);

	GSSize nFrames = BMGetPtrSize (reinterpret_cast<GSPtr>(memo.cWallFrames)) / sizeof (API_CWFrameType);
	for (Int32 idx = 0; idx < nFrames; ++idx) {
		applicationIds.Push (memo.cWallFrames[idx].head.guid);
	}

	GSSize nPanels = BMGetPtrSize (reinterpret_cast<GSPtr>(memo.cWallPanels)) / sizeof (API_CWPanelType);
	for (Int32 idx = 0; idx < nPanels; ++idx) {
		applicationIds.Push (memo.cWallPanels[idx].head.guid);
	}

	GSSize nJunctions = BMGetPtrSize (reinterpret_cast<GSPtr>(memo.cWallJunctions)) / sizeof (API_CWJunctionType);
	for (Int32 idx = 0; idx < nJunctions; ++idx) {
		applicationIds.Push (memo.cWallJunctions[idx].head.guid);
	}

	GSSize nAccessories = BMGetPtrSize (reinterpret_cast<GSPtr>(memo.cWallAccessories)) / sizeof (API_CWAccessoryType);
	for (Int32 idx = 0; idx < nAccessories; ++idx) {
		applicationIds.Push (memo.cWallAccessories[idx].head.guid);
	}

	return applicationIds;
}


static GS::Array<API_Guid> GetBeamSubElements (const API_Guid& applicationId)
{
	GS::Array<API_Guid> applicationIds;

	API_ElementMemo memo{};
	ACAPI_Element_GetMemo (applicationId, &memo, APIMemoMask_BeamSegment);

	GSSize nSegments = BMGetPtrSize (reinterpret_cast<GSPtr>(memo.beamSegments)) / sizeof (API_BeamSegmentType);
	for (Int32 idx = 0; idx < nSegments; ++idx) {
		applicationIds.Push (memo.beamSegments[idx].head.guid);
	}

	return applicationIds;
}


static GS::Array<API_Guid> GetColumnSubElements (const API_Guid& applicationId)
{
	GS::Array<API_Guid> applicationIds;

	API_ElementMemo memo{};
	ACAPI_Element_GetMemo (applicationId, &memo, APIMemoMask_ColumnSegment);

	GSSize nSegments = BMGetPtrSize (reinterpret_cast<GSPtr>(memo.columnSegments)) / sizeof (API_ColumnSegmentType);
	for (Int32 idx = 0; idx < nSegments; ++idx) {
		applicationIds.Push (memo.columnSegments[idx].head.guid);
	}

	return applicationIds;
}


static GS::Array<API_Guid> CheckForSubelements (const API_Guid& applicationId)
{
	API_Elem_Head header{};
	header.guid = applicationId;

	const GSErrCode err = ACAPI_Element_GetHeader (&header);
	if (err != NoError) {
		return GS::Array<API_Guid> ();
	}

#ifdef ServerMainVers_2600
	switch (header.type.typeID) {
#else
	switch (header.typeID) {
#endif
	case API_CurtainWallID:					return GetCurtainWallSubElements (applicationId);
	case API_BeamID:						return GetBeamSubElements (applicationId);
	case API_ColumnID:						return GetColumnSubElements (applicationId);
	default:								return GS::Array<API_Guid> { applicationId };
	}
	}


static ModelInfo CalculateModelOfElement (const Modeler::Model3DViewer & modelViewer, const API_Guid & applicationId)
{
	ModelInfo modelInfo;
	const Modeler::Attributes::Viewer& attributes (modelViewer.GetConstAttributesPtr ());

	GS::Array<API_Guid> applicationIds = CheckForSubelements (applicationId);
	for (const auto& id : applicationIds) {
		const auto modelElement = modelViewer.GetConstElemPtr (APIGuid2GSGuid (id));
		if (modelElement == nullptr) {
			continue;
		}

		GetModelInfoForElement (*modelElement, attributes, modelInfo);
	}

	return modelInfo;
}


static GS::ObjectState StoreModelOfElements (const GS::Array<API_Guid>&applicationIds)
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

	const Modeler::Model3DPtr model = sight->GetMainModelPtr ();
	if (model == nullptr) {
		return {};
	}

	const Modeler::Model3DViewer modelViewer (model);

	GS::ObjectState result;
	const auto modelInserter = result.AddList<GS::ObjectState> (ModelsFieldName);
	for (const auto& applicationId : applicationIds) {
		modelInserter (GS::ObjectState{ApplicationIdFieldName, APIGuidToString (applicationId), Model::ModelFieldName, CalculateModelOfElement (modelViewer, applicationId)});
	}

	return result;
}


GS::String GetModelForElements::GetName () const
{
	return GetModelForElementsCommandName;
}


GS::ObjectState GetModelForElements::Execute (const GS::ObjectState & parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::Array<GS::UniString> ids;
	parameters.Get (ApplicationIdsFieldName, ids);

	return StoreModelOfElements (ids.Transform<API_Guid> ([] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); }));
}


}