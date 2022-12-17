#ifndef GET_MODEL_FOR_ELEMENTS_HPP
#define GET_MODEL_FOR_ELEMENTS_HPP

#include "BaseCommand.hpp"


namespace AddOnCommands {


class GetModelForElements : public BaseCommand {
public:
	virtual GS::String							GetName () const override;
	virtual GS::ObjectState						Execute (const GS::ObjectState& parameters, GS::ProcessControl& processControl) const override;
#ifdef ServerMainVers_2600
	virtual bool								IsProcessWindowVisible () const override { return true; }
#endif
};


}


#endif