#ifndef GET_SUBELEMENT_DATA_HPP
#define GET_SUBELEMENT_DATA_HPP

#include "BaseCommand.hpp"

namespace AddOnCommands {


class GetSubElementInfo : public BaseCommand {


public:
	virtual GS::String		GetName () const override;
	virtual GS::ObjectState	Execute (const GS::ObjectState& parameters, GS::ProcessControl& processControl) const override;
};


}


#endif