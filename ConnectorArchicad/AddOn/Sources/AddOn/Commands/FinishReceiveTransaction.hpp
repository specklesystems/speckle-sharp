#ifndef FINISH_RECEIVE_TRANSACTION_HPP
#define FINISH_RECEIVE_TRANSACTION_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"
#include "BaseCommand.hpp"


namespace AddOnCommands {


class FinishReceiveTransaction : public BaseCommand {

public:
	GS::ObjectState	Execute (const GS::ObjectState& parameters, GS::ProcessControl& processControl) const override;
	GS::String		GetName () const override;
};
}
#endif // !FINISH_RECEIVE_TRANSACTION_HPP
