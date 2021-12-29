using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


namespace VRLabs.ModularShaderSystem
{
    public class TemplateGraph : IModularShaderDebuggerTab
    {
        public VisualElement TabContainer { get; set; }
        public string TabName { get; set; }
        
        private TemplateRow _column;
        private List<TemplatePair> _pairs;

        public TemplateGraph()
        {
            _column = new TemplateRow();
            _pairs = new List<TemplatePair>();
            
            TabName = "Template graph";
            TabContainer = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            TabContainer.AddToClassList("graph");
            TabContainer.style.flexGrow = 1;
            TabContainer.Add(_column);
            
            var styleSheet = Resources.Load<StyleSheet>(MSSConstants.RESOURCES_FOLDER + "/MSSUIElements/TemplateGraphStyle");
            TabContainer.styleSheets.Add(styleSheet);

        }
        
        public void UpdateTab(ModularShader shader)
        {
            _pairs.Clear();
            _column.Reset();
            if (shader == null) return;
            _column.AddBaseTemplate("Shader", shader.ShaderTemplate);
            
            if (shader.UseTemplatesForProperties)
            {
                var keywords = new []{"#K#" + MSSConstants.TEMPLATE_PROPERTIES_KEYWORD};
                _column.AddBaseTemplate("ShaderPropertiesRoot", new TemplateAsset{ Template = "", Keywords = keywords});
                if(shader.ShaderPropertiesTemplate != null) _column.AddTemplate("ShaderPropertiesTemplate", shader.ShaderTemplate, keywords);
            }
            
            var moduleByTemplate = new Dictionary<ModuleTemplate, ShaderModule>();
            foreach (var module in shader.BaseModules.Concat(shader.AdditionalModules))
            foreach (var template in module.Templates)
                moduleByTemplate.Add(template, module);
            
            foreach (var template in  shader.BaseModules.Concat(shader.AdditionalModules).SelectMany(x => x.Templates).OrderBy(x => x.Queue))
            {
                var module = moduleByTemplate[template];
                _pairs.AddRange(_column.AddTemplate(module.Id, template));
            }
            _column.ReorderTemplates(_pairs);
            _column.AddItemsToElementsHierarchy(_pairs);
        }
    }

    public class TextPopup : EditorWindow
    {
        public string Text;
        private void CreateGUI()
        {
            ScrollView s = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            var textLabel = new Label(Text);
            s.AddToClassList("unity-base-text-field__input");
            s.AddToClassList("unity-text-field__input");
            s.AddToClassList("unity-base-field__input");
            
            s.Add(textLabel);
            rootVisualElement.Add(s);
        }
    }
    
    public class TemplateItemElement : VisualElement
    {
        private ModuleTemplate _template;
        public TemplateAsset _asset;
        public string ModuleId;
        private string _key;

        private static TextPopup _popup;
        private  TextPopup _personalPopup;
        private VisualElement _header;

        public TemplateItemElement(string moduleId, ModuleTemplate template, string key) : this (moduleId, template.Template, key)
        {
            _template = template;

            if (_template == null) return;
            var priority = new Label("" +_template.Queue);
            priority.AddToClassList("node-header-queue");
            _header.Add(priority);
        }
        
        public TemplateItemElement(string moduleId, TemplateAsset template, string key)
        {
            _asset = template;
            _key = key;
            ModuleId = moduleId;
            _header = new VisualElement();
            _header.AddToClassList("node-header");
            var mainLabel = new Label(_asset.name);
            mainLabel.AddToClassList("node-header-name");
            var idLabel = new Label($"({ModuleId})");
            idLabel.AddToClassList("node-header-id");

            _header.Add(mainLabel);
            _header.Add(idLabel);
            
            var body = new VisualElement();
            body.AddToClassList("node-body");
            if(!string.IsNullOrEmpty(_key) &&_asset.Keywords.Length>0)
                body.AddToClassList("divider");
            if (!string.IsNullOrEmpty(_key))
            {
                var usedKeyword = new Label(_key);
                body.Add(usedKeyword);
            }

            var containedKeywords = new VisualElement();
            body.Add(containedKeywords);
            foreach (string keyword in _asset.Keywords)
                containedKeywords.Add(new Label(keyword.Replace("#K#","").Replace("#KI#", "")));

            Add(_header);
            Add(body);
            
            RegisterCallback<MouseUpEvent>(evt =>
            {
                if (evt.button != 0) return;
                if (_popup == null || _personalPopup == null || _personalPopup != _popup)
                {
                    if(_popup != null) _popup.Close();
                    _personalPopup = _popup = ScriptableObject.CreateInstance<TextPopup>();
                    _personalPopup.Text = _asset.Template;
                    var position = GUIUtility.GUIToScreenRect(this.worldBound);
                    _personalPopup.ShowAsDropDown(position, new Vector2(600, 800));
                }
            });

        }

        public bool ContainsKeyword(string keyword)
        {
            return _asset.Keywords.Contains(keyword);
        }
    }
    
    public class TemplatePair
    {
        public TemplateItemElement Left;
        public TemplateItemElement Right;
        
        public TemplatePair(TemplateItemElement left, TemplateItemElement right)
        {
            Left = left;
            Right = right;
        }
    }

    public class BezierElement : VisualElement
    {
        private TemplateItemElement _left;
        private TemplateItemElement _right;

        public BezierElement(TemplatePair pairs)
        {
            _left = pairs.Left;
            _right = pairs.Right;

            this.style.position = Position.Absolute;

            this.generateVisualContent = OnGenerateVisualContent;
        }
        
        
        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var leftX = _left.layout.position.x + _left.layout.width; 
            var leftY = _left.layout.position.y + (_left.layout.height / 2); 
            
            var rightX = _right.worldBound.x - _left.parent.worldBound.x; 
            var rightY = _right.layout.position.y + (_right.layout.height / 2); 
            
            DrawBezier(new Vector2(leftX,leftY), new Vector2(rightX,rightY), 2, Color.grey, mgc);
        }

        private static void DrawBezier(Vector2 left, Vector2 right, float thickness, Color color, MeshGenerationContext context)
        {
            List<Vertex> vertices = new List<Vertex>();
            List<ushort> indices = new List<ushort>();
 
            Vector2 currentPoint = GetPoint(0, left, left + Vector2.right * 20, right + Vector2.left * 20, right);
            for (int i = 0; i < 10; i++)
            {
                Vector2 nextPoint = GetPoint((i+1)/10f, left, left + Vector2.right * 20, right + Vector2.left * 20, right);

                float angle = Mathf.Atan2(nextPoint.y - currentPoint.y, nextPoint.x - currentPoint.x);
                float offsetX = thickness / 2 * Mathf.Sin(angle);
                float offsetY = thickness / 2 * Mathf.Cos(angle);
 
                vertices.Add(new Vertex()
                {
                    position = new Vector3(currentPoint.x + offsetX, currentPoint.y - offsetY, Vertex.nearZ),
                    tint = color
                });
                vertices.Add(new Vertex()
                {
                    position = new Vector3(nextPoint.x + offsetX, nextPoint.y - offsetY, Vertex.nearZ),
                    tint = color
                });
                vertices.Add(new Vertex()
                {
                    position = new Vector3(nextPoint.x - offsetX, nextPoint.y + offsetY, Vertex.nearZ),
                    tint = color
                });
                vertices.Add(new Vertex()
                {
                    position = new Vector3(nextPoint.x - offsetX, nextPoint.y + offsetY, Vertex.nearZ),
                    tint = color
                });
                vertices.Add(new Vertex()
                {
                    position = new Vector3(currentPoint.x - offsetX, currentPoint.y + offsetY, Vertex.nearZ),
                    tint = color
                });
                vertices.Add(new Vertex()
                {
                    position = new Vector3(currentPoint.x + offsetX, currentPoint.y - offsetY, Vertex.nearZ),
                    tint = color
                });
 
                ushort indexOffset(int value) => (ushort)(value + (i * 6));
                indices.Add(indexOffset(0));
                indices.Add(indexOffset(1));
                indices.Add(indexOffset(2));
                indices.Add(indexOffset(3));
                indices.Add(indexOffset(4));
                indices.Add(indexOffset(5));

                currentPoint = nextPoint;
            }
 
            var mesh = context.Allocate(vertices.Count, indices.Count);
            mesh.SetAllVertices(vertices.ToArray());
            mesh.SetAllIndices(indices.ToArray());
        }
        
        private static Vector2 GetPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float cx = 3 * (p1.x - p0.x);
            float cy = 3 * (p1.y - p0.y);
            float bx = 3 * (p2.x - p1.x) - cx;
            float by = 3 * (p2.y - p1.y) - cy;
            float ax = p3.x - p0.x - cx - bx;
            float ay = p3.y - p0.y - cy - by;
            float Cube = t * t * t;
            float Square = t * t;

            float resX = (ax * Cube) + (bx * Square) + (cx * t) + p0.x;
            float resY = (ay * Cube) + (by * Square) + (cy * t) + p0.y;

            return new Vector2(resX, resY);
        }
    }
    
    public class TemplateRow : VisualElement
    {
        private TemplateRow _child;
        private VisualElement _container;
        private VisualElement _bezierContainer;
        public List<TemplateItemElement> _items;
        public List<TemplateItemElement[]> _itemsChildren;
        
        public TemplateRow()
        {
            _items = new List<TemplateItemElement>();
            _itemsChildren = new List<TemplateItemElement[]>();
            style.flexDirection = FlexDirection.Row;

            _bezierContainer = new VisualElement();
            Add(_bezierContainer);
            _container = new VisualElement();
            _container.style.flexShrink = 0;
            Add(_container);
        }

        public List<TemplatePair> AddTemplate(string moduleId, ModuleTemplate template)
        {
            var pairs = new List<TemplatePair>();

            foreach ((TemplateItemElement item, string key) in _items.Select(item => (item, template.Keywords.FirstOrDefault(y => IsKeywordValid(moduleId, item, y)))).Where(x => !string.IsNullOrEmpty(x.Item2)))
            {
                if (_child == null)
                {
                    _child = new TemplateRow();
                    Add(_child);
                }
                var i = new TemplateItemElement(moduleId, template, key);
                _child.AddItem(i);
                
                pairs.Add(new TemplatePair(item, i));
            }

            pairs.AddRange(_child?.AddTemplate(moduleId, template) ?? new List<TemplatePair>());

            return pairs;
        }
        
        public List<TemplatePair> AddTemplate(string moduleId, TemplateAsset template, string[] keywords)
        {
            var pairs = new List<TemplatePair>();
            
            foreach ((TemplateItemElement item, string key) in _items.Select(item => (item, keywords.FirstOrDefault(y => IsKeywordValid(moduleId, item, y)))).Where(x => !string.IsNullOrEmpty(x.Item2)))
            {
                if (_child == null)
                {
                    _child = new TemplateRow();
                    Add(_child);
                }
                var i = new TemplateItemElement(moduleId, template, key);
                _child.AddItem(i);

                pairs.Add(new TemplatePair(item, i));
            }

            pairs.AddRange(_child?.AddTemplate(moduleId, template, keywords) ?? new List<TemplatePair>());

            return pairs;
        }
        
        public void AddBaseTemplate(string moduleId, TemplateAsset template)
        {
            AddItem(new TemplateItemElement(moduleId, template, ""));
        }

        public void ReorderTemplates(List<TemplatePair> pairs)
        {
            if (_child == null) return;
            
            var reorderedItems = new List<TemplateItemElement>();
            foreach (var item in _items)
            {
                var children = pairs.Where(x => x.Left == item).Select(x => x.Right).ToArray();
                _itemsChildren.Add(children);
                reorderedItems.AddRange(children);
            }

            _child._items = reorderedItems;
            
            _child.ReorderTemplates(pairs);
        }

        public void AddItemsToElementsHierarchy(List<TemplatePair> pairs)
        {
            foreach (var item in _items)
            {
                int i = 0;
                float counter = 0;
                foreach (TemplatePair pair in pairs.Where(x => x.Left == item))
                {
                    _bezierContainer.Add(new BezierElement(pair));
                    counter += _child?.GetTotalStackHeight(i) ?? 1;
                    i++;
                }

                if (_child == null)
                {
                    _container.Add(item);
                    continue;
                }

                float height = (counter - ((item._asset.Keywords.Length - 1) * 15 + 89)) / 2;
                var space = new VisualElement();
                space.style.height = height;
                _container.Add(space);
                _container.Add(item);
                space = new VisualElement();
                space.style.height = height;
                _container.Add(space);
            }
            
            _child?.AddItemsToElementsHierarchy(pairs);
        }

        public float GetTotalStackHeight(int c)
        {
            if (_child == null) return (_items[c]._asset.Keywords.Length - 1) * 15 + 89;

            float counter = 0;
            for (int i = 0; i < _itemsChildren[c].Length; i++)
            {
                counter += _child.GetTotalStackHeight(i);
            }

            return counter;
        }
        
        private static bool IsKeywordValid(string moduleId, TemplateItemElement item, string y)
        {
            if (item.ContainsKeyword("#K#"+y)) return true;
            return item.ContainsKeyword("#KI#"+y) && moduleId.Equals(item.ModuleId);
        }

        private void AddItem(TemplateItemElement item)
        {
            _items.Add(item);
        }

        public void Reset()
        {
            Clear();
            _items.Clear();
            _itemsChildren.Clear();;
            _child = null;
            
            _bezierContainer = new VisualElement();
            Add(_bezierContainer);
            _container = new VisualElement();
            _container.style.flexShrink = 0;
            Add(_container);
        }
    }
}