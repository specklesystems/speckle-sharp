#ifndef GET_DATA_COMMAND_HPP
#define GET_DATA_COMMAND_HPP

#include "BaseCommand.hpp"


namespace AddOnCommands {


class GetDataCommand : public BaseCommand {
	virtual GS::String		GetFieldName () const = 0;
	virtual API_ElemTypeID	GetElemTypeID () const = 0;
	virtual GS::UInt64		GetMemoMask () const;
	
protected:
	GS::ErrCode				ExportProperties (const API_Element& element, const bool& sendProperties, const bool& sendListingParameters, const GS::Array<GS::Pair<API_Guid, API_Guid>>& systemItemPairs, GS::ObjectState& os) const;
	GS::ErrCode				ExportClassificationsAndProperties(const API_Element& element, GS::ObjectState& os, const bool& sendProperties, const bool& sendListingParameters) const;

	virtual GS::ErrCode		SerializeElementType (const API_Element& elem,
												  const API_ElementMemo& memo,
												  GS::ObjectState& os) const = 0;

	GS::ErrCode				SerializeElementType (const API_Element& elem,
												  const API_ElementMemo& memo,
												  GS::ObjectState& os,
												  const bool& sendProperties,
												  const bool& sendListingParameters) const;

public:
	virtual GS::ObjectState	Execute (const GS::ObjectState& parameters,
									 GS::ProcessControl& processControl) const override;
};


}

#endif
