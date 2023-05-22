#include "ObjectState.hpp"
#include "Utility.hpp"
#include "CreateCommand.hpp"
#include "LibpartImportManager.hpp"
#include "APIHelper.hpp"
#include "FieldNames.hpp"
#include "OnExit.hpp"
#include "ExchangeManager.hpp"
#include "Database.hpp"
#include "Objects/Level.hpp"


using namespace FieldNames;

namespace AddOnCommands {


GSErrCode CreateCommand::CreateNewElement (API_Element& element,
	API_ElementMemo& elementMemo,
	API_SubElement* marker /*= nullptr*/) const
{
	if (marker != nullptr)
		return ACAPI_Element_CreateExt (&element, &elementMemo, 1UL, marker);

	return ACAPI_Element_Create (&element, &elementMemo);
}


GSErrCode CreateCommand::ModifyExistingElement (API_Element& element,
	API_Element& elementMask,
	API_ElementMemo& memo,
	GS::UInt64 memoMask) const
{
	return ACAPI_Element_Change (&element, &elementMask, &memo, memoMask, true);
}


void CreateCommand::GetStoryFromObjectState (const GS::ObjectState& os, const double& elementLevel, short& floorIndex, double& relativeLevel) const
{
	Objects::Level level;
	os.Get (ElementBase::Level, level);
	Utility::SetStoryLevelAndFloor (elementLevel, level.floorIndex, relativeLevel);
	floorIndex = level.floorIndex;
}


GS::ObjectState CreateCommand::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::ObjectState result;

	GS::Array<GS::ObjectState> objectStates;
	parameters.Get (GetFieldName (), objectStates);

	ACAPI_CallUndoableCommand (GetUndoableCommandName (), [&] () -> GSErrCode {
		LibraryHelper helper (false);

		GS::Array<GS::ObjectState> applicationObjects;

		AttributeManager* attributeManager = AttributeManager::GetInstance ();
		LibpartImportManager* libpartImportManager = LibpartImportManager::GetInstance ();

		Utility::Database db;
		db.SwitchToFloorPlan ();

		for (const GS::ObjectState& objectState : objectStates) {
			API_Element element {};
			API_Element elementMask {};
			API_ElementMemo memo {};
			GS::UInt64 memoMask = 0;
			API_SubElement* marker = nullptr;
			GS::OnExit memoDisposer ([&memo, &marker] {
				ACAPI_DisposeElemMemoHdls (&memo);
				
				if (marker != nullptr)
					ACAPI_DisposeElemMemoHdls (&marker->memo);
			});

			GSErrCode err = NoError;

			GS::String speckleId;
			{
				objectState.Get (ElementBase::Id, speckleId);
				
				if (speckleId.IsEmpty())
					err = Error;
			}
	
			bool elementExists = false;
			GS::Array<GS::UniString> log;
			
			if (err == NoError) {
				bool isConverted = false;
				API_Guid convertedArchicadId;
				ExchangeManager::GetInstance().GetState (speckleId, isConverted, convertedArchicadId);
				
				elementExists = isConverted && Utility::ElementExists (convertedArchicadId);
				
				{
					// if already converted and element exists, use that
					if (elementExists) {
						element.header.guid = convertedArchicadId;
					}
					// otherwise try to use applicationId
					else {
						GS::UniString applicationId;
						objectState.Get (ElementBase::ApplicationId, applicationId);
						element.header.guid = APIGuidFromString (applicationId.ToCStr ());
					}
				}
				
				err = GetElementFromObjectState (objectState, element, elementMask, memo, memoMask, &marker, *attributeManager, *libpartImportManager, log);
				if (err == NoError) {
					if (elementExists) {
						err = ModifyExistingElement (element, elementMask, memo, memoMask);
					} else {
						err = CreateNewElement (element, memo, marker);
					}
				}
			}

			GS::ObjectState applicationObject;
			applicationObject.Add (ApplicationObject::OriginalId, speckleId);

			if (err == NoError) {
				GS::UniString applicationId = APIGuidToString (element.header.guid);
				applicationObject.Add (ElementBase::ApplicationId, applicationId);
				GS::Array<GS::UniString> createdIds;
				createdIds.Push (applicationId);
				applicationObject.Add (ApplicationObject::CreatedIds, createdIds);

				if (elementExists)
					applicationObject.Add (ApplicationObject::Status, ApplicationObject::StateUpdated);
				else
					applicationObject.Add (ApplicationObject::Status, ApplicationObject::StateCreated);

				ExchangeManager::GetInstance().UpdateState (speckleId, element.header.guid);
			} else {
				applicationObject.Add (ApplicationObject::Status, ApplicationObject::StateFailed);
				applicationObject.Add (ApplicationObject::Log, log);
			}

			applicationObjects.Push (applicationObject);
		}

		result.Add (ApplicationObject::ApplicationObjects, applicationObjects);

		return NoError;
	});

	return result;
}


} // namespace AddOnCommands
