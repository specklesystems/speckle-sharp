#include "SchemaDefinitionProvider.hpp"


namespace Json {


GS::UniString SchemaDefinitionProvider::ElementIdSchema ()
{
	return R"(
		"ElementId": {
			"type": "string",
			"format": "uuid",
			"pattern": "^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$"
		}
	)";
}


GS::UniString SchemaDefinitionProvider::ElementIdsSchema ()
{
	return R"(
		"ElementIds": {
			"type": "array",
	  		"items": { "$ref": "#/definitions/ElementId" }
		}
	)";
}


GS::UniString SchemaDefinitionProvider::ElementTypeSchema ()
{
	return R"(
		"ElementType": {
            "enum": [
                "InvalidType",
                "Wall",
                "Column",
                "Beam",
                "Window",
                "Door",
                "Object",
                "Lamp",
                "Slab",
                "Roof",
                "Mesh",
                "Zone",
                "CurtainWall",
                "Shell",
                "Skylight",
                "Morph",
                "Stair",
                "Railing",
                "Opening"
            ]
        }
	)";
}


GS::UniString SchemaDefinitionProvider::Point3DSchema ()
{
	return R"(
		"Point3D": {
            "type": "object",
            "properties": {
                "x": { "type": "number" },
                "y": { "type": "number" },
                "z": { "type": "number" }
             },
            "additionalProperties": false,
            "required": [ "x", "y", "z" ]
        }
	)";
}


GS::UniString SchemaDefinitionProvider::PolygonSchema ()
{
    return R"(
		"Polygon": {
			"type": "object",
			"properties": {
				"pointIds": {
					"type": "array",
					"items": { "type": "integer" }
				}
			},
			"additionalProperties" : false,
			"required" : [ "pointIds" ]
		}
	)";
}


GS::UniString SchemaDefinitionProvider::ElementModelSchema ()
{
	return R"(
		"ElementModel": {
            "type": "object",
			"properties" : {
				"elementId": { "$ref": "#/definitions/ElementId" },
				"model": {
					"type": "array",
					"items" : {
						"type": "object",
						"properties": {
							"vertecies": {
								"type": "array",
								"items": { "$ref": "#/definitions/Point3D" }
							},
							"polygons": {
								"type": "array",
								"items": { "$ref": "#/definitions/Polygon" }
							}
						},
						"additionalProperties" : false,
						"required" : [ "vertecies", "polygons" ]
					}
				}
			},
			"additionalProperties" : false,
			"required" : [ "elementId", "model" ]
        }
	)";
}


}