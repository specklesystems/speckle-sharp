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

public:
	void AddVertex (const Vertex& vertex);
	void AddVertex (Vertex&& vertex);

	void AddPolygon (const Polygon& polygon);
	void AddPolygon (Polygon&& polygon);

	void AddId (const GS::UniString& id);
	void AddId (GS::UniString&& id);

	UInt32 AddMaterial (const UMAT& material);
	GSErrCode GetMaterial (const UInt32 materialIndex, ModelInfo::Material& material) const;

	inline const GS::Array<Vertex>& GetVertices () const { return vertices; }
	inline const GS::Array<Polygon>& GetPolygons () const { return polygons; }
	inline const GS::Array<Material>& GetMaterials () const { return materials; }
	inline const GS::HashTable<GS::Pair<GS::Int32, GS::Int32>, GS::UShort>& GetEdges () const { return edges; }
	inline const GS::Array<GS::UniString>& GetIds () const { return ids; }

	GSErrCode Store (GS::ObjectState& os) const;
	GSErrCode Restore (const GS::ObjectState& os);

private:
	GS::Array<GS::UniString> ids;
	GS::Array<Vertex> vertices;
	GS::Array<Polygon> polygons;
	GS::Array<Material> materials;
	GS::HashTable<GS::Pair<GS::Int32, GS::Int32>, GS::UShort> edges;
};


#endif
