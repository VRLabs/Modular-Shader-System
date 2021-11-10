using System;
using System.Collections.Generic;
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
        private GUIStyle _style;
        private List<string> _issues = new List<string>();
        
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
                _issues = ShaderGenerator.CheckShaderIssues(_shader);
                if (_issues.Count == 0)
                    ShaderGenerator.GenerateShader("Assets", _shader);
            }

            if (_issues.Count <= 0) return;
            foreach (string issue in _issues)
                EditorGUILayout.HelpBox(issue, MessageType.Error);
        }
    }
}