using System.Collections.Generic;
using UnityEngine;

namespace VRLabs.ModularShaderSystem
{
    [CreateAssetMenu(fileName = "ShaderModule", menuName = "Modular Shader/Modular Shader", order = 0)]
    public class ModularShader : ScriptableObject
    {
        public string Id;
        public string Name;
        public string Version;
        public string Author;
        public string ShaderPath;
        public TemplateAsset ShaderTemplate;
        public string CustomEditor;
        public List<string> TemplateKeywords;
        public List<Property> Properties;
        public List<ShaderModule> BaseModules;
        [HideInInspector]public List<ShaderModule> AdditionalModules;
        
    }
}