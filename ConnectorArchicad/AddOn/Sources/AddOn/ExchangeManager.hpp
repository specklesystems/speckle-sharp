#ifndef EXCHANGE_MANAGER_HPP
#define EXCHANGE_MANAGER_HPP

#include "ModelInfo.hpp"
#include "AttributeManager.hpp"


class ExchangeManager {
private:
	GS::HashTable<GS::String, API_Guid> speckleToArchicadIds;
	ExchangeManager ();

public:
	ExchangeManager (ExchangeManager const&) = delete;
	void operator=(ExchangeManager const&) = delete;
	~ExchangeManager ();

	static ExchangeManager& GetInstance ();

	GSErrCode GetState (const GS::String& speckleId, bool& isConverted, API_Guid& convertedArchicadId) const;
	GSErrCode UpdateState (const GS::String& speckleId, API_Guid convertedArchicadId);
};

#endif
