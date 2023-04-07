#include "FinishReceiveTransaction.hpp"
#include "LibpartImportManager.hpp"
#include "ResourceIds.hpp"


GS::ObjectState AddOnCommands::FinishReceiveTransaction::Execute (const GS::ObjectState& /*parameters*/, GS::ProcessControl& /*processControl*/) const
{
	AttributeManager::DeleteInstance();
    LibpartImportManager::DeleteInstance ();
    return GS::ObjectState ();
}


GS::String AddOnCommands::FinishReceiveTransaction::GetName() const
{
    return EndCreateTransactionCommandName;
}
