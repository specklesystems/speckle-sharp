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
	API_SubElement** /*marker*/,
	AttributeManager& /*attributeManager*/,
	LibpartImportManager& libpartImportManager,
	GS::Array<GS::UniString>& log) const
{
	GSErrCode err = NoError;

	Utility::SetElementType (element.header, API_ObjectID);

	err = Utility::GetBaseElementData (element, &memo, nullptr, log);
	if (err != NoError)
		return err;

	// get the mesh
	GS::Array<GS::UniString> modelIds;
	os.Get (Model::ModelIds, modelIds);

	API_LibPart libPart;
	err = libpartImportManager.GetLibpartFromCache (modelIds, libPart);
	if (err != NoError)
		return err;

	element.object.libInd = libPart.index;
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ObjectType, libInd);

	// transform transformation matrix
	API_Tranmat transform;
	if (os.Contains (Object::transform)) {
		GS::ObjectState transformOs;
		os.Get (Object::transform, transformOs);

		Utility::CreateTransform (transformOs, transform);
		
		// set transformation GDL parameter
		{
			Int32 				addParNumDef = 0;
			API_AddParType		**addParDefault = nullptr;

			err = ACAPI_LibPart_GetParams (libPart.index, nullptr, nullptr, &addParNumDef, &addParDefault);
			if (err != NoError)
				return err;
			
			for (Int32 i = 0; i < addParNumDef; i++) {
				API_AddParType &parameter = (*addParDefault)[i];
				if (CHCompareCStrings (parameter.name, "map_xform", CS_CaseSensitive) == 0) {
					if (parameter.dim1 != 4 || parameter.dim2 != 3) {
						err = Error;
						break;
					}
					
					double** arrHdl = reinterpret_cast<double**>(parameter.value.array);
					for (Int32 k = 0; k < parameter.dim1; k++)
						for (Int32 j = 0; j < parameter.dim2; j++)
							// transpose matrix
							(*arrHdl)[k * parameter.dim2 + j] = transform.tmx[k + j * parameter.dim1];
					
					break;
				}
			}
		
			BMKillHandle (reinterpret_cast<GSHandle*>(&memo.params));
			if (err != NoError)
				return err;

			memo.params = addParDefault;
		}
	}
	
	Objects::Point3D pos;
	if (os.Contains (Object::pos)) {
		os.Get (Object::pos, pos);
		element.object.pos = pos.ToAPI_Coord ();
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ObjectType, pos);
	}

	memoMask = APIMemoMask_AddPars;

	return NoError;
}


GS::String CreateObject::GetName () const
{
	return CreateObjectCommandName;
}


GS::ObjectState CreateObject::Execute (const GS::ObjectState& parameters, GS::ProcessControl& processControl) const
{
	GS::Array<ModelInfo> meshModels;
	parameters.Get (FieldNames::MeshModels, meshModels);

	ACAPI_CallUndoableCommand (GetUndoableCommandName (), [&] () -> GSErrCode {
		LibraryHelper helper (false);
		AttributeManager* attributeManager = AttributeManager::GetInstance ();
		LibpartImportManager* libpartImportManager = LibpartImportManager::GetInstance ();
		for (ModelInfo meshModel : meshModels) {
			API_LibPart libPart;
			GS::ErrCode err = libpartImportManager->GetLibpart (meshModel, *attributeManager, libPart);
			if (err != NoError)
				return err;
		}
		return NoError;
	});

	return CreateCommand::Execute (parameters, processControl);
}


}

