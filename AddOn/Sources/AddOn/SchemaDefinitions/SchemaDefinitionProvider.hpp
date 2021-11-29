#ifndef	SCHEMA_DEFINITION_PROVIDER_HPP
#define SCHEMA_DEFINITION_PROVIDER_HPP


#include "APIEnvir.h"
#include "ACAPinc.h"


namespace Json {


class SchemaDefinitionProvider {
public:
	static GS::UniString ElementIdSchema ();
	static GS::UniString ElementIdsSchema ();
	static GS::UniString ElementTypeSchema ();
	
	static GS::UniString Point3DSchema ();
	static GS::UniString PolygonSchema ();
	static GS::UniString ElementModelSchema ();
	static GS::UniString WallDataSchema ();
};


}


#endif