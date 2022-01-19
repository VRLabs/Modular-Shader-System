using System.Collections.Generic;
using UnityEngine;

namespace VRLabs.ModularShaderSystem
{
    /// <summary>
    /// Asset containing a module to add features to a shader.
    /// </summary>
    [CreateAssetMenu(fileName = "ShaderModule", menuName = MSSConstants.CREATE_PATH + "/Shader Module", order = 0)]
    public class ShaderModule : ScriptableObject
    {
        /// <summary>
        /// Id of the shader module.
        /// </summary>
        public string Id;
        
        /// <summary>
        /// Name of the shader module.
        /// </summary>
        public string Name;
        
        /// <summary>
        /// Version of the shader module.
        /// </summary>
        public string Version;
        
        /// <summary>
        /// Author of the shader module.
        /// </summary>
        public string Author;
        
        /// <summary>
        /// Description of the shader model.
        /// </summary>
        public string Description;
        
        /// <summary>
        /// Property used to toggle on and off the module in the generated shader.
        /// </summary>
        public EnableProperty Enabled;
        
        /// <summary>
        /// List of properties declared by the module.
        /// </summary>
        public List<Property> Properties;
        
        /// <summary>
        /// List of ids of shader modules this shader module depends on.
        /// </summary>
        public List<string> ModuleDependencies;
        
        /// <summary>
        /// List of ids of shader modules this shader module is incompatible.
        /// </summary>
        public List<string> IncompatibleWith;
        
        /// <summary>
        /// List of templates this shader module uses.
        /// </summary>
        public List<ModuleTemplate> Templates;
        
        /// <summary>
        /// List functions this shader module uses.
        /// </summary>
        public List<ShaderFunction> Functions;
        
        /// <summary>
        /// string that can contain whatever you want, it is originally intended to contain serialized data that you may need for your own custom system based on the modular shader system.
        /// </summary>
        [HideInInspector] public string AdditionalSerializedData;
    }
}