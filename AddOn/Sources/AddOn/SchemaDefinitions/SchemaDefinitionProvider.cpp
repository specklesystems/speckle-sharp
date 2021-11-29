#include "SchemaDefinitionProvider.hpp"
#include "Utility.hpp"


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
	GS::UniString buildedEnum;

	for (const auto& element : Utility::elementNames) {
		buildedEnum.Append('"' + *element.value + '"');
		buildedEnum.Append(",");
	}

	buildedEnum.DeleteLast();

	return "\"ElementType\":{\"enum\" : [" + buildedEnum + "]}";
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


GS::UniString SchemaDefinitionProvider::Point2DSchema ()
{
	return R"(
		"Point2D": {
            "type": "object",
            "properties": {
                "x": { "type": "number" },
                "y": { "type": "number" }
             },
            "additionalProperties": false,
            "required": [ "x", "y" ]
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


GS::UniString SchemaDefinitionProvider::PolylineSegmentSchema ()
{
	return R"(
		"PolylineSegment": {
            "type": "object",
            "properties": {
                "startPoint": { "$ref": "#/definitions/Point3D" },
                "endPoint": { "$ref": "#/definitions/Point3D" },
                "arcAngle": { "type": "number" }
             },
            "additionalProperties": false,
            "required": [ "startPoint", "endPoint", "arcAngle" ]
        }
	)";
}


GS::UniString SchemaDefinitionProvider::PolylineSchema ()
{
	return R"(
		"Polyline": {
			"type": "object",
			"properties": {
				"polylineSegments": {
					"type": "array",
					"items": { "$ref": "#/definitions/PolylineSegment" }
				}
			},
			"additionalProperties" : false,
			"required" : [ "polylineSegments" ]
		}
	)";
}


GS::UniString SchemaDefinitionProvider::ElementShapeSchema ()
{
	return R"(
		"Polygon": {
			"type": "object",
			"properties": {
				"contourPolyline": { "$ref": "#/definitions/Polyline" },
				"holePolylines": {
					"type": "array",
					"items": { "$ref": "#/definitions/Polyline" }
				}
			},
			"additionalProperties" : false,
			"required" : [ "contourPolyline" ]
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

GS::UniString SchemaDefinitionProvider::WallDataSchema ()
{
	return R"(
		"WallData": {
			"type": "object",
			"properties" : {
				"elementId" : { "$ref": "#/definitions/ElementId" },
				"floorIndex" : { "type": "integer" },
				"startPoint" : { "$ref": "#/definitions/Point3D" },
				"endPoint" : { "$ref": "#/definitions/Point3D" },
				"arcAngle" : { "type": "number" },
				"height" : { "type": "number" },
				"structure" : { "enum" : [ "Basic", "Composite", "Complex Profile" ] },
				"geometryMethod" : { "enum" : [ "Straight", "Trapezoid", "Polygonal" ] },
				"wallComplexity" : { "enum" : [ "Straight", "Profiled", "Slanted", "Double Slanted" ] },
				"thickness" : { "type": "number" },
				"firstThickness" : { "type": "number" },
				"secondThickness" : { "type": "number" },
				"outsideSlantAngle" : { "type": "number" },
				"insideSlantAngle" : { "type": "number" }
			},
			"additionalProperties" : false,
			"required" : [ "elementId", "startPoint", "endPoint" ]
		}
	)";
}

}