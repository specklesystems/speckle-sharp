using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using System;

namespace Objects.Structural.Loading
{
    public class LoadCombination : Base //combination case
    {
        public string name { get; set; }  
        public Dictionary<string, double> caseFactors { get; set; }
        public CombinationType combinationType { get; set; }
        public LoadCombination() { }

        public LoadCombination(string name,
            [SchemaParamInfo("A dictionary with key/value pairs to map a load factor (value) to a load case (key)")] Dictionary<string, double> caseFactors, CombinationType combinationType)
        {
            this.name = name;
            this.caseFactors = caseFactors;
            this.combinationType = combinationType;
        }

        [SchemaInfo("LoadCombination", "Creates a Speckle load combination", "Structural", "Loading")]
        public LoadCombination(string name,
            [SchemaParamInfo("A list of load cases")] List<LoadCase> loadCases,
            [SchemaParamInfo("A list of load factors (to be mapped to provided load cases)")] List<double> loadFactors,
            CombinationType combinationType)
        {
            this.name = name;

            if (loadCases.Count != loadFactors.Count)
                throw new ArgumentException("Number of load cases provided does not match number of load factors provided");            

            var caseFactorsDict = new Dictionary<string, double> { };
            for (int i = 0; i < loadCases.Count; i++)
                caseFactorsDict[loadCases[i].name] = loadFactors[i];
            this.caseFactors = caseFactorsDict;
            this.combinationType = combinationType;
        }
    }
}
