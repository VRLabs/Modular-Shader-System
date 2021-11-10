using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace VRLabs.ModularShaderSystem
{
    public class ModularShaderDataViewer : EditorWindow
    {
        [MenuItem(MSSConstants.WINDOW_PATH + "/Modular shader data view")]
        public static void ShowExample()
        {
            ModularShaderDataViewer wnd = GetWindow<ModularShaderDataViewer>();
            wnd.titleContent = new GUIContent("ModularShaderDataViewer");
        }
        
        private ObjectField _modularShaderField;
        private Foldout _propertiesFoldout;
        private Foldout _variablesFoldout;
        private Foldout _functionsFoldout;
        private ModularShader _modularShader;

        private VisualTreeAsset _propertyViewUxml;
        private VisualTreeAsset _functionViewUxml;

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Import UXML
            var visualTree = Resources.Load<VisualTreeAsset>(MSSConstants.RESOURCES_FOLDER + "/MSSUIElements/ModularShaderDataViewer");
            _propertyViewUxml = Resources.Load<VisualTreeAsset>(MSSConstants.RESOURCES_FOLDER + "/MSSUIElements/PropertyView");
            _functionViewUxml = Resources.Load<VisualTreeAsset>(MSSConstants.RESOURCES_FOLDER + "/MSSUIElements/FunctionView");
            VisualElement uxmlBase = visualTree.CloneTree();
            root.Add(uxmlBase);
            
            // Query all needed fields
            _modularShaderField = root.Q<ObjectField>("ModularShaderField");
            _propertiesFoldout = root.Q<Foldout>("PropertiesFoldout");
            _variablesFoldout = root.Q<Foldout>("VariablesFoldout");
            _functionsFoldout = root.Q<Foldout>("FunctionsFoldout");
            
            // Setup object picker
            _modularShaderField.objectType = typeof(ModularShader);
            _modularShaderField.RegisterCallback<ChangeEvent<UnityEngine.Object>>(e =>
            {
                if (_modularShaderField.value != null)
                    _modularShader = (ModularShader)_modularShaderField.value;
                else
                    _modularShader = null;

                PopulateLists();
            
            });
            
            // Add stylesheet
            var styleSheet = Resources.Load<StyleSheet>(MSSConstants.RESOURCES_FOLDER + "/MSSUIElements/ModularShaderDataViewerStyle");
            root.styleSheets.Add(styleSheet);
        }

        private void PopulateLists()
        {
            _propertiesFoldout.Clear();
            var properties = ShaderGenerator.FindAllProperties(_modularShader).OrderBy(x => x.Type).ThenBy(x => x.Name);;
            foreach (Property property in properties)
            {
                VisualElement p = _propertyViewUxml.CloneTree();
                var name = p.Q<Label>("PropertyLabel");
                var type = p.Q<Label>("TypeLabel");
                name.text = property.Name;
                type.text = property.Type;
                
                _propertiesFoldout.Add(p);
                
            }
            
            var functions = ShaderGenerator.FindAllFunctions(_modularShader);
            
            _variablesFoldout.Clear();
            GetVariables(functions, MSSConstants.DEFAULT_VARIABLES_KEYWORD, true);
            foreach (var sink in functions.SelectMany(x => x.VariableKeywords).Distinct().Where(x => !string.IsNullOrEmpty(x) && !x.Equals(MSSConstants.DEFAULT_VARIABLES_KEYWORD)))
                GetVariables(functions, sink);
            
            _functionsFoldout.Clear();
            foreach (var functionsGroup in functions.Where(x => x.AppendAfter.StartsWith("#K#")).GroupBy(x => x.AppendAfter))
            {
                VisualElement p = _functionViewUxml.CloneTree();
                var name = p.Q<Label>("FunctionLabel");
                var foldout = p.Q<Foldout>("FoldoutArea");
                foldout.text = functionsGroup.Key;
                name.RemoveFromHierarchy();
                _functionsFoldout.Add(p);

                foreach (ShaderFunction function in functionsGroup.OrderBy(x => x.Queue))
                    CreateFunctionsHierarchy(functions, function, foldout);   
                
            }

        }
        
        private void GetVariables(List<ShaderFunction> functions, string sink, bool isDefaultSink = false)
        {
            var variables = functions
                .Where(x => (isDefaultSink && x.VariableKeywords.Count == 0) || x.VariableKeywords.Any(y => y.Equals(sink)))
                .SelectMany(x => x.UsedVariables)
                .Distinct()
                .OrderBy(x => x.Type).ThenBy(x => x.Name).ToList();
            if (variables.Count == 0) return;
            var foldout = new Foldout{  text = $"#K#{sink}" };
            
            foreach (var variable in variables)
            {
                VisualElement p = _propertyViewUxml.CloneTree();
                var name = p.Q<Label>("PropertyLabel");
                var type = p.Q<Label>("TypeLabel");
                name.text = variable.Name;
                type.text = Enum.GetName(typeof(VariableType),variable.Type);
                foldout.Add(p);
                
                if (variable.Type.Equals("sampler2D") || variable.Type.Equals("Texture2D"))
                {
                    VisualElement p2 = _propertyViewUxml.CloneTree();
                    var name2 = p.Q<Label>("PropertyLabel");
                    var type2 = p.Q<Label>("TypeLabel");
                    name2.text = $"{variable.Name}_ST";
                    type2.text = "float4";
                    foldout.Add(p2);
                }
            }

            _variablesFoldout.Add(foldout);
        }
        
        private void CreateFunctionsHierarchy(List<ShaderFunction> functions, ShaderFunction function, VisualElement parent)
        {
            var subFunctions = functions.Where(x => x.AppendAfter.Equals(function.Name)).OrderBy(x => x.Queue).ToList();
            if (subFunctions.Count > 0)
            {
                VisualElement p = _functionViewUxml.CloneTree();
                var name = p.Q<Label>("FunctionLabel");
                var priority = p.Q<Label>("PriorityLabel");
                var foldout = p.Q<Foldout>("FoldoutArea");
                foldout.text = function.Name;
                name.RemoveFromHierarchy();
                priority.text = function.Queue.ToString();
                parent.Add(p);
                
                foreach (var fn in functions.Where(x => x.AppendAfter.Equals(function.Name)).OrderBy(x => x.Queue))
                    CreateFunctionsHierarchy(functions, fn, foldout);
            }
            else
            {
                VisualElement p = _functionViewUxml.CloneTree();
                var name = p.Q<Label>("FunctionLabel");
                var priority = p.Q<Label>("PriorityLabel");
                var foldout = p.Q<Foldout>("FoldoutArea");
                foldout.RemoveFromHierarchy();
                name.text = function.Name;
                priority.text = function.Queue.ToString();
                
                parent.Add(p);
            }
        }
    }
}