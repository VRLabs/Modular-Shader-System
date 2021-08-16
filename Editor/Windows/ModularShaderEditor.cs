using System.Collections.Generic;
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
        //    (?<=\]|\n)[^\[\]\(\)\/]+(?=\()   regex for retrieving a template
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
            bool areModulesEditable = !serializedObject.FindProperty("LockBaseModules").boolValue;
            bool checkForProperties = serializedObject.FindProperty("UseTemplatesForProperties").boolValue;;
            if(!areModulesEditable)
                baseModulesField.SetFoldingState(true);
            baseModulesField.SetEnabled(areModulesEditable);

            var templateField = _root.Q<ObjectField>("ShaderTemplateField");
            templateField.objectType = typeof(TemplateAsset);
            
            var propertiesTemplateField = _root.Q<ObjectField>("ShaderPropertiesTemplateField");
            propertiesTemplateField.objectType = typeof(TemplateAsset);
            propertiesTemplateField.style.display = checkForProperties ? DisplayStyle.Flex : DisplayStyle.None;
            
            var missingPropertiesField = _root.Q<Label>("MissingPropertiesField");

            baseModulesField.CheckForProperties = checkForProperties;

            if(checkForProperties)
                CheckShaderMissingProperties(missingPropertiesField);
            else
                propertiesTemplateField.style.display = DisplayStyle.None;

            var useTemplatesField = _root.Q<Toggle>("UseTemplatesForPropertiesField");
            useTemplatesField.RegisterValueChangedCallback(x =>
            {
                baseModulesField.CheckForProperties = x.newValue;
                propertiesTemplateField.style.display = x.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                
                if(x.newValue)
                    CheckShaderMissingProperties(missingPropertiesField);
                else
                    missingPropertiesField.style.display = DisplayStyle.None;

                baseModulesField.UpdateList();
            });
            propertiesTemplateField.RegisterValueChangedCallback(_ => CheckShaderMissingProperties(missingPropertiesField));

            return _root;
        }

        private void CheckShaderMissingProperties(Label missingPropertiesField)
        {
            List<string> missingProperties = ShaderGenerator.GetMissingPropertiesFromShaderTemplate(_shader, true);

            if (missingProperties.Count > 0)
            {
                missingPropertiesField.style.display = DisplayStyle.Flex;
                missingPropertiesField.text = "These properties are missing from the properties template: " + string.Join(", ", missingProperties);
            }
            else
            {
                missingPropertiesField.style.display = DisplayStyle.None;
            }
        }
    }
}