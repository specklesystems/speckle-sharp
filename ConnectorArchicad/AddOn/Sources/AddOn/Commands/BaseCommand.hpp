#ifndef BASE_COMMAND_HPP
#define BASE_COMMAND_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"


namespace AddOnCommands {


class BaseCommand : public API_AddOnCommand {
public:
	virtual GS::String						GetNamespace () const override final;
	virtual GS::Optional<GS::UniString>		GetSchemaDefinitions () const override final;
	virtual GS::Optional<GS::UniString>		GetInputParametersSchema () const override final;
	virtual GS::Optional<GS::UniString>		GetResponseSchema () const override final;
	virtual API_AddOnCommandExecutionPolicy	GetExecutionPolicy () const override final;
	virtual void							OnResponseValidationFailed (const GS::ObjectState& response) const override final;
#ifdef ServerMainVers_2600
	virtual bool							IsProcessWindowVisible () const override { return true; }
#endif
};


}


#endif