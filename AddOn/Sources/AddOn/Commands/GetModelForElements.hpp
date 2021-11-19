#ifndef GET_MODEL_FOR_ELEMENTS_HPP
#define GET_MODEL_FOR_ELEMENTS_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"


namespace AddOnCommands {


class GetModelForElements : public API_AddOnCommand {
public:
	virtual GS::String							GetNamespace () const override;
	virtual GS::String							GetName () const override;
	virtual GS::Optional<GS::UniString>			GetSchemaDefinitions () const override;
	virtual GS::Optional<GS::UniString>			GetInputParametersSchema () const override;
	virtual GS::Optional<GS::UniString>			GetResponseSchema () const override;
	virtual API_AddOnCommandExecutionPolicy		GetExecutionPolicy () const override;
	virtual GS::ObjectState						Execute (const GS::ObjectState& parameters, GS::ProcessControl& processControl) const override;
	virtual void								OnResponseValidationFailed (const GS::ObjectState& response) const override;
};


}


#endif