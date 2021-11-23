#ifndef	SCHEMA_DEFINITION_BUILDER_HPP
#define SCHEMA_DEFINITION_BUILDER_HPP

#include "SchemaDefinitionProvider.hpp"


namespace Json {


class SchemaDefinitionBuilder {
public:
	SchemaDefinitionBuilder () = default;
	SchemaDefinitionBuilder (const GS::Array<GS::UniString>& definitions);

	void Add (const GS::UniString& definition);
	
	GS::Optional<GS::UniString> Build () const;

private:
	GS::Array<GS::UniString> definitions;
};


}


#endif