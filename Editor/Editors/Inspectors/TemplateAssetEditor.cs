using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace VRLabs.ModularShaderSystem.UI
{
    /// <summary>
    /// Inspector for the <see cref="TemplateAsset"/> asset
    /// </summary>
    [CustomEditor(typeof(TemplateAsset))]
    public class TemplateAssetEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            CodeViewElement element = new CodeViewElement();
            element.Text = serializedObject.FindProperty("Template").stringValue;
            element.style.minHeight = 600;
            return element;
        }
        
    }
}