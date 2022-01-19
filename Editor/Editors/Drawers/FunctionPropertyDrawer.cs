using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace VRLabs.ModularShaderSystem.UI
{
    /// <summary>
    /// Inspector drawer for <see cref="ShaderFunction"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(ShaderFunction))]
    public class FunctionPropertyDrawer : PropertyDrawer
    {
        private VisualElement _root;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Each editor window contains a root VisualElement object
            _root = new VisualElement();

            // Import UXML
            var visualTree =  Resources.Load<VisualTreeAsset>(MSSConstants.RESOURCES_FOLDER + "/MSSUIElements/FunctionPropertyDrawer");
            VisualElement template = visualTree.CloneTree();
            var foldout = new Foldout();
            foldout.text = property.displayName;
            foldout.RegisterValueChangedCallback((e) => property.isExpanded = e.newValue);
            foldout.value = property.isExpanded;
            
            var nameField = template.Q<TextField>("Name");
            nameField.RegisterValueChangedCallback(evt => foldout.text = evt.newValue);
            
            foldout.Add(template);
            _root.Add(foldout);

            return _root;
        }
    }
}