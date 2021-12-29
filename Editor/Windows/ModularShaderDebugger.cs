using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace VRLabs.ModularShaderSystem
{
    /// <summary>
    /// Interface indicating a tab for the Modular Shader Debugger.
    /// </summary>
    /// <remarks>
    /// By implementing this interface in a custom class you can add a tab on the <see cref="ModularShaderDebugger"/> with your own debug tools.
    ///
    /// This can be useful in cases where you have specific implementations you want to track in your modular shader.
    /// </remarks>
    public interface IModularShaderDebuggerTab
    { 
        /// <summary>
        /// VisualElement that will be visualized in the tab. Your ui goes here. Remember to initialize it in your constructor.
        /// </summary>
        VisualElement TabContainer { get; set; }
        
        /// <summary>
        /// Name of the tab.
        /// </summary>
        string TabName { get; set; }

        /// <summary>
        /// Function called when updating the shader field (or refreshing). Changes in the data of your ui should go here
        /// </summary>
        /// <param name="shader">New shader being shown in the debugger</param>
        void UpdateTab(ModularShader shader);
    }
    
    /// <summary>
    /// Debugger Window for modular shaders. In here you can check various data visualization for your modular shader.
    /// </summary>
    /// <remarks>
    /// When creating and editing modules you may need to get some information (example: template dependency) without checking manually each module asset. This window can help you get said information rapidly.
    ///
    /// It can also be expanded with custom tabs for your own needs by implementing the <see cref="IModularShaderDebuggerTab"/> interface.
    /// </remarks>
    public class ModularShaderDebugger : EditorWindow
    {
        [MenuItem(MSSConstants.WINDOW_PATH + "/Modular Shader Debugger")]
        public static void ShowExample()
        {
            ModularShaderDebugger wnd = GetWindow<ModularShaderDebugger>();
            wnd.titleContent = new GUIContent("Modular Shader Debugger");
            
            if (wnd.position.width < 300 || wnd.position.height < 300)
            {
                Rect size = wnd.position;
                size.width = 1280;
                size.height = 720;
                wnd.position = size;
            }

        }
        
        private ObjectField _modularShaderField;
        private ModularShader _modularShader;
        private VisualElement _selectedTab;

        private List<IModularShaderDebuggerTab> _tabs;

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            
            var styleSheet = Resources.Load<StyleSheet>(MSSConstants.RESOURCES_FOLDER + "/MSSUIElements/ModularShaderDebuggerStyle");
            root.styleSheets.Add(styleSheet);

            _modularShaderField = new ObjectField("Shader");
            _modularShaderField.objectType = typeof(ModularShader);
            _modularShaderField.RegisterCallback<ChangeEvent<UnityEngine.Object>>(e =>
            {
                if (_modularShaderField.value != null)
                    _modularShader = (ModularShader)_modularShaderField.value;
                else
                    _modularShader = null;
                
                UpdateTabs();
            });

            _tabs = new List<IModularShaderDebuggerTab>();

            var buttonRow = new VisualElement();
            buttonRow.AddToClassList("button-tab-area");

            _selectedTab = new VisualElement();
            _selectedTab.style.flexGrow = 1;

            root.Add(_modularShaderField);
            root.Add(buttonRow);
            root.Add(_selectedTab);
            
            // Find all tabs in assemblies
            var tabTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => x.GetInterface(typeof(IModularShaderDebuggerTab).FullName) != null)
                .OrderBy(x => x.Name)
                .ToList();

            // Instance tabs and tab buttons
            foreach (var type in tabTypes)
            {
                var tab = Activator.CreateInstance(type) as IModularShaderDebuggerTab;
                
                var tabButton = new Button();
                tabButton.text = tab?.TabName;
                tabButton.AddToClassList("button-tab");
                
                tabButton.clicked += () =>
                {
                    foreach (var button in buttonRow.Children())
                        if(button.ClassListContains("button-tab-selected"))
                            button.RemoveFromClassList("button-tab-selected");
                    
                    tabButton.AddToClassList("button-tab-selected");
                   
                    _selectedTab.Clear();
                    _selectedTab.Add(tab.TabContainer);
                };
                
                buttonRow.Add(tabButton);
                _tabs.Add(tab);
            }

            if (_tabs.Count == 0) return;
            // Make sure the 2 base tabs are the first tabs of the row, other tabs will be after in alphabetical order
            var graph = _tabs.FirstOrDefault(x => x.GetType() == typeof(TemplateGraph));
            var timeline = _tabs.FirstOrDefault(x => x.GetType() == typeof(FunctionTimeline));

            if (timeline != null)
            {
                var index = _tabs.IndexOf(timeline);
                var button = buttonRow[index];
                _tabs.RemoveAt(index);
                buttonRow.RemoveAt(index);
                _tabs.Insert(0, timeline);
                buttonRow.Insert(0, button);
            }
            if (graph != null)
            {
                var index = _tabs.IndexOf(graph);
                var button = buttonRow[index];
                _tabs.RemoveAt(index);
                buttonRow.RemoveAt(index);
                _tabs.Insert(0, graph);
                buttonRow.Insert(0, button);
            }
            
            buttonRow[0].AddToClassList("button-tab-selected");
            _selectedTab.Add(_tabs[0].TabContainer);
        }

        private void UpdateTabs()
        {
            foreach (IModularShaderDebuggerTab tab in _tabs)
            {
                tab.UpdateTab(_modularShader);
            }
        }
    }
}