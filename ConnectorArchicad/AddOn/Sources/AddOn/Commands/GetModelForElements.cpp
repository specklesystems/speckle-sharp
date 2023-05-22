#include "GetModelForElements.hpp"
#include "ResourceIds.hpp"
#include "Sight.hpp"
#include "ModelInfo.hpp"
#include "FieldNames.hpp"
#include "Utility.hpp"
using namespace FieldNames;


namespace AddOnCommands {


static UInt32 MaximumSupportedPolygonPoints = 4;


static GS::Array<Int32> GetPolygonFromBody (const Modeler::MeshBody& body,
	Int32 polygonIdx,
	Int32 convexPolygonIdx,
	UInt32 vetrexOffset)
{
	GS::Array<Int32> polygonPoints;
	for (Int32 convexPolygonVertexIdx = 0; convexPolygonVertexIdx < body.GetConvexPolygonVertexCount (polygonIdx, convexPolygonIdx); ++convexPolygonVertexIdx) {
		polygonPoints.Push (body.GetConvexPolygonVertexIndex (polygonIdx, convexPolygonIdx, convexPolygonVertexIdx) + vetrexOffset);
	}

	return polygonPoints;
}


static void CollectPolygonsFromBody (const Modeler::MeshBody& body,
	const Modeler::Attributes::Viewer& attributes,
	UInt32 vetrexOffset,
	ModelInfo& modelInfo)
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


static void GetModelInfoForElement (const Modeler::Elem& elem,
	const Modeler::Attributes::Viewer& attributes,
	ModelInfo& modelInfo)
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


template<typename T>
GSErrCode GetSubElements (T* ptr, GS::Array<API_Guid>& applicationIds)
{
	GSSize nSubElements = BMGetPtrSize (reinterpret_cast<GSPtr>(ptr)) / sizeof (T);
	for (Int32 idx = 0; idx < nSubElements; ++idx) {
		applicationIds.Push (ptr[idx].head.guid);
	}

	return NoError;
}


static GS::Array<API_Guid> GetCurtainWallSubElements (const API_Guid& applicationId)
{
	GS::Array<API_Guid> applicationIds;

	API_ElementMemo memo{};
	ACAPI_Element_GetMemo (applicationId, &memo, APIMemoMask_CWallFrames | APIMemoMask_CWallPanels | APIMemoMask_CWallJunctions | APIMemoMask_CWallAccessories);

	GetSubElements<API_CWFrameType> (memo.cWallFrames, applicationIds);
	GetSubElements<API_CWPanelType> (memo.cWallPanels, applicationIds);
	GetSubElements<API_CWJunctionType> (memo.cWallJunctions, applicationIds);
	GetSubElements<API_CWAccessoryType> (memo.cWallAccessories, applicationIds);

	return applicationIds;
}


static GS::Array<API_Guid> GetStairSubElements (const API_Guid& applicationId)
{
	GS::Array<API_Guid> applicationIds;

	API_ElementMemo memo{};
	ACAPI_Element_GetMemo (applicationId, &memo, APIMemoMask_StairRiser | APIMemoMask_StairTread | APIMemoMask_StairStructure);

	GetSubElements<API_StairRiserType> (memo.stairRisers, applicationIds);
	GetSubElements<API_StairTreadType> (memo.stairTreads, applicationIds);
	GetSubElements<API_StairStructureType> (memo.stairStructures, applicationIds);

	return applicationIds;
}


static GS::Array<API_Guid> GetRailingSubElements (const API_Guid& applicationId)
{
	GS::Array<API_Guid> applicationIds;

	API_ElementMemo memo{};
	ACAPI_Element_GetMemo (applicationId, &memo, APIMemoMask_RailingToprail | APIMemoMask_RailingHandrail | APIMemoMask_RailingRail | APIMemoMask_RailingPost | APIMemoMask_RailingInnerPost | APIMemoMask_RailingBaluster | APIMemoMask_RailingPanel);

	GetSubElements<API_RailingToprailType> (memo.railingToprails, applicationIds);
	GetSubElements<API_RailingHandrailType> (memo.railingHandrails, applicationIds);
	GetSubElements<API_RailingRailType> (memo.railingRails, applicationIds);
	GetSubElements<API_RailingPostType> (memo.railingPosts, applicationIds);
	GetSubElements<API_RailingInnerPostType> (memo.railingInnerPosts, applicationIds);
	GetSubElements<API_RailingBalusterType> (memo.railingBalusters, applicationIds);
	GetSubElements<API_RailingPanelType> (memo.railingPanels, applicationIds);

	return applicationIds;
}


static GS::Array<API_Guid> GetBeamSubElements (const API_Guid& applicationId)
{
	GS::Array<API_Guid> applicationIds;

	API_ElementMemo memo{};
	ACAPI_Element_GetMemo (applicationId, &memo, APIMemoMask_BeamSegment);

	GetSubElements<API_BeamSegmentType> (memo.beamSegments, applicationIds);

	return applicationIds;
}


static GS::Array<API_Guid> GetColumnSubElements (const API_Guid& applicationId)
{
	GS::Array<API_Guid> applicationIds;

	API_ElementMemo memo{};
	ACAPI_Element_GetMemo (applicationId, &memo, APIMemoMask_ColumnSegment);

	GetSubElements<API_ColumnSegmentType> (memo.columnSegments, applicationIds);

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

	switch (Utility::GetElementType (header)) {
		case API_CurtainWallID:					return GetCurtainWallSubElements (applicationId);
		case API_StairID:						return GetStairSubElements (applicationId);
		case API_RailingID:						return GetRailingSubElements (applicationId);
		case API_BeamID:						return GetBeamSubElements (applicationId);
		case API_ColumnID:						return GetColumnSubElements (applicationId);
		default:								return GS::Array<API_Guid> { applicationId };
	}
}


static ModelInfo CalculateModelOfElement (const Modeler::Model3DViewer& modelViewer, const API_Guid& applicationId)
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
	const auto modelInserter = result.AddList<GS::ObjectState> (Models);
	for (const auto& applicationId : applicationIds) {
		modelInserter (GS::ObjectState{ElementBase::ApplicationId, APIGuidToString (applicationId), Model::Model, CalculateModelOfElement (modelViewer, applicationId)});
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
	parameters.Get (ElementBase::ApplicationIds, ids);

	return StoreModelOfElements (ids.Transform<API_Guid> ([] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); }));
}


}
