#ifndef GET_ELEMENT_TYPES_HPP
#define GET_ELEMENT_TYPES_HPP

#include "BaseCommand.hpp"


namespace AddOnCommands {


class GetElementTypes : public BaseCommand {
public:
	virtual GS::String		GetName () const override;
	virtual GS::ObjectState	Execute (const GS::ObjectState& parameters, GS::ProcessControl& processControl) const override;
};


}


#endif