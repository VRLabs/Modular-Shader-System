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
        private ModularShader _shader;
        
        public override VisualElement CreateInspectorGUI()
        {
            _root = new VisualElement();
            _shader = (ModularShader)serializedObject.targetObject;
            // Import UXML
            var visualTree = Resources.Load<VisualTreeAsset>(MSSConstants.RESOURCES_FOLDER + "/MSSUIElements/ModularShaderEditor");
            VisualElement template = visualTree.CloneTree();

            //templateKeywordList.
            _root.Add(template);
            
            var baseModulesField = _root.Q<ModuleInspectorList>("BaseModulesField");
            bool areModulesEditable = !_shader.LockBaseModules;
            bool checkForProperties = _shader.UseTemplatesForProperties;
            if(!areModulesEditable)
                baseModulesField.SetFoldingState(true);
            baseModulesField.SetEnabled(areModulesEditable);

            var templateField = _root.Q<ObjectField>("ShaderTemplateField");
            templateField.objectType = typeof(TemplateAsset);
            
            var propertiesTemplateField = _root.Q<ObjectField>("ShaderPropertiesTemplateField");
            propertiesTemplateField.objectType = typeof(TemplateAsset);
            propertiesTemplateField.style.display = checkForProperties ? DisplayStyle.Flex : DisplayStyle.None;
            
            var useTemplatesField = _root.Q<Toggle>("UseTemplatesForPropertiesField");
            useTemplatesField.RegisterValueChangedCallback(x =>
            {
                propertiesTemplateField.style.display = x.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });

            return _root;
        }
    }
}