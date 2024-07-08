#ifndef GET_OPENING_DATA_HPP
#define GET_OPENING_DATA_HPP

#include "GetDataCommand.hpp"

namespace AddOnCommands {


class GetOpeningData : public GetDataCommand {
		GS::String				GetFieldName() const override;
		API_ElemTypeID			GetElemTypeID() const override;
		GS::ErrCode				SerializeElementType(const API_Element& elem,
									const API_ElementMemo& memo,
									GS::ObjectState& os) const override;

public:
	virtual GS::String		GetName() const override;
};


}


#endif // GET_OPENING_DATA_HPP