#ifndef GET_SELECTED_ELEMENT_IDS_HPP
#define GET_SELECTED_ELEMENT_IDS_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"


namespace AddOnCommands {


class GetSelectedElementIds : public API_AddOnCommand {
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