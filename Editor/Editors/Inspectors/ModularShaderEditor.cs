using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace VRLabs.ModularShaderSystem.UI
{
    /// <summary>
    /// Inspector for the <see cref="ModularShader"/> asset.
    /// </summary>
    [CustomEditor(typeof(ModularShader))]
    public class ModularShaderEditor : Editor
    {
        private VisualElement _root;
        private ModularShader _shader;
        
        public override VisualElement CreateInspectorGUI()
        {
            _root = new VisualElement();
            _shader = (ModularShader)serializedObject.targetObject;
            
            // Temporary code to move default textures to the new place, will be removed sometime in the future
            if (_shader.DefaultTextures == null) _shader.DefaultTextures = new List<DefaultTexture>();
            foreach (var prop in _shader.Properties)
            {
#pragma warning disable CS0612
                if (prop.DefaultTextureAsset != null)
                {
                    _shader.DefaultTextures.Add(new DefaultTexture{PropertyName = prop.Name, Texture = prop.DefaultTextureAsset});
                }
#pragma warning restore CS0612
            }
            
            
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
            
            var generateButton = _root.Q<Button>("RegenerateShaderButton");

            generateButton.clicked += () =>
            {
                var _issues = ShaderGenerator.CheckShaderIssues(_shader);
                if (_issues.Count > 0)
                {
                    EditorUtility.DisplayDialog("Error", $"The modular shader has issues that must be resolved before generating the shader:\n  {string.Join("\n  ", _issues)}", "Ok");
                    return;
                }

                string path = EditorUtility.OpenFolderPanel("Select folder", "Assets", "");
                if (path.Length == 0)
                    return;

                string localPath = Environment.CurrentDirectory;
                localPath = localPath.Replace('\\', '/');
                path = path.Replace(localPath + "/", "");
                ShaderGenerator.GenerateShader(path, _shader);
            };

            return _root;
        }
    }
}