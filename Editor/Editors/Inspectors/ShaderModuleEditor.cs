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

            // Import UXML
            var visualTree = Resources.Load<VisualTreeAsset>(MSSConstants.RESOURCES_FOLDER + "/MSSUIElements/ShaderModuleEditor");
            VisualElement template = visualTree.CloneTree();
            _root.Add(template);

            return _root;
        }
    }
}