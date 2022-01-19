using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace VRLabs.ModularShaderSystem
{
    /// <summary>
    /// Object indicating a template contained in a module that indicate what asset to use, in which keywords to add it to, and when to add it.
    /// </summary>
    [Serializable]
    public class ModuleTemplate 
    {
        /// <summary>
        /// Template asset to use
        /// </summary>
        public TemplateAsset Template;
        
        /// <summary>
        /// List of keyword hooks
        /// </summary>
        [FormerlySerializedAs("Keyword")] public List<string> Keywords;
        
        /// <summary>
        /// Boolean indicating if the template needs to generate it's own versions of the shader to toggle on and off the content of it.
        /// </summary>
        [FormerlySerializedAs("IsCGOnly")] public bool NeedsVariant;
        
        /// <summary>
        /// Queue indicating when the template is processed by the generator.
        /// </summary>
        public int Queue = 100;
    }
}