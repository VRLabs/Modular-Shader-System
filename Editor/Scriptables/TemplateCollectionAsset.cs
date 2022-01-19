using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VRLabs.ModularShaderSystem
{
    /// <summary>
    /// Asset containing multiple templates separated by special keywords.
    /// </summary>
    /// <remarks>
    /// Each template is marked by starting a "#T#TEMPLATE_NAME" and ends whenever a new template keyword is declared or the end of the file.
    /// </remarks>
    public class TemplateCollectionAsset : ScriptableObject
    {
        /// <summary>
        /// List of templates contained in this collection.
        /// </summary>
        public List<TemplateAsset> Templates;

        public TemplateCollectionAsset()
        {
            Templates = new List<TemplateAsset>();
        }

        //public override string ToString() => Template;

        //TODO: add preview icon
        [MenuItem("Assets/Create/" + MSSConstants.CREATE_PATH + "/Template Collection", priority = 9)]
        private static void CreateTemplate()
        {
            Type projectWindowUtilType = typeof(ProjectWindowUtil);
            MethodInfo getActiveFolderPath = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
            object obj = getActiveFolderPath.Invoke(null, new object[0]);
            string pathToCurrentFolder = obj.ToString();
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath($"{pathToCurrentFolder}/Template.{MSSConstants.TEMPLATE_COLLECTION_EXTENSION}");
            
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