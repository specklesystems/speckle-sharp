#ifndef GET_COLUMN_DATA_HPP
#define GET_COLUMN_DATA_HPP

#include "GetDataCommand.hpp"


namespace AddOnCommands {


class GetColumnData : public GetDataCommand {
	GS::String			GetFieldName () const override;
	API_ElemTypeID		GetElemTypeID () const override;
	GS::UInt64			GetMemoMask () const override;
	GS::ErrCode			SerializeElementType (const API_Element& elem,
							const API_ElementMemo& memo,
							GS::ObjectState& os) const override;

public:
	virtual GS::String	GetName () const override;
};


}


#endif
