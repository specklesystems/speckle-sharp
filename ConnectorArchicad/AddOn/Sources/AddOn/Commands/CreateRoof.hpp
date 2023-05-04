#ifndef CREATE_ROOF_HPP
#define CREATE_ROOF_HPP

#include "CreateCommand.hpp"
#include "LibpartImportManager.hpp"


namespace AddOnCommands {


class CreateRoof : public CreateCommand {
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
	GS::String			GetName () const override;
};


}


#endif