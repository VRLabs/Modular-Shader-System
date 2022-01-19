using UnityEditor;
using UnityEngine.UIElements;
using VRLabs.ModularShaderSystem.Debug;

namespace VRLabs.ModularShaderSystem.UI
{
    /// <summary>
    /// Editor window that shows a code element. should be shown with "EditorWindow.ShowAsDropDown".
    /// </summary>
    public class TextPopup : EditorWindow
    {
        public string Text;
        private void CreateGUI()
        {
            var viewer = new CodeViewElement();
            viewer.Text = Text;
            viewer.StretchToParentSize();
            var darkThemeStyleSheet = EditorGUIUtility.Load("StyleSheets/Generated/DefaultCommonDark_inter.uss.asset") as StyleSheet;
            rootVisualElement.styleSheets.Add(darkThemeStyleSheet);
            rootVisualElement.Add(viewer);
        }
    }
}