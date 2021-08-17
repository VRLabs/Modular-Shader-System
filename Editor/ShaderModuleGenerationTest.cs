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
            window.titleContent = new GUIContent("TITLE");
            window.Show();
        }

        private ModularShader shader;
        private string message = "";
        
        private void OnGUI()
        {
            shader = (ModularShader)EditorGUILayout.ObjectField("Modular shader", shader, typeof(ModularShader), false);

            if (GUILayout.Button("Generate") && shader != null)
            {
                var g = new ShaderGenerator();

                var response = ShaderGenerator.VerifyShaderModules(shader);
                
                switch(response)
                {
                    case VerificationResponse.NoIssues:
                        g.GenerateMainShader("Assets", shader);
                        message = "";
                        break;
                    case VerificationResponse.DuplicateModule:
                        message = "Error: Duplicate modules found";
                        break;
                    case VerificationResponse.MissingDependencies:
                        message = "Error: Missing dependency modules";
                        break;
                    case VerificationResponse.IncompatibleModules:
                        message = "Error: Some modules are incompatible with each other";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            EditorGUILayout.LabelField(message);
            
        }
    }
}