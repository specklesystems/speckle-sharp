#ifndef SELECT_ELEMENTS_HPP
#define SELECT_ELEMENTS_HPP

#include "BaseCommand.hpp"


namespace AddOnCommands {


class SelectElements : public BaseCommand {
public:
	virtual GS::String							GetName () const override;
	virtual GS::ObjectState						Execute (const GS::ObjectState& parameters, GS::ProcessControl& processControl) const override;
};


}


#endif