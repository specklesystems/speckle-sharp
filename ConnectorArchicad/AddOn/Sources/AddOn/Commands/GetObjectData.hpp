#ifndef GET_OBJECT_DATA_HPP
#define GET_OBJECT_DATA_HPP

#include "GetDataCommand.hpp"


namespace AddOnCommands {


class GetObjectData : public GetDataCommand {
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
