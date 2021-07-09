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
        
        private void OnGUI()
        {
            shader = (ModularShader)EditorGUILayout.ObjectField("Modular shader", shader, typeof(ModularShader), false);

            if (GUILayout.Button("Generate") && shader != null)
            {
                var g = new ShaderGenerator();
                g.GenerateMainShader("Assets", shader);
            }
            
        }
    }
}