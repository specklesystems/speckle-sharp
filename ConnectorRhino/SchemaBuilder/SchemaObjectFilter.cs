using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Collections;

namespace SpeckleRhino
{
    /// <summary>
    /// Handles QA/QC for determining whether RhinoObjects can be converted to supported Speckle schemas
    /// </summary>
    public class SchemaObjectFilter
    {
        private List<string> SupportedSchemas = new List<string>() { "Floor", "Wall", "Roof", "Ceiling" };

        #region Properties
        private Rhino.RhinoDoc Doc;
        public Dictionary<string, List<RhinoObject>> SchemaDictionary = new Dictionary<string, List<RhinoObject>>();

        // internal vars for processing doc objects
        private Dictionary<string, List<RhinoObject>> filterDictionary = new Dictionary<string, List<RhinoObject>>();
        private List<RhinoObject> objsToBeFiltered;
        #endregion

        #region Constructors
        public SchemaObjectFilter (List<RhinoObject> docObjects, Rhino.RhinoDoc doc)
        {
            Doc = doc;

            foreach (string SupportedSchema in SupportedSchemas)
            {
                filterDictionary.Add(SupportedSchema, new List<RhinoObject>());
                SchemaDictionary.Add(SupportedSchema, new List<RhinoObject>());
            }
            objsToBeFiltered = docObjects;

            ApplyNamingFilter();
            ApplyGeomFilter();
        }
        #endregion

        #region Internal Methods
        // check layer path and object name for all supported schemas
        private void ApplyNamingFilter()
        {
            for (int j = objsToBeFiltered.Count - 1; j >= 0; j--)
            {
                bool applied = false; 

                RhinoObject obj = objsToBeFiltered[j];
                string objName = obj.Attributes.Name;
                string objPath = Doc.Layers[obj.Attributes.LayerIndex].FullPath;

                if (objName != null)
                {
                    foreach (string schema in SupportedSchemas)
                    {
                        if (objName.Contains(schema))
                        {
                            // add to filter dic and remove from filter list
                            filterDictionary[schema].Add(obj);
                            objsToBeFiltered.RemoveAt(j);
                            applied = true;
                            break;
                        }
                    }
                }
                if (!applied)
                {
                    foreach (string schema in SupportedSchemas)
                    {
                        if (objPath.Contains(schema))
                        {
                            // add to filter dic and remove from filter list
                            filterDictionary[schema].Add(obj);
                            objsToBeFiltered.RemoveAt(j);
                            applied = true;
                            break;
                        }
                    }
                }
            }
        }

        private void ApplyGeomFilter()
        {
            // process the filter dictionary first and add all viable geom to the output dict
            foreach (string schema in filterDictionary.Keys)
            {
                foreach (RhinoObject obj in filterDictionary[schema])
                {
                    if (IsViableObject(schema,obj))
                    {
                        SchemaDictionary[schema].Add(obj);
                    }
                }
            }

            // process any srf objs remaining in the objstobefiltered list
            //ObjectEnumeratorSettings settings = new ObjectEnumeratorSettings();
            //settings.ObjectTypeFilter = ObjectType.Surface | ObjectType.Brep | ObjectType.PolysrfFilter;
            //List<RhinoObject> rhObjs = Doc.Objects.GetObjectList(settings).Where(o => objsToBeFiltered.Contains(o)).ToList();
            ProcessSurfaceObjects(objsToBeFiltered);
        }

        // this will output a dictionary with supported schemas for surface objects
        private void ProcessSurfaceObjects(List<RhinoObject> objs)
        {
            string floor = "Floor";
            string wall = "Wall";

            // try to select all xy planar srf: default to floor if so
            foreach (RhinoObject obj in objs)
            {
                Brep brp = obj.Geometry as Brep;
                if (brp.Surfaces.Count == 1)
                {
                    // test as floor first and then wall if this is a single face brp
                    if (IsViableObject(floor, obj))
                    {
                        SchemaDictionary[floor].Add(obj);
                    }
                    else if (IsViableObject(wall, obj))
                    {
                        SchemaDictionary[wall].Add(obj);
                    }
                }
                else
                {
                    // if multi surface, test if it may be a wall
                    if (IsViableObject(wall, obj))
                    {
                        SchemaDictionary[wall].Add(obj);
                    }
                }
            }
        }

        private bool IsViableObject(string schema, RhinoObject obj)
        {
            switch (schema)
            {
                case "Floor":
                case "Ceiling":
                case "Roof":
                    // assumes xy planar single surface
                    try
                    {
                        Brep brp = obj.Geometry as Brep;
                        if (brp.Surfaces.Count > 1) { return false; }
                        if (IsPlanar(brp.Surfaces.First(), out bool singleH, out bool singleV))
                        {
                            if (singleH)
                            {
                                return true;
                            }
                        }
                    }
                    catch
                    {
                        return false;
                    }
                    break;
                case "Wall":
                    // assumes all vertical planar surfaces
                    try
                    {
                        Brep brp = obj.Geometry as Brep;
                        bool allV = true;
                        foreach (Surface srf in brp.Surfaces)
                        {
                            if (IsPlanar(srf, out bool isH, out bool isV))
                            {
                                if (!isV)
                                {
                                    allV = false; break;
                                }
                            }
                        }
                        if (allV) return true;
                    }
                    catch
                    {
                        return false;
                    }
                    break;
                default:
                    return false;
            }
            return false;
        }

        private bool IsPlanar(Surface srf, out bool isHorizontal, out bool isVertical)
        {
            isHorizontal = false;
            isVertical = false;
            Plane p = new Plane();

            if (srf.TryGetPlane(out p))
            {
                Vector3d normal = p.Normal;
                if (normal.Unitize())
                {
                    if (Math.Abs(normal.Z) == 1)
                    {
                        isHorizontal = true;
                    }
                    else if (normal.Z == 0)
                    {
                        isVertical = true;
                    }
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
