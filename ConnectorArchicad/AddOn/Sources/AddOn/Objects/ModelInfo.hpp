#ifndef OBJECTS_MODELINFO_HPP
#define OBJECTS_MODELINFO_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"
#include "ObjectState.hpp"
#include "Model3D/UMAT.hpp"


class ModelInfo {
public:
	enum EdgeStatus {
		HiddenEdge = 1,	// invisible
		SmoothEdge = 2,	// visible if countour bit
		VisibleEdge = 3	// visible (AKA hard, sharp, welded edge)
	};

	class Vertex {
	public:
		Vertex () = default;
		Vertex (double x, double y, double z);

		inline double GetX () const { return x; }
		inline double GetY () const { return y; }
		inline double GetZ () const { return z; }

		GSErrCode Store (GS::ObjectState& os) const;
		GSErrCode Restore (const GS::ObjectState& os);

	private:
		double x = {};
		double y = {};
		double z = {};
	};

	class EdgeId {
	public:
		Int32	vertexId1, vertexId2;

		EdgeId (Int32 vertexId1, Int32 vertexId2);

		bool operator== (EdgeId otherEdgeId) const;
		
		ULong GenerateHashValue (void) const;
	};

	class EdgeData {
	public:
		static const Int32 InvalidPolygonId = -1;

		EdgeStatus	edgeStatus;
		Int32		polygonId1, polygonId2;

		EdgeData ();
		EdgeData (EdgeStatus edgeStatus, Int32 polygonId1 = InvalidPolygonId, Int32 polygonId2 = InvalidPolygonId);
	};

	class Polygon {
	public:
		Polygon () = default;
		Polygon (const GS::Array<Int32>& pointIds, UInt32 material);

		inline const GS::Array<Int32>& GetPointIds () const { return pointIds; }
		inline const Int32 GetMaterial () const { return material; }

		GSErrCode Store (GS::ObjectState& os) const;
		GSErrCode Restore (const GS::ObjectState& os);

	private:
		GS::Array<Int32> pointIds;
		UInt32 material = {};
	};
	
	class Material {
	public:
		Material () = default;
		Material (const UMAT& aumat);
		Material (GS::UniString& name, short transparency, GS_RGBColor& ambientColor, GS_RGBColor& emissionColor);

		inline const GS::UniString& GetName () const { return name; }
		inline short GetTransparency () const { return transparency; }
		inline GS_RGBColor GetAmbientColor () const { return ambientColor; }
		inline GS_RGBColor GetEmissionColor () const { return emissionColor; }

		GSErrCode Store (GS::ObjectState& os) const;
		GSErrCode Restore (const GS::ObjectState& os);

	private:
		GS::UniString	name;
		short			transparency = {};			// [0..100]
		GS_RGBColor		ambientColor = {};
		GS_RGBColor		emissionColor = {};

	};

public:
	void AddVertex (const Vertex& vertex);
	void AddVertex (Vertex&& vertex);

	void AddEdge (const EdgeId& edgeId, const EdgeData& edgeData);
	void AddEdge (EdgeId&& edgeId, EdgeData&& edgeData);

	void AddPolygon (const Polygon& polygon);
	void AddPolygon (Polygon&& polygon);

	void AddId (const GS::UniString& id);
	void AddId (GS::UniString&& id);

	UInt32 AddMaterial (const UMAT& material);
	GSErrCode GetMaterial (const UInt32 materialIndex, ModelInfo::Material& material) const;

	inline const GS::Array<Vertex>& GetVertices () const { return vertices; }
	inline const GS::HashTable<EdgeId, EdgeData>& GetEdges () const { return edges; }
	inline const GS::Array<Polygon>& GetPolygons () const { return polygons; }
	inline const GS::Array<Material>& GetMaterials () const { return materials; }
	inline const GS::Array<GS::UniString>& GetIds () const { return ids; }

	GSErrCode Store (GS::ObjectState& os) const;
	GSErrCode Restore (const GS::ObjectState& os);

private:
	GS::Array<GS::UniString> ids;
	GS::Array<Vertex> vertices;
	GS::HashTable<EdgeId, EdgeData> edges;
	GS::Array<Polygon> polygons;
	GS::Array<Material> materials;
};


#endif
