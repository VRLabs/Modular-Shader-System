using System;
using System.Collections.Generic;
using UnityEngine;

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
        public string VariableSinkKeyword;
        public string CodeSinkKeyword;
    }
}