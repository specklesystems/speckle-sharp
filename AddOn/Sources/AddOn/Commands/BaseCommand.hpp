#ifndef BASE_COMMAND_HPP
#define BASE_COMMAND_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"


namespace AddOnCommands {


class BaseCommand : public API_AddOnCommand {
public:
	virtual GS::Optional<GS::UniString>			GetSchemaDefinitions () const override;
	virtual GS::Optional<GS::UniString>			GetInputParametersSchema () const override;
	virtual GS::Optional<GS::UniString>			GetResponseSchema () const override;
	virtual API_AddOnCommandExecutionPolicy		GetExecutionPolicy () const override;
	virtual void								OnResponseValidationFailed (const GS::ObjectState& response) const override;
};


}


#endif