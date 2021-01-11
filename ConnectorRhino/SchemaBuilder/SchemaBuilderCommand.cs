using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino;
using Rhino.DocObjects;
using Rhino.Commands;
using Rhino.PlugIns;

namespace SpeckleRhino
{
    public class SchemaBuilderCommand : Command
    {
        RhinoDoc ActiveDoc = null;

        public SchemaBuilderCommand()
        {
            Instance = this;
        }

        public static SchemaBuilderCommand Instance
        {
            get; private set;
        }

        public override string EnglishName
        {
            get { return "SpeckleApplySchema"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            ActiveDoc = doc;

            // get selection doc objects
            var selectionObjs = doc.Objects.GetSelectedObjects(false, false);
            SchemaObjectFilter schemaFilter = new SchemaObjectFilter(selectionObjs.ToList(), doc);

            // process the schema dictionary to apply a User Attribute Text field to each doc object
            var schemaDictionary = schemaFilter.SchemaDictionary;
            foreach (string schema in schemaDictionary.Keys)
            {
                foreach (RhinoObject obj in schemaDictionary[schema])
                {
                    SchemaConverter objConverter = new SchemaConverter(obj);
                    objConverter.CreateSchema(schema);
                }
            }

            return Result.Success;
        }
        protected void CommandTask()
        {
            
        }
    }
}
