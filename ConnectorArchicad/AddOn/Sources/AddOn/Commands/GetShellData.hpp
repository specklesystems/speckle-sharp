#ifndef GET_SHELL_DATA_HPP
#define GET_SHELL_DATA_HPP

#include "GetDataCommand.hpp"


namespace AddOnCommands {


class GetShellData : public GetDataCommand {
	GS::String			GetFieldName () const override;
	API_ElemTypeID		GetElemTypeID () const override;
	GS::ErrCode			SerializeElementType (const API_Element& elem,
							const API_ElementMemo& memo,
							GS::ObjectState& os) const override;

public:
	virtual GS::String	GetName () const override;
};


}


#endif