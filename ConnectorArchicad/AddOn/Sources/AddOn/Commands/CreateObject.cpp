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


GS::String CreateObject::GetFieldName () const
{
	return FieldNames::Objects;
}


GS::UniString CreateObject::GetUndoableCommandName () const
{
	return "CreateSpeckleObject";
}


GSErrCode CreateObject::GetElementFromObjectState (const GS::ObjectState& os,
	API_Element& element,
	API_Element& elementMask,
	API_ElementMemo& memo,
	GS::UInt64& memoMask,
	AttributeManager& attributeManager,
	LibpartImportManager& libpartImportManager,
	API_SubElement** /*marker = nullptr*/) const
{
	GSErrCode err = NoError;

#ifdef ServerMainVers_2600
	element.header.type.typeID = API_ObjectID;
#else
	element.header.typeID = API_ObjectID;
#endif

	err = Utility::GetBaseElementData (element, &memo);
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
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ObjectType, libInd);

	Objects::Point3D pos;
	if (os.Contains (Object::pos))
		os.Get (Object::pos, pos);
	element.object.pos = pos.ToAPI_Coord ();
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ObjectType, pos);

	memoMask = APIMemoMask_AddPars;

	return NoError;
}


GS::String CreateObject::GetName () const
{
	return CreateObjectCommandName;
}


}

