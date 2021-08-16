using System;
using UnityEditor;
using UnityEngine;

namespace VRLabs.ModularShaderSystem
{
    public class ShaderModuleGenerationTest : EditorWindow
    {
        [MenuItem(MSSConstants.WINDOW_PATH + "/Test generator")]
        private static void ShowWindow()
        {
            var window = GetWindow<ShaderModuleGenerationTest>();
            window.titleContent = new GUIContent("Test Generator");
            window.Show();
        }

        private ModularShader _shader;
        private string _message = "";
        private GUIStyle _style;
        
        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(EditorStyles.label);
                _style.wordWrap = true;
            }
            
            _shader = (ModularShader)EditorGUILayout.ObjectField("Modular shader", _shader, typeof(ModularShader), false);

            if (GUILayout.Button("Generate") && _shader != null)
            {
                var g = new ShaderGenerator();

                var response = ShaderGenerator.VerifyShaderModules(_shader);
                
                switch(response)
                {
                    case VerificationResponse.NoIssues:
                        g.GenerateMainShader("Assets", _shader);
                        _message = "";
                        break;
                    case VerificationResponse.DuplicateModule:
                        _message = "Error: Duplicate modules found";
                        break;
                    case VerificationResponse.MissingDependencies:
                        _message = "Error: Missing dependency modules";
                        break;
                    case VerificationResponse.IncompatibleModules:
                        _message = "Error: Some modules are incompatible with each other";
                        break;
                    case VerificationResponse.MissingPropertiesFromTemplates:
                        _message = "Error: The modular shader or some of its modules do not declare some properties in their templates and the shader is set to require properties from templates";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            GUILayout.Label(_message, _style);
            
        }
    }
}