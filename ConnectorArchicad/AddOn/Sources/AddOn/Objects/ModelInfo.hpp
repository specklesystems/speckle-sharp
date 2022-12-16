#ifndef OBJECTS_MODELINFO_HPP
#define OBJECTS_MODELINFO_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"
#include "ObjectState.hpp"
#include "Model3D/UMAT.hpp"


class ModelInfo {
public:
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

		inline const GS::UniString& GetName () const { return name; }

		GSErrCode Store (GS::ObjectState& os) const;

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

	UInt32 AddMaterial (const UMAT& material);

	inline const GS::Array<Vertex>& GetVertices () const { return vertices; }

	GSErrCode Store (GS::ObjectState& os) const;

private:
	GS::Array<Vertex> vertices;
	GS::Array<Polygon> polygons;
	GS::Array<Material> materials;
};


#endif