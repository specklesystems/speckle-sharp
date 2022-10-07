#include "GetSlabData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Polyline.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"


namespace AddOnCommands {


	GS::ObjectState SerializeSlabType(const API_SlabType& slab, const API_ElementMemo& memo)
	{
		GS::ObjectState os;

		// The identifier of the slab
		os.Add(ApplicationIdFieldName, APIGuidToString(slab.head.guid));

		// The index of the slab's floor
		os.Add(FloorIndexFieldName, slab.head.floorInd);

		// The shape of the slab
		double level = Utility::GetStoryLevel(slab.head.floorInd) + slab.level;
		os.Add(ShapeFieldName, Objects::ElementShape(slab.poly, memo, level));

		// The structure type of the slab (basic or composite)
		os.Add(Slab::StructureFieldName, structureTypeNames.Get(slab.modelElemStructureType));

		// The building material index or composite index of the slab
		switch (slab.modelElemStructureType) {
		case API_BasicStructure:
			os.Add(Slab::BuildingMaterialIndexFieldName, slab.buildingMaterial);
			break;
		case API_CompositeStructure:
			os.Add(Slab::CompositeIndexFieldName, slab.composite);
			break;
		default:
			break;
		}

		// The thickness of the slab
		os.Add(Slab::ThicknessFieldName, slab.thickness);

		// The edge type and edge angle of the slab
		if ((BMGetHandleSize((GSHandle)memo.edgeTrims) / sizeof(API_EdgeTrim) >= 1) &&
			(*(memo.edgeTrims))[1].sideType == APIEdgeTrim_CustomAngle) {
			double angle = (*(memo.edgeTrims))[1].sideAngle;
			os.Add(Slab::EdgeAngleTypeFieldName, edgeAngleTypeNames.Get(APIEdgeTrim_CustomAngle));
			os.Add(Slab::EdgeAngleFieldName, angle);
		}
		else {
			os.Add(Slab::EdgeAngleTypeFieldName, edgeAngleTypeNames.Get(APIEdgeTrim_Perpendicular));
		}

		// The reference plane location of the slab
		os.Add(Slab::ReferencePlaneLocationFieldName, referencePlaneLocationNames.Get(slab.referencePlaneLocation));

		return os;
	}


	GS::String GetSlabData::GetName() const
	{
		return GetSlabDataCommandName;
	}


	GS::ObjectState GetSlabData::Execute(const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
	{
		GS::Array<GS::UniString> ids;
		parameters.Get(ApplicationIdsFieldName, ids);
		GS::Array<API_Guid>	elementGuids = ids.Transform<API_Guid>([](const GS::UniString& idStr) { return APIGuidFromString(idStr.ToCStr()); });

		GS::ObjectState result;

		const auto& listAdder = result.AddList<GS::ObjectState>(SlabsFieldName);
		for (const API_Guid& guid : elementGuids) {

			API_Element element{};
			API_ElementMemo elementMemo{};

			element.header.guid = guid;
			GSErrCode err = ACAPI_Element_Get(&element);
			if (err != NoError) continue;

#ifdef ServerMainVers_2600
			if (element.header.type.typeID != API_SlabID)
#else
			if (element.header.typeID != API_SlabID)
#endif
				continue;

			err = ACAPI_Element_GetMemo(guid, &elementMemo);
			if (err != NoError) continue;

			listAdder(SerializeSlabType(element.slab, elementMemo));
		}

		return result;
	}


}