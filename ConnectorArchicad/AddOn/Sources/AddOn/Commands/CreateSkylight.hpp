#ifndef CREATE_SKYLIGHT_HPP
#define CREATE_SKYLIGHT_HPP

#include "CreateOpeningBase.hpp"


namespace AddOnCommands {


class CreateSkylight : public CreateOpeningBase {
	GS::UniString		GetUndoableCommandName () const override;
	GSErrCode			GetElementFromObjectState (const GS::ObjectState& os,
							API_Element& element,
							API_Element& elementMask,
							API_ElementMemo& memo,
							GS::UInt64& memoMask,
							API_SubElement** marker,
							AttributeManager& attributeManager,
							LibpartImportManager& libpartImportManager,
							GS::Array<GS::UniString>& log) const override;

public:
	virtual GS::String	GetName () const override;
};


}


#endif
