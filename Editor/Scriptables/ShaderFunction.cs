using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace VRLabs.ModularShaderSystem
{
    [Serializable]
    public class ShaderFunction 
    {
        public string Name;
        public string AppendAfter;
        public short Priority = 100;
        public TemplateAsset ShaderFunctionCode;
        public List<Variable> UsedVariables;
        [FormerlySerializedAs("VariableSinkKeyword")] public List<string> VariableSinkKeywords;
        [FormerlySerializedAs("CodeSinkKeyword")] public List<string> CodeSinkKeywords;
    }
}