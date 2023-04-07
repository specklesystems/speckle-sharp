#ifndef GET_SELECTED_ELEMENT_IDS_HPP
#define GET_SELECTED_ELEMENT_IDS_HPP

#include "BaseCommand.hpp"


namespace AddOnCommands {


class GetElementIds : public BaseCommand {
public:
	virtual GS::String		GetName () const override;
	virtual GS::ObjectState	Execute (const GS::ObjectState& parameters, GS::ProcessControl& processControl) const override;
};


}


#endif