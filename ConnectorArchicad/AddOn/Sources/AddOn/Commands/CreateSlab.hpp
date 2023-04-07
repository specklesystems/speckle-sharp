#ifndef CREATE_SLAB_HPP
#define CREATE_SLAB_HPP

#include "CreateCommand.hpp"


namespace AddOnCommands {


class CreateSlab : public CreateCommand {
	GS::String			GetFieldName () const override;
	GS::UniString		GetUndoableCommandName () const override;

	GSErrCode			GetElementFromObjectState (const GS::ObjectState& os,
							API_Element& element,
							API_Element& elementMask,
							API_ElementMemo& memo,
							GS::UInt64& memoMask,
							AttributeManager& attributeManager,
							LibpartImportManager& libpartImportManager,
							API_SubElement** marker = nullptr) const override;

public:
	virtual GS::String	GetName () const override;
};


}


#endif
