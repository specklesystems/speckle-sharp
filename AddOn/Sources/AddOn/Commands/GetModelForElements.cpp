#include "GetModelForElements.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"


namespace AddOnCommands {


static const char* VerteciesFieldName = "vertecies";
static const char* VertexXFieldName = "x";
static const char* VertexYFieldName = "y";
static const char* VertexZFieldName = "z";
static const char* ModelFieldName = "model";
static const char* ModelsFieldName = "models";
static const char* ElementIdFieldName = "elementId";
static const char* ElementIdsFieldName = "elementIds";


static GS::Array<API_Coord3D> GetVerticiesOfBody (Int32 bodyIdx, const API_Coord& dbOffset)
{
	GS::Array<API_Coord3D> result;

	API_Component3D component = {};
	component.header.typeID = API_BodyID;
	component.header.index = bodyIdx;

	const auto err = ACAPI_3D_GetComponent (&component);
	if (err != NoError) {
		return {};
	}

	Int32 nVert = component.body.nVert;
	API_Tranmat tm = component.body.tranmat;

	for (Int32 j = 1; j <= nVert; j++) {
		component.header.typeID = API_VertID;
		component.header.index = j;
		const auto error = ACAPI_3D_GetComponent (&component);
		if (error != NoError) {
			return {};
		}

		API_Coord3D	trCoord {};
		trCoord.x = tm.tmx[0] * component.vert.x + tm.tmx[1] * component.vert.y + tm.tmx[2] * component.vert.z + tm.tmx[3];
		trCoord.y = tm.tmx[4] * component.vert.x + tm.tmx[5] * component.vert.y + tm.tmx[6] * component.vert.z + tm.tmx[7];
		trCoord.z = tm.tmx[8] * component.vert.x + tm.tmx[9] * component.vert.y + tm.tmx[10] * component.vert.z + tm.tmx[11];
		trCoord.x += dbOffset.x;
		trCoord.y += dbOffset.y;

		result.Push (trCoord);
	}

	return result;
}


static GS::Optional<GS::ObjectState> CreateElementModel (const API_Guid& elementId, const API_Coord& dbOffset)
{
	API_ElemInfo3D info3D {};
	API_Elem_Head element {};
	element.guid = elementId;

	const auto err = ACAPI_Element_Get3DInfo (element, &info3D);
	if (err != NoError) {
		return GS::NoValue;
	}

	GS::Array<API_Coord3D> vertecies;
	for (Int32 ibody = info3D.fbody; ibody <= info3D.lbody; ibody++) {
		vertecies.Append (GetVerticiesOfBody (ibody, dbOffset));
	}

	GS::ObjectState result;
	auto vertexInserter = result.AddList<GS::ObjectState> (VerteciesFieldName);
	for (const auto& vertex : vertecies) {
		vertexInserter (GS::ObjectState { VertexXFieldName, vertex.x, VertexYFieldName, vertex.y, VertexZFieldName, vertex.z });
	}

	return result;
}


static GS::ObjectState CreateElementsFromModel (const GS::Array<API_Guid>& elementIds)
{
	API_Coord dbOffset {};
	ACAPI_Database (APIDb_GetOffsetID, &dbOffset, nullptr);

	GS::ObjectState result;
	const auto modelInserter = result.AddList<GS::ObjectState> (ModelsFieldName);
	for (const auto& elementId : elementIds) {
		const auto model = CreateElementModel (elementId, dbOffset);
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
	return GS::NoValue; 
}


GS::Optional<GS::UniString>	GetModelForElements::GetInputParametersSchema () const
{
	return GS::NoValue; 
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

	return CreateElementsFromModel (ids.Transform<API_Guid> ([] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); }));
}


void GetModelForElements::OnResponseValidationFailed (const GS::ObjectState& /*response*/) const
{
}


}