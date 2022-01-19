using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace VRLabs.ModularShaderSystem
{
    /// <summary>
    /// Function information for a shader module.
    /// </summary>
    [Serializable]
    public class ShaderFunction 
    {
        /// <summary>
        /// Name of the function.
        /// </summary>
        public string Name;
        
        /// <summary>
        /// Function or keyword this function appends after.
        /// </summary>
        public string AppendAfter;
        
        /// <summary>
        /// Order in which this function is evaluated respectively to their <see cref="AppendAfter"/> value.
        /// </summary>
        [FormerlySerializedAs("Priority")] public short Queue = 100;
        
        /// <summary>
        /// Template containing the function implementation.
        /// </summary>
        public TemplateAsset ShaderFunctionCode;
        
        /// <summary>
        /// List of variables the functions uses.
        /// </summary>
        public List<Variable> UsedVariables;
        
        /// <summary>
        /// Keywords used to place variable definitions.
        /// </summary>
        [FormerlySerializedAs("VariableSinkKeywords")] [FormerlySerializedAs("VariableSinkKeyword")] public List<string> VariableKeywords;
        
        /// <summary>
        /// Keywords used to place function code implementation.
        /// </summary>
        [FormerlySerializedAs("CodeSinkKeywords")] [FormerlySerializedAs("CodeSinkKeyword")] public List<string> CodeKeywords;
    }
}