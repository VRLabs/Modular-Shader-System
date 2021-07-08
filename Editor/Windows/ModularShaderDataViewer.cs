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
        [MenuItem("VRLabs/Modular Shader/Modular shader data view")]
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
            var visualTree = Resources.Load<VisualTreeAsset>("MSSUIElements/ModularShaderDataViewer");
            _propertyViewUxml = Resources.Load<VisualTreeAsset>("MSSUIElements/PropertyView");
            _functionViewUxml = Resources.Load<VisualTreeAsset>("MSSUIElements/FunctionView");
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
                {
                    // 4
                    _modularShader = (ModularShader)_modularShaderField.value;
                    //_modularShaderSerialized = new SerializedObject(_modularShader);
                }
                else
                {
                    _modularShader = null;
                }

                PopulateLists();
            
            });
            
            // Add stylesheet
            var styleSheet = Resources.Load<StyleSheet>("MSSUIElements/ModularShaderDataViewer");
            root.styleSheets.Add(styleSheet);
        }

        private void PopulateLists()
        {
            // Update Properties
            _propertiesFoldout.Clear();
            var properties = ShaderGenerator.FindAllProperties(_modularShader);
            foreach (Property property in properties)
            {
                VisualElement p = _propertyViewUxml.CloneTree();
                var name = p.Q<Label>("PropertyLabel");
                var type = p.Q<Label>("TypeLabel");
                name.text = property.Name;
                type.text = property.Type;
                
                _propertiesFoldout.Add(p);
                
            }
            
            // Fetch all functions
            var functions = ShaderGenerator.FindAllFunctions(_modularShader);
            
            // Update variables
            _variablesFoldout.Clear();
            GetVariables(functions, MSSConstants.DEFAULT_VARIABLES_SINK, true);
            foreach (var sink in functions.Select(x => x.VariableSinkKeyword).Distinct().Where(x => !string.IsNullOrEmpty(x) && !x.Equals(MSSConstants.DEFAULT_VARIABLES_SINK)))
                GetVariables(functions, sink);
            
            // Update Functions
            _functionsFoldout.Clear();
            foreach (var functionsGroup in functions.Where(x => x.AppendAfter.StartsWith("#K#")).GroupBy(x => x.AppendAfter))
            {
                VisualElement p = _functionViewUxml.CloneTree();
                var name = p.Q<Label>("FunctionLabel");
                var foldout = p.Q<Foldout>("FoldoutArea");
                foldout.text = functionsGroup.Key;
                name.RemoveFromHierarchy();
                _functionsFoldout.Add(p);

                foreach (ShaderFunction function in functionsGroup.OrderBy(x => x.Priority))
                    CreateFunctionsHierarchy(functions, function, foldout);   
                
            }

        }
        
        private void GetVariables(List<ShaderFunction> functions, string sink, bool isDefaultSink = false)
        {
            var variables = functions
                .Where(x => x.VariableSinkKeyword.Equals(sink) || (isDefaultSink && string.IsNullOrWhiteSpace(x.VariableSinkKeyword)))
                .SelectMany(x => x.UsedVariables)
                .Distinct()
                .OrderBy(x => x.Type).ToList();
            if (variables.Count == 0) return;
            var foldout = new Foldout{  text = $"#K#{sink}" };
            
            foreach (var variable in variables)
            {
                VisualElement p = _propertyViewUxml.CloneTree();
                var name = p.Q<Label>("PropertyLabel");
                var type = p.Q<Label>("TypeLabel");
                name.text = variable.Name;
                type.text = variable.Type;
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
            var subFunctions = functions.Where(x => x.AppendAfter.Equals(function.Name)).OrderBy(x => x.Priority).ToList();
            if (subFunctions.Count > 0)
            {
                VisualElement p = _functionViewUxml.CloneTree();
                var name = p.Q<Label>("FunctionLabel");
                var priority = p.Q<Label>("PriorityLabel");
                var foldout = p.Q<Foldout>("FoldoutArea");
                foldout.text = function.Name;
                name.RemoveFromHierarchy();
                priority.text = function.Priority.ToString();
                parent.Add(p);
                
                foreach (var fn in functions.Where(x => x.AppendAfter.Equals(function.Name)).OrderBy(x => x.Priority))
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
                priority.text = function.Priority.ToString();
                
                parent.Add(p);
            }
        }
    }
}