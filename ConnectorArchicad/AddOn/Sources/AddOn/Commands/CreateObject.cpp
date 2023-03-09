#include "APIHelper.hpp"

#include "CreateObject.hpp"

#include "LibpartImportManager.hpp"

#include "ResourceIds.hpp"
#include "Utility.hpp"
#include "DGModule.hpp"
#include "FieldNames.hpp"

#include "ModelInfo.hpp"
using namespace FieldNames;


namespace AddOnCommands
{


static GSErrCode CreateNewObject (API_Element& object, API_ElementMemo* memo)
{
	return ACAPI_Element_Create (&object, memo);
}


static GSErrCode ModifyExistingObject (API_Element& object, API_Element& mask, API_ElementMemo* memo)
{
	return ACAPI_Element_Change (&object, &mask, memo, 0, true);
}


static GSErrCode GetObjectFromObjectState (const GS::ObjectState& os, API_Element& element, API_Element& objectMask, API_ElementMemo* memo, LibpartImportManager& libpartImportManager, AttributeManager& attributeManager)
{
	GSErrCode err = NoError;

	GS::UniString guidString;
	os.Get (ApplicationId, guidString);
	element.header.guid = APIGuidFromString (guidString.ToCStr ());
#ifdef ServerMainVers_2600
	element.header.type.typeID = API_ObjectID;
#else
	element.header.typeID = API_ObjectID;
#endif

	err = Utility::GetBaseElementData (element, memo);
	if (err != NoError)
		return err;

	// get the mesh
	ModelInfo modelInfo;
	os.Get (Model::Model, modelInfo);
	
	API_LibPart libPart;
	err = libpartImportManager.GetLibpart (modelInfo, attributeManager, libPart);
	if (err != NoError)
		return err;

	element.object.libInd = libPart.index;
	ACAPI_ELEMENT_MASK_SET (objectMask, API_ObjectType, libInd);

	Objects::Point3D pos;
	if (os.Contains (Object::pos))
		os.Get (Object::pos, pos);
	element.object.pos = pos.ToAPI_Coord ();
	ACAPI_ELEMENT_MASK_SET (objectMask, API_ObjectType, pos);
	
	return NoError;
}


GS::String CreateObject::GetName () const
{
	return CreateObjectCommandName;
}


GS::ObjectState CreateObject::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::ObjectState result;

	GS::Array<GS::ObjectState> objects;
	parameters.Get (FieldNames::Objects, objects);

	const auto& listAdder = result.AddList<GS::UniString> (ApplicationIds);

	ACAPI_CallUndoableCommand ("CreateSpeckleObject", [&] () -> GSErrCode {

		LibraryHelper helper (false);
		LibpartImportManager libpartImportManager;

		AttributeManager attributeManager;
		
		for (const GS::ObjectState& objectOs : objects) {
			API_Element object{};
			API_Element objectMask{};
			API_ElementMemo memo{};
			
			ModelInfo modelInfo;
			GSErrCode err = GetObjectFromObjectState (objectOs, object, objectMask, &memo, libpartImportManager, attributeManager);
			if (err != NoError)
				continue;

			bool objectExists = Utility::ElementExists (object.header.guid);
			if (objectExists) {
				err = ModifyExistingObject (object, objectMask, &memo);
			} else {
				err = CreateNewObject (object, &memo);
			}

			if (err == NoError) {
				GS::UniString elemId = APIGuidToString (object.header.guid);
				listAdder (elemId);
			}

			ACAPI_DisposeElemMemoHdls (&memo);
		}
		return NoError;
		});

	return result;
}


}

