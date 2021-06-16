using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VRLabs.ModularShaderSystem
{
    public class TemplateAsset : ScriptableObject
    {
        public string Template;

        public TemplateAsset(string template)
        {
            Template = template;
        }
        public TemplateAsset() : this("") { }
        
        //TODO: add preview icon
        [MenuItem("Assets/Create/Modular Shader/Template", priority = 9)]
        private static void CreateTemplate()
        {
            Type projectWindowUtilType = typeof(ProjectWindowUtil);
            MethodInfo getActiveFolderPath = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
            object obj = getActiveFolderPath.Invoke(null, new object[0]);
            string pathToCurrentFolder = obj.ToString();
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(pathToCurrentFolder + "/Template.stemplate");
            
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewAsset>(), uniquePath, null, (string) null);
        }
        
        internal class DoCreateNewAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                File.WriteAllText(pathName, "");
                AssetDatabase.Refresh();
                Object o = AssetDatabase.LoadAssetAtPath<Object>(pathName);
                Selection.activeObject = o;
            }

            public override void Cancelled(int instanceId, string pathName, string resourceFile) => Selection.activeObject = (Object) null;
        }
    }
}