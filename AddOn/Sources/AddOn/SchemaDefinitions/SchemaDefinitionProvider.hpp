#ifndef	SCHEMA_DEFINITION_PROVIDER_HPP
#define SCHEMA_DEFINITION_PROVIDER_HPP


#include "APIEnvir.h"
#include "ACAPinc.h"


namespace Json {


class SchemaDefintionProvider {
public:
	static GS::UniString ElementIdsSchema ();
	static GS::UniString ElementTypeSchema ();
};


}


#endif