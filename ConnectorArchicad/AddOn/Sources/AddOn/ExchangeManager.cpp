#include "ExchangeManager.hpp"


ExchangeManager::ExchangeManager ()
{
}


ExchangeManager::~ExchangeManager ()
{
}


ExchangeManager& ExchangeManager::GetInstance ()
{
	static ExchangeManager instance;
	return instance;
}


GSErrCode ExchangeManager::GetState (const GS::String& speckleId, bool& isConverted, API_Guid& convertedArchicadId) const
{
	isConverted = false;

	if (!speckleId.IsEmpty () && speckleToArchicadIds.ContainsKey (speckleId)) {
		isConverted = true;
		convertedArchicadId = speckleToArchicadIds[speckleId];
	}

	return NoError;
}


GSErrCode ExchangeManager::UpdateState (const GS::String& speckleId, API_Guid convertedArchicadId)
{
	if (speckleToArchicadIds.ContainsKey (speckleId))
		speckleToArchicadIds[speckleId] = convertedArchicadId;
	else
		speckleToArchicadIds.Add (speckleId, convertedArchicadId);

	return NoError;
}
