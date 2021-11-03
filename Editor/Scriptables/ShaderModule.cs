using System.Collections.Generic;
using UnityEngine;

namespace VRLabs.ModularShaderSystem
{
    [CreateAssetMenu(fileName = "ShaderModule", menuName = MSSConstants.CREATE_PATH + "/Shader Module", order = 0)]
    public class ShaderModule : ScriptableObject
    {
        public string Id;
        public string Name;
        public string Version;
        public string Author;
        public string Description;
        public EnableProperty Enabled;
        public List<Property> Properties;
        public List<string> ModuleDependencies;
        public List<string> IncompatibleWith;
        public List<ModuleTemplate> Templates;
        public List<ShaderFunction> Functions;
        [HideInInspector] public string AdditionalSerializedData;
    }
}