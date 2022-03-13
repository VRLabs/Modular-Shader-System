using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using VRLabs.ModularShaderSystem;

namespace VRLabs.ModularShaderSystem.Tools
{
    /// <summary>
    /// Editor window to let users embed the modular shader system into another library.
    /// </summary>
    public class EmbedLibraryWindow : EditorWindow
    {
        [MenuItem(MSSConstants.WINDOW_PATH + "/Tools/Embed Library", priority = 102)]
        public static void CreateWindow()
        {
            var window = GetWindow<EmbedLibraryWindow>();
            window.titleContent = new GUIContent("Embed Library");
            window.Show();
        }

        [Serializable]
        private class LibrarySettings
        {
            public string nmsc;
            public string variableSink;
            public string codeSink;
            public string propKeyword;
            public string tmpExtension;
            public string tmpclExtension;
            public string rscfName;
            public string windowPath;
            public string createPath;
        }

        private const string PATH = "Assets/VRLabs/ModularShaderSystem/Editor";
        private const string NAMESPACE = "VRLabs.ModularShaderSystem";

        private static readonly Regex NamespaceRegex = new Regex("^[a-zA-Z0-9.]*$");

        private TextField _namespaceField;
        private TextField _codeKeywordField;
        private TextField _variableKeywordField;
        private TextField _propertiesKeyword;
        private TextField _templateExtension;
        private TextField _templateCollectionExtension;
        private TextField _resourceFolderField;
        private TextField _windowPathField;
        private TextField _createPathField;
        private Label _namespaceLabel;
        private Button _embedButton;
        private Button _loadButton;
        private Button _saveButton;

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Import UXML
            var visualTree = Resources.Load<VisualTreeAsset>(MSSConstants.RESOURCES_FOLDER + "/MSSUIElements/EmbedLibraryWindow");
            VisualElement labelFromUxml = visualTree.CloneTree();
            root.Add(labelFromUxml);

            _namespaceField = root.Q<TextField>("NamespaceField");
            _variableKeywordField = root.Q<TextField>("VariableKeywordField");
            _codeKeywordField = root.Q<TextField>("CodeKeywordField");
            _propertiesKeyword = root.Q<TextField>("PropertiesKeywordField");
            _templateExtension = root.Q<TextField>("ExtensionField");
            _templateCollectionExtension = root.Q<TextField>("CollectionExtensionField");
            _resourceFolderField = root.Q<TextField>("ResourceFolderField");
            _windowPathField = root.Q<TextField>("WindowPathField");
            _createPathField = root.Q<TextField>("CreatePathField");
            _namespaceLabel = root.Q<Label>("NamespacePreview");
            _embedButton = root.Q<Button>("EmbedButton");
            _loadButton = root.Q<Button>("LoadButton");
            _saveButton = root.Q<Button>("SaveButton");

            _embedButton.clicked += EmbedButtonOnclick;
            _loadButton.clicked += LoadSettingsFromFile;
            _saveButton.clicked += SaveSettingsOnFile;

            _namespaceField.RegisterValueChangedCallback(x =>
            {
                if (NamespaceRegex.IsMatch(x.newValue))
                    _namespaceLabel.text = x.newValue + ".ModularShaderSystem";
                else
                    _namespaceField.value = x.previousValue;
            });

            // A stylesheet can be added to a VisualElement.
            // The style will be applied to the VisualElement and all of its children.
            //var styleSheet = Resources.Load<StyleSheet>("MSSUIElements/EmberLibraryWindow");
            //VisualElement labelWithStyle = new Label("Hello World! With Style");
            //labelWithStyle.styleSheets.Add(styleSheet);
            //root.Add(labelWithStyle);
        }

        private void EmbedButtonOnclick()
        {
            if (!Directory.Exists(PATH))
            {
                EditorUtility.DisplayDialog("Error", "Modular shader system has not been found in its default location, consider deleting it and reinstalling it using the official UnityPackage.", "Ok");
                return;
            }

            string path = EditorUtility.OpenFolderPanel("Select editor folder to use", "Assets", "Editor");
            if (path.Length == 0)
                return;

            if (!Path.GetFileName(path).Equals("Editor"))
            {
                EditorUtility.DisplayDialog("Error", "The folder must be an \"Editor\" folder", "Ok");
                return;
            }

            path = GetPathRelativeToProject(path);

            if (Directory.Exists(path + "/ModularShaderSystem"))
                Directory.Delete(path + "/ModularShaderSystem", true);

            CopyDirectory(PATH, path, "", false);

            string licencePath = PATH.Replace("Editor", "LICENSE");
            if (File.Exists(licencePath))
                File.Copy(licencePath, path + "/ModularShaderSystem/LICENSE");

            AssetDatabase.Refresh();
            
            MethodInfo SetIconForObject = typeof(EditorGUIUtility).GetMethod("SetIconForObject", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo CopyMonoScriptIconToImporters = typeof(MonoImporter).GetMethod("CopyMonoScriptIconToImporters", BindingFlags.Static | BindingFlags.NonPublic);

            SetIconToScript(SetIconForObject, CopyMonoScriptIconToImporters, $"{path}/ModularShaderSystem/Scriptables/ModularShader.cs", $"{_resourceFolderField.value}/ModularShaderIcon");
            SetIconToScript(SetIconForObject, CopyMonoScriptIconToImporters, $"{path}/ModularShaderSystem/Scriptables/ShaderModule.cs", $"{_resourceFolderField.value}/ShaderModuleIcon");
            SetIconToScript(SetIconForObject, CopyMonoScriptIconToImporters, $"{path}/ModularShaderSystem/Scriptables/TemplateAsset.cs", $"{_resourceFolderField.value}/TemplateIcon");
            SetIconToScript(SetIconForObject, CopyMonoScriptIconToImporters, $"{path}/ModularShaderSystem/Scriptables/TemplateCollectionAsset.cs", $"{_resourceFolderField.value}/TemplateCollectionIcon");
        }

        private void SetIconToScript(MethodInfo SetIconForObject, MethodInfo CopyMonoScriptIconToImporters, string scriptPath, string iconResourcePath)
        {
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            Texture2D icon = Resources.Load<Texture2D>(iconResourcePath);
            SetIconForObject.Invoke(null, new object[] { script, icon });
            CopyMonoScriptIconToImporters.Invoke(null, new object[] { script });
        }

        private void SaveSettingsOnFile()
        {
            string path = EditorUtility.SaveFilePanel("Save settings to file", "Assets", "embedSettings.json", "json");
            if (path.Length == 0)
                return;

            LibrarySettings settings = new LibrarySettings
            {
                nmsc = _namespaceField.value,
                variableSink = _variableKeywordField.value,
                codeSink = _codeKeywordField.value,
                propKeyword = _propertiesKeyword.value,
                tmpExtension = _templateExtension.value,
                tmpclExtension = _templateCollectionExtension.value,
                rscfName = _resourceFolderField.value,
                windowPath = _windowPathField.value,
                createPath = _createPathField.value
            };

            File.WriteAllText(path, JsonUtility.ToJson(settings));
        }

        private void LoadSettingsFromFile()
        {
            string path = EditorUtility.OpenFilePanel("Load settings", "Assets", "json");
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("Error", "File does not exist", "Ok");
                return;
            }

            var settings = JsonUtility.FromJson<LibrarySettings>(File.ReadAllText(path));

            _namespaceField.value = settings.nmsc;
            _variableKeywordField.value = settings.variableSink;
            _codeKeywordField.value = settings.codeSink;
            _propertiesKeyword.value = settings.propKeyword;
            _templateExtension.value = settings.tmpExtension;
            _templateCollectionExtension.value = settings.tmpclExtension;
            _resourceFolderField.value = settings.rscfName;
            _windowPathField.value = settings.windowPath;
            _createPathField.value = settings.createPath;
        }


        private void CopyDirectory(string oldPath, string newPath, string subpath, bool keepComments)
        {
            foreach (var file in Directory.GetFiles(oldPath).Where(x => !Path.GetExtension(x).Equals(".meta")))
            {
                if (Path.GetFileName(file).Contains("EmbedLibraryWindow")) continue;

                if (new [] {".cs", ".uxml", ".asmdef"}.Contains(Path.GetExtension(file)))
                {
                    var lines = new List<string>(File.ReadAllLines(file));
                    var newLines = lines.Where(line => !line.Trim().StartsWith("//")).ToList();
                    string text = string.Join(System.Environment.NewLine, newLines);

                    text = text.Replace(NAMESPACE, _namespaceField.value + ".ModularShaderSystem");

                    if (Path.GetFileName(file).Equals("MSSConstants.cs"))
                    {
                        text = text.Replace($"\"{MSSConstants.DEFAULT_CODE_KEYWORD}\"", $"\"{_codeKeywordField.value}\"");
                        text = text.Replace($"\"{MSSConstants.DEFAULT_VARIABLES_KEYWORD}\"", $"\"{_variableKeywordField.value}\"");
                        text = text.Replace($"\"{MSSConstants.TEMPLATE_PROPERTIES_KEYWORD}\"", $"\"{_propertiesKeyword.value}\"");
                        text = text.Replace($"\"{MSSConstants.TEMPLATE_EXTENSION}\"", $"\"{_templateExtension.value}\"");
                        text = text.Replace($"\"{MSSConstants.TEMPLATE_COLLECTION_EXTENSION}\"", $"\"{_templateCollectionExtension.value}\"");
                        text = text.Replace($"\"{MSSConstants.WINDOW_PATH}\"", $"\"{_windowPathField.value}\"");
                        text = text.Replace($"\"{MSSConstants.CREATE_PATH}\"", $"\"{_createPathField.value}\"");
                        text = text.Replace($"\"{MSSConstants.RESOURCES_FOLDER}\"", $"\"{_resourceFolderField.value}\"");
                    }

                    string finalPath = file.Replace(oldPath, newPath + "/ModularShaderSystem" + subpath);
                    Directory.CreateDirectory(Path.GetDirectoryName(finalPath) ?? string.Empty);
                    File.WriteAllText(finalPath, text);
                }
                else if (Path.GetDirectoryName(file).Contains("Resources"))
                {
                    string finalPath = file.Replace(oldPath, newPath + "/ModularShaderSystem" + subpath);
                    Directory.CreateDirectory(Path.GetDirectoryName(finalPath) ?? string.Empty);
                    //finalPath = finalPath.Substring(finalPath.IndexOf("Assets", StringComparison.Ordinal));
                    AssetDatabase.CopyAsset(file, finalPath);

                    if (Path.GetExtension(finalPath).Equals(".uss"))
                    {
                        string text = File.ReadAllText(finalPath);
                        text = text.Replace($"resource(\"{MSSConstants.RESOURCES_FOLDER}/", $"resource(\"{_resourceFolderField.value}/");
                        File.WriteAllText(finalPath, text);
                    }
                }
                else if (Path.GetFileName(file).Equals("LICENSE"))
                {
                    string finalPath = file.Replace(oldPath, newPath + "/ModularShaderSystem" + subpath);
                    File.Copy(file, finalPath);
                }
            }

            foreach (string directory in Directory.GetDirectories(oldPath))
            {
                if (Path.GetFileName(directory).Equals("Tools")) continue;

                string newSubPath = subpath + "/" + Path.GetFileName(directory);
                if (Path.GetFileName(directory).Equals(MSSConstants.RESOURCES_FOLDER) && Path.GetFileName(Path.GetDirectoryName(directory)).Equals("Resources"))
                    newSubPath = subpath + "/" + _resourceFolderField.value;
                CopyDirectory(directory, newPath, newSubPath, keepComments);
            }
        }
        
        private static string GetPathRelativeToProject(string path)
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"The folder \"{path}\" is not found");

            if (!path.Contains(Application.dataPath) && !path.StartsWith("Assets"))
                throw new DirectoryNotFoundException($"The folder \"{path}\" is not part of the unity project");

            if(!path.StartsWith("Assets"))
                path = path.Replace(Application.dataPath, "Assets");
            
            return path;
        }
    }
}