using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using VRLabs.ModularShaderSystem;

public class EmbedLibraryWindow : EditorWindow
{
    [MenuItem("VRLabs/Modular Shader/Embed Library")]
    public static void ShowExample()
    {
        EmbedLibraryWindow wnd = GetWindow<EmbedLibraryWindow>();
        wnd.titleContent = new GUIContent("Embed Library");
    }
    
    private const string PATH = "Assets/VRLabs/ModularShaderSystem/Editor";
    private const string NAMESPACE = "VRLabs.ModularShaderSystem";
    
    private static readonly Regex NamespaceRegex = new Regex("^[a-zA-Z0-9.]*$");

    private TextField _namespaceField;
    private TextField _codeSinkField;
    private TextField _variableSinkField;
    private TextField _templateExtension;
    private Label _namespaceLabel;
    private Button _embedButton;

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Import UXML
        var visualTree =Resources.Load<VisualTreeAsset>("MSSUIElements/EmberLibraryWindow");
        VisualElement labelFromUxml = visualTree.CloneTree();
        root.Add(labelFromUxml);
        
        _namespaceField = root.Q<TextField>("NamespaceField");
        _codeSinkField = root.Q<TextField>("VariableSinkField");
        _variableSinkField = root.Q<TextField>("CodeSinkField");
        _templateExtension = root.Q<TextField>("ExtensionField");
        _namespaceLabel = root.Q<Label>("NamespacePreview");
        _embedButton = root.Q<Button>("EmbedButton");
        
        _embedButton.clicked += EmbedButtonOnclick;

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
            EditorUtility.DisplayDialog("Error", "Modular shader system has not been found in its default location, consider deleting it and reinstalling it using the official UnityPackage.", "Ok");
        
        string path = EditorUtility.OpenFolderPanel("Select editor folder to use", "Assets", "Editor");
        if (path.Length == 0)
            return;

        if (!Path.GetFileName(path).Equals("Editor"))
        {
            EditorUtility.DisplayDialog("Error", "The folder must be an \"Editor\" folder", "Ok");
            return;
        }

        if (Directory.Exists(path + "/ModularShaderSystem"))
            Directory.Delete(path + "/ModularShaderSystem", true);
            
        CopyDirectory(PATH, path, _namespaceField.value, _codeSinkField.value, _variableSinkField.value, _templateExtension.value, "", false);

        AssetDatabase.Refresh();
    }


    private static void CopyDirectory(string oldPath, string newPath, string customNamespace, string codeSink, string variableSink, string extension, string subpath, bool keepComments)
        {
            foreach (var file in Directory.GetFiles(oldPath).Where(x => !Path.GetExtension(x).Equals(".meta")))
            {
                if (Path.GetExtension(file).Equals(".cs") || Path.GetExtension(file).Equals(".uxml"))
                {
                    var lines = new List<string>();
                    lines.AddRange(File.ReadAllLines(file));
                    int i = 0;
                    while (i < lines.Count && !keepComments)
                    {
                        int index = lines[i].IndexOf("//", StringComparison.Ordinal);
                        if (index != -1)
                        {
                            if (!string.IsNullOrEmpty(lines[i].Substring(0, index).Trim()))
                            {
                                lines[i] = lines[i].Substring(0, index);
                                i++;
                            }
                            else
                            {
                                lines.RemoveAt(i);
                            }
                        }
                        else
                        {
                            i++;
                        }
                    }

                    string text = string.Join(System.Environment.NewLine, lines);

                    text = text.Replace(NAMESPACE, customNamespace + ".ModularShaderSystem");

                    if (Path.GetFileName(file).Equals("MSSConstants.cs"))
                    {
                        text = text.Replace(MSSConstants.DEFAULT_CODE_SINK, codeSink);
                        text = text.Replace(MSSConstants.DEFAULT_VARIABLES_SINK, variableSink);
                        text = text.Replace(MSSConstants.TEMPLATE_EXTENSION, extension);
                    }

                    string finalPath = file.Replace(oldPath, newPath + "/ModularShaderSystem" + subpath);
                    Directory.CreateDirectory(Path.GetDirectoryName(finalPath) ?? string.Empty);
                    File.WriteAllText(finalPath, text);
                }
                else if (Path.GetDirectoryName(file).Contains("Resources"))
                {
                    string finalPath = file.Replace(oldPath, newPath + "/ModularShaderSystem" + subpath);
                    Directory.CreateDirectory(Path.GetDirectoryName(finalPath) ?? string.Empty);
                    finalPath = finalPath.Substring(finalPath.IndexOf("Assets", StringComparison.Ordinal));
                    AssetDatabase.CopyAsset(file, finalPath);
                }
            }

            foreach (string directory in Directory.GetDirectories(oldPath))
            {
                if (!Path.GetFileName(directory).Equals("Tools"))
                {
                    string newSubPath = subpath + directory.Replace(PATH + subpath, "");
                    CopyDirectory(directory, newPath, customNamespace, codeSink, variableSink, extension, newSubPath, keepComments);
                }
            }
        }
}