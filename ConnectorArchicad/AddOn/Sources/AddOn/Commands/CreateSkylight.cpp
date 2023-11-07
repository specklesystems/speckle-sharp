#include "CreateSkylight.hpp"
#include "CreateOpeningBase.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "DGModule.hpp"
#include "LibpartImportManager.hpp"
#include "APIHelper.hpp"
#include "FieldNames.hpp"
#include "OnExit.hpp"
#include "ExchangeManager.hpp"
#include "TypeNameTables.hpp"
#include "Database.hpp"
#include "BM.hpp"


using namespace FieldNames;


namespace AddOnCommands
{


GS::UniString CreateSkylight::GetUndoableCommandName () const
{
	return "CreateSpeckleSkylight";
}


GSErrCode CreateSkylight::GetElementFromObjectState (const GS::ObjectState& os,
	API_Element& element,
	API_Element& elementMask,
	API_ElementMemo& memo,
	GS::UInt64& /*memoMask*/,
	API_SubElement** marker,
	AttributeManager& /*attributeManager*/,
	LibpartImportManager& /*libpartImportManager*/,
	GS::Array<GS::UniString>& log) const
{
	GSErrCode err = NoError;

	Utility::SetElementType (element.header, API_SkylightID);

	*marker = new API_SubElement ();
	BNZeroMemory (*marker, sizeof (API_SubElement));
	err = Utility::GetBaseElementData (element, &memo, marker, log);
	if (err != NoError)
		return err;

	err = GetElementBaseFromObjectState (os, element, elementMask);
	if (err != NoError)
		return err;

	if (!CheckEnvironment (os, element))
		return Error;

	if (os.Contains (Skylight::VertexID)) {
		os.Get (Skylight::VertexID, element.skylight.vertexID);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_SkylightType, vertexID);
	}

	// Skylight fix mode name
	if (os.Contains (Skylight::SkylightFixMode)) {
		GS::UniString skylightFixModeName;
		os.Get (Skylight::SkylightFixMode, skylightFixModeName);

		GS::Optional<API_SkylightFixModeID> type = skylightFixModeNames.FindValue (skylightFixModeName);
		if (type.HasValue ()) {
			element.skylight.fixMode = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_SkylightType, fixMode);
		}
	}

	// Skylight anchor name
	if (os.Contains (Skylight::SkylightAnchor)) {
		GS::UniString skylightAnchorName;
		os.Get (Skylight::SkylightAnchor, skylightAnchorName);

		GS::Optional<API_SkylightAnchorID> type = skylightAnchorNames.FindValue (skylightAnchorName);
		if (type.HasValue ()) {
			element.skylight.anchorPoint = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_SkylightType, anchorPoint);
		}
	}

	Objects::Point3D anchorPosition;
	if (os.Contains (Skylight::AnchorPosition)) {
		os.Get (Skylight::AnchorPosition, anchorPosition);
		element.skylight.anchorPosition = anchorPosition.ToAPI_Coord ();
		ACAPI_ELEMENT_MASK_SET (elementMask, API_SkylightType, anchorPosition);
	}

	if (os.Contains (Skylight::AzimuthAngle)) {
		os.Get (Skylight::AzimuthAngle, element.skylight.azimuthAngle);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_SkylightType, azimuthAngle);
	}

	if (os.Contains (Skylight::ElevationAngle)) {
		os.Get (Skylight::ElevationAngle, element.skylight.elevationAngle);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_SkylightType, elevationAngle);
	}

	err = GetOpeningBaseFromObjectState<API_SkylightType> (os, element.skylight, elementMask, log);

	return err;
}


GS::String CreateSkylight::GetName () const
{
	return CreateSkylightCommandName;
}


}
