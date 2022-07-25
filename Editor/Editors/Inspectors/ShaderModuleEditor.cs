using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace VRLabs.ModularShaderSystem.UI
{
    /// <summary>
    /// Inspector for the <see cref="ShaderModule"/> asset.
    /// </summary>
    [CustomEditor(typeof(ShaderModule))]
    public class ShaderModuleEditor : Editor
    {
        private VisualElement _root;

        public override VisualElement CreateInspectorGUI()
        {
            // Each editor window contains a root VisualElement object
            _root = new VisualElement();
            
            // Temporary code to move default textures to the new place, will be removed sometime in the future
            var module = (ShaderModule)serializedObject.targetObject;
            if (module.DefaultTextures == null) module.DefaultTextures = new List<DefaultTexture>();
            foreach (var prop in module.Properties)
            {
#pragma warning disable CS0612
                if (prop.DefaultTextureAsset != null)
                {
                    module.DefaultTextures.Add(new DefaultTexture{PropertyName = prop.Name, Texture = prop.DefaultTextureAsset});
                }
#pragma warning restore CS0612
            }

            // Import UXML
            var visualTree = Resources.Load<VisualTreeAsset>(MSSConstants.RESOURCES_FOLDER + "/MSSUIElements/ShaderModuleEditor");
            VisualElement template = visualTree.CloneTree();
            _root.Add(template);

            return _root;
        }
    }
}