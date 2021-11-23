#include "SchemaDefinitionBuilder.hpp"


namespace Json {


SchemaDefinitionBuilder::SchemaDefinitionBuilder (const GS::Array<GS::UniString>& definitions) : definitions (definitions)
{
}


void SchemaDefinitionBuilder::Add (const GS::UniString& definition)
{
	definitions.Push (definition);
}


GS::Optional<GS::UniString> SchemaDefinitionBuilder::Build () const
{
	if (definitions.IsEmpty ()) {
		return GS::NoValue;
	}

	GS::UniString result;
	for (const auto& defintion : definitions) {
		result.Append (defintion);
		result.Append (",");
	}
	result.DeleteLast ();

	return "{" + result + "}";
}


}