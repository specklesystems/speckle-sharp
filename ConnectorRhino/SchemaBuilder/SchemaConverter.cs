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

namespace SpeckleRhino
{
    /// <summary>
    /// Converts between Speckle Schemas and Attribute User Text (AUT) entries for a Rhino Object.
    /// </summary>
    /// <remarks>
    /// Assumptions: 
    /// The AUT schema builder entry value will be of format "Schema{Property:Value}".
    /// The AUT schema builder can only support custom property values of type string and double (no list support yet).
    /// Ex: "RevitFloor{Family:Floor,Type:Floor,Level:L2}"
    /// </remarks>
    public class SchemaConverter
    {
        #region Properties
        private RhinoObject DocObject;
        private string SpeckleAUTValue;
        private List<Type> ValidTypes;
        private ConstructorInfo SchemaConstructor;

        public ISpeckleConverter Converter;
        public ISpeckleKit Kit;

        // AUT string settings
        private string SpeckleUATKey = "SpeckleSchemaBuilder";
        private char PropertyDelimiter = ',';
        private char PropertyValueDelimiter = ':';
        #endregion

        #region Constructors
        public SchemaConverter(RhinoObject inputObject)
        {
            DocObject = inputObject;
            ValidTypes = GetValidTypes();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Creates a AUT schema entry for the Rhino Object
        /// </summary>
        /// <param name="inputType">the Speckle type to use</param>
        public bool CreateSchema(Type inputType)
        {
            if (!ValidTypes.Contains(inputType)) return false;
            return true;
        }

        /// <summary>
        /// Return method for valid schemas
        /// </summary>
        /// <returns></returns>
        public List<Type> GetValidSchemas()
        {
            return ValidTypes;
        }

        /// <summary>
        /// Gets the Speckle base object from the Rhino Object's AUT entry
        /// </summary>
        /// <returns></returns>
        public bool GetSchemaObject(out Base SchemaObject)
        {
            Kit = KitManager.GetDefaultKit();
            Converter = Kit.LoadConverter(Applications.Rhino);
            Converter.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);

            SchemaObject = null;

            // get the AUT entry
            try
            {
                string AUTValue = DocObject.Attributes.GetUserStrings()[SpeckleUATKey];
                SpeckleAUTValue = AUTValue.Replace(" ", string.Empty);
            }
            catch
            {
                // if no schema entry exists
                Rhino.RhinoApp.WriteLine(DocObject.Id + ": Speckle Schema doesn't exist!");
                return false;
            }

            // if an entry exists but the value is invalid
            if (!IsValidEntry(out Type schema, out Dictionary<string,object> properties)) 
            {
                Rhino.RhinoApp.WriteLine(DocObject.Id + ": Speckle Schema value is invalid!");
                return false; 
            }

            // using the first available constructor for the speckle schema type, create a list of final converted object properties with the properties dictionary
            SchemaConstructor = GetValidConstr(schema).First();
            var outputSchemaValues = new List<object>();
            var schemaParams = SchemaConstructor.GetParameters();
            foreach (var param in schemaParams)
            {
                // get property name and value
                Type propType = param.ParameterType;
                string propName = param.Name;

                // try to match param to value in prop dict and do a direct add if its a simple type (namely, string or double)
                if (properties.ContainsKey(propName) && propType.IsSimpleType())
                {
                    outputSchemaValues.Add(properties[propName]);
                }
                // try to match param to value in prop dict by using value as a constructor
                else if (properties.ContainsKey(propName))
                {
                    ConstructorInfo propConstructor = GetValidConstr(propType).First();
                    object propObject = propConstructor.Invoke((object[])properties[propName]);
                    if (propObject != null)
                    {
                        outputSchemaValues.Add(propObject);
                    }
                }
                // try to extract required value from the rhino object geom
                // may not catch required bools - think about making all speckle object constructor bools set to default value?
                else if (!param.IsOptional)
                {
                    if (ExtractValueFromGeometry(propType, out object propObject))
                    {
                        outputSchemaValues.Add(propObject);
                    }
                }
                else
                {
                    // if the prop dict didn't contain a necessary param and the necessary param could not be extracted from the rhino object
                    Rhino.RhinoApp.WriteLine(DocObject.Id + ": Speckle Schema values is invalid!");
                    return false;
                }
            }

            // get all the output schema values to construct a speckle base object
            var outputObject = SchemaConstructor.Invoke(outputSchemaValues.ToArray());
            ((Base)outputObject).applicationId = $"{SchemaConstructor.Name}-{DocObject.Id}";
            ((Base)outputObject).units = Units.GetUnitsFromString(Rhino.RhinoDoc.ActiveDoc.GetUnitSystemName(true, false, false, false));
            SchemaObject = outputObject as Base;

            return true;
        }
        #endregion

        #region Internal

        // get valid speckle schema types for this geometry object
        private List<Type> GetValidTypes()
        {
            // todo: based on input geometry type, create specific filters (eg rhino surface can only be wall, floor, or ceiling)

            return KitManager.Types.Where(
              x => GetValidConstr(x).Any()).OrderBy(x => x.Name).ToList();
        }

        // exclude types that don't have any constructors with a SchemaInfo attribute
        private IEnumerable<ConstructorInfo> GetValidConstr(Type type)
        {
            return type.GetConstructors().Where(y => y.GetCustomAttribute<SchemaInfo>() != null);
        }

        // determines whether the AUT schema builder entry value is valid
        private bool IsValidEntry(out Type schemaType, out Dictionary<string, object> schemaProperties)
        {
            schemaType = null;
            schemaProperties = null;

            // check for null or empty
            if (string.IsNullOrEmpty(SpeckleAUTValue)) { return false; }

            // TODO: add additional string checkers

            // get the schema type from the substring before the first bracket
            try
            {
                string typeString = SpeckleAUTValue.Substring(0, SpeckleAUTValue.IndexOf('{'));
                schemaType = ValidTypes.First(o => o.Name == typeString);
            }
            catch
            {
                Rhino.RhinoApp.WriteLine(DocObject.Id + ": Speckle schema name isn't valid!"); return false;
            }

            // get the schema properties from the substring inside the brackets
            string propertiesString = SpeckleAUTValue.Split('{', '}')[1];
            if (!string.IsNullOrEmpty(propertiesString))
            {
                string[] propertyStrings = propertiesString.Split(PropertyDelimiter);
                foreach (string property in propertyStrings)
                {
                    try
                    {
                        string propertyName = property.Split(PropertyValueDelimiter)[0];
                        object propertyValue = property.Split(PropertyValueDelimiter)[1];
                        if (Double.TryParse(propertyValue as string, out double propertyValueDouble))
                        {
                            propertyValue = propertyValueDouble;
                        }
                        schemaProperties.Add(propertyName, propertyValueDouble);
                    }
                    catch
                    {
                        Rhino.RhinoApp.WriteLine(DocObject.Id + ": Speckle schema property isn't valid!"); return false;
                    }
                }
            }

            return true;
        }

        // process geometry type to extract relevant schema properties
        private bool ExtractValueFromGeometry(Type t, out object obj)
        {
            obj = null;
            switch (DocObject.ObjectType)
            {
                case ObjectType.Surface:
                    Surface srf = DocObject.Geometry as Surface;
                    switch (t.Name)
                    {
                        case "Curve": 
                            break;
                        default: break;
                    }
                    break;
                case ObjectType.Brep: case ObjectType.PolysrfFilter:
                    Brep brp = DocObject.Geometry as Brep;
                    switch (t.Name)
                    {
                        case "Curve":
                            obj = brp.Curves2D[0].ToNurbsCurve();
                            if (obj != null) return true;
                            break;
                        default: break;
                    }
                    break;
                default: break;
            }
            return false;
        }

        private object GetObjectProp(object value, Type t)
        {
            var convertedValue = ConvertType(t, value);
            return convertedValue;
        }

        private object ConvertType(Type type, object value)
        {
            if (value == null)
            {
                return null;
            }

            var typeOfValue = value.GetType();
            if (value == null || typeOfValue == type || type.IsAssignableFrom(typeOfValue))
                return value;

            //needs to be before IsSimpleType
            if (type.IsEnum)
            {
                try
                {
                    return Enum.Parse(type, value.ToString());
                }
                catch { }
            }

            // int, doubles, etc
            if (Speckle.Core.Models.Utilities.IsSimpleType(value.GetType()))
            {
                return Convert.ChangeType(value, type);
            }

            if (Converter.CanConvertToSpeckle(value))
            {
                var converted = Converter.ConvertToSpeckle(value);
                //in some situations the converted type is not exactly the type needed by the constructor
                //even if an implicit casting is defined, invoking the constructor will fail because the type is boxed
                //to convert the boxed type, it seems the only valid solution is to use Convert.ChangeType
                //currently, this is to support conversion of Polyline to Polycurve in Objects
                if (converted.GetType() != type && !type.IsAssignableFrom(converted.GetType()))
                {
                    return Convert.ChangeType(converted, type);
                }
                return converted;
            }
            else
            {
                // Log conversion error?
            }
            throw new Exception($"Could not covert object to {type}");
        }
        #endregion
    }
}
