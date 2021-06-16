using System.Collections.Generic;
using UnityEngine;

namespace VRLabs.ModularShaderSystem
{
    [CreateAssetMenu(fileName = "ModularShader", menuName = "Modular Shader/Shader Module", order = 0)]
    public class ShaderModule : ScriptableObject
    {
        public string Id;
        public string Name;
        public string Version;
        public string Author;
        public List<string> RequiredIncludes;
        public EnableProperty Enabled;
        public List<Property> Properties;
        public List<string> IncompatibleWith;
        public List<ModuleTemplate> Templates;
        public List<ShaderFunction> Functions;
    }
}