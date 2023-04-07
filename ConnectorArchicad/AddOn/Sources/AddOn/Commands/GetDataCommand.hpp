#ifndef GET_DATA_COMMAND_HPP
#define GET_DATA_COMMAND_HPP

#include "BaseCommand.hpp"


namespace AddOnCommands {


class GetDataCommand : public BaseCommand {
	virtual GS::String		GetFieldName () const = 0;
	virtual API_ElemTypeID	GetElemTypeID () const = 0;
	virtual GS::UInt64		GetMemoMask () const;
	virtual GS::ErrCode		SerializeElementType (const API_Element& elem,
								const API_ElementMemo& memo,
								GS::ObjectState& os) const = 0;

public:
	virtual GS::ObjectState	Execute (const GS::ObjectState& parameters,
								GS::ProcessControl& processControl) const override;
};


}

#endif