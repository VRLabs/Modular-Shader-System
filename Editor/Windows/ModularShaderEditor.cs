using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace VRLabs.ModularShaderSystem
{
    [CustomEditor(typeof(ModularShader))]
    public class ModularShaderEditor : Editor
    {
        private VisualElement _root;
        
        public override VisualElement CreateInspectorGUI()
        {
            _root = new VisualElement();
            // Import UXML
            var visualTree = Resources.Load<VisualTreeAsset>("MSSUIElements/ModularShaderEditor");
            var all = Resources.LoadAll("MSSUIElements");
            VisualElement template = visualTree.CloneTree();

            //templateKeywordList.
            _root.Add(template);
            
            var objectField = _root.Q<ObjectField>("ShaderTemplate");
            objectField.objectType = typeof(TemplateAsset);

            return _root;
        }
    }
}