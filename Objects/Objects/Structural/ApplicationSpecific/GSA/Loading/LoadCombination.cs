using Speckle.Core.Kits;
using System.Collections.Generic;
using Objects.Structural.Loading;
using System;

namespace Objects.Structural.GSA.Loading
{
    public class LoadCombination : Structural.Loading.LoadCombination
    {
        public int nativeId { get; set; }
        public LoadCombination() { }

        public LoadCombination(int nativeId, string name,
            [SchemaParamInfo("A dictionary with key/value pairs to map a load factor (value) to a load case (key)")] Dictionary<string, double> caseFactors)
        {
            this.nativeId = nativeId;
            this.name = name;
            this.caseFactors = caseFactors;
            this.nativeId = nativeId;
        }

        [SchemaInfo("GSALoadCombination", "Creates a Speckle load combination for GSA", "GSA", "Loading")]
        public LoadCombination(int nativeId, string name,
            [SchemaParamInfo("A list of load cases")] List<Structural.Loading.LoadCase> loadCases,
            [SchemaParamInfo("A list of load factors (to be mapped to provided load cases)")] List<double> loadFactors)
        {
            this.nativeId = nativeId;
            this.name = name;

            if (loadCases.Count != loadFactors.Count)
                throw new ArgumentException("Number of load cases provided does not match number of load factors provided");

            var caseFactorsDict = new Dictionary<string, double> { };
            for (int i = 0; i < loadCases.Count; i++)
                caseFactorsDict[loadCases[i].name] = loadFactors[i];
            this.caseFactors = caseFactorsDict;

            this.nativeId = nativeId;
        }
    }





}
