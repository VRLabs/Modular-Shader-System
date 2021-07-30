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
            var visualTree = Resources.Load<VisualTreeAsset>(MSSConstants.RESOURCES_FOLDER + "/MSSUIElements/ModularShaderEditor");
            VisualElement template = visualTree.CloneTree();

            //templateKeywordList.
            _root.Add(template);
            
            var baseModulesField = _root.Q<InspectorList>("BaseModulesField");
            bool areModulesEditable = !serializedObject.FindProperty("LockBaseModules").boolValue;
            if(!areModulesEditable)
                baseModulesField.SetFoldingState(true);
            baseModulesField.SetEnabled(areModulesEditable);
            
            var objectField = _root.Q<ObjectField>("ShaderTemplate");
            objectField.objectType = typeof(TemplateAsset);

            return _root;
        }
    }
}