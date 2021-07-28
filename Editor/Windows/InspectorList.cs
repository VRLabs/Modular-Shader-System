using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace VRLabs.ModularShaderSystem
{

    // Shamelessly taken from here: https://forum.unity.com/threads/custom-bindableelement.989693/    
    public class InspectorList : BindableElement
    {
        Foldout _listContainer;
        Button _addButton;
        SerializedProperty _array;
        private bool _showElementsButtons;

        public InspectorList()
        {
            _listContainer = new Foldout();
            _listContainer.text = "Unbound List";
            _listContainer.contentContainer.AddToClassList("inspector-list-container");
            _listContainer.value = false;
            _addButton = new Button(AddItem);
            _addButton.text = "Add";
            _addButton.AddToClassList("inspector-list-add-button");
            Add(_listContainer);
            _listContainer.Add(_addButton);
            _listContainer.RegisterValueChangedCallback((e) => _array.isExpanded = e.newValue);
            var styleSheet = Resources.Load<StyleSheet>(MSSConstants.RESOURCES_FOLDER + "/MSSUIElements/InspectorList");
            styleSheets.Add(styleSheet);
        }
     
        // Get the reference to the bound serialized object.
        public override void HandleEvent(EventBase evt)
        {
            var type = evt.GetType(); //SerializedObjectBindEvent is internal, so need to use reflection here
            if ((type.Name == "SerializedPropertyBindEvent") && !string.IsNullOrWhiteSpace(bindingPath))
            {
                var obj = type.GetProperty("bindProperty")?.GetValue(evt) as SerializedProperty;
                _array = obj;
                if (obj != null) _listContainer.value = obj.isExpanded;
                // Updating it twice here doesn't cause an issue.
                UpdateList();
            }
            base.HandleEvent(evt);
        }
     
        // Refresh/recreate the list.
        public void UpdateList()
        {
            _listContainer.Clear();
           
            if (_array == null)
                return;
            _listContainer.text = _array.displayName;
            for (int i = 0; i < _array.arraySize; i++)
            {
                int index = i;
                var item = new InspectorListItem(new PropertyField(_array.GetArrayElementAtIndex(index)), _array, index, _showElementsButtons);
                item.removeButton.RegisterCallback<PointerUpEvent>((evt) => {
                    RemoveItem(index);
                });
                item.upButton.RegisterCallback<PointerUpEvent>((evt) =>
                {
                    MoveUpItem(index);
                });
                item.downButton.RegisterCallback<PointerUpEvent>((evt) => {
                    MoveDownItem(index);
                });
                _listContainer.Add(item);
            }
            _listContainer.Add(_addButton);
        }
     
        // Remove an item and refresh the list
        public void RemoveItem(int index)
        {
            if(_array != null)
            {
                if(index < _array.arraySize - 1)
                    _array.GetArrayElementAtIndex(index).isExpanded = _array.GetArrayElementAtIndex(index + 1).isExpanded;
                _array.DeleteArrayElementAtIndex(index);
                _array.serializedObject.ApplyModifiedProperties();
            }
     
            UpdateList();
        }
        
        public void MoveUpItem(int index)
        {
            if(_array != null && index > 0)
            {
                _array.MoveArrayElement(index, index - 1);
                bool expanded = _array.GetArrayElementAtIndex(index).isExpanded;
                _array.GetArrayElementAtIndex(index).isExpanded = _array.GetArrayElementAtIndex(index - 1).isExpanded;
                _array.GetArrayElementAtIndex(index - 1).isExpanded = expanded;
                _array.serializedObject.ApplyModifiedProperties();
            }
     
            UpdateList();
        }
        
        public void MoveDownItem(int index)
        {
            if(_array != null && index<_array.arraySize - 1)
            {
                _array.MoveArrayElement(index, index + 1);
                bool expanded = _array.GetArrayElementAtIndex(index).isExpanded;
                _array.GetArrayElementAtIndex(index).isExpanded = _array.GetArrayElementAtIndex(index + 1).isExpanded;
                _array.GetArrayElementAtIndex(index + 1).isExpanded = expanded;
                _array.serializedObject.ApplyModifiedProperties();
            }
     
            UpdateList();
        }
     
        // Add an item and refresh the list
        public void AddItem()
        {
            if (_array != null)
            {
                _array.InsertArrayElementAtIndex(_array.arraySize);
                _array.serializedObject.ApplyModifiedProperties();
            }
     
            UpdateList();
        }
     
        public new class UxmlFactory : UxmlFactory<InspectorList, UxmlTraits> { }
     
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlBoolAttributeDescription showElements =
                new UxmlBoolAttributeDescription { name = "show-elements-text", defaultValue = true };
            
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                if (ve is InspectorList ate) ate._showElementsButtons = showElements.GetValueFromBag(bag, cc);
            }
        }
     
    }
     
    public class InspectorListItem : VisualElement {
        public Button removeButton;
        public Button upButton;
        public Button downButton;
        public InspectorListItem(VisualElement element, SerializedProperty array, int index, bool showButtonsText)
        {
            AddToClassList("inspector-list-item-container");
            
            VisualElement buttonsArea = new VisualElement();

            this.RegisterCallback<UnityEngine.UIElements.GeometryChangedEvent>(e =>
            {
                buttonsArea.ClearClassList();
                if (e.newRect.height > 60)
                {
                    buttonsArea.AddToClassList("inspector-list-buttons-container-vertical");
                    buttonsArea.Add(removeButton);
                    buttonsArea.Add(upButton);
                    buttonsArea.Add(downButton);
                }
                else
                {
                    buttonsArea.AddToClassList("inspector-list-buttons-container-horizontal");
                    buttonsArea.Add(upButton);
                    buttonsArea.Add(downButton);
                    buttonsArea.Add(removeButton);
                }
            });
            
            upButton = new Button();
            upButton.name = "UpInspectorListItem";
            upButton.AddToClassList("inspector-list-up-button");
            if (index == 0)
                upButton.SetEnabled(false);
            downButton = new Button();
            downButton.name = "DownInspectorListItem";
            downButton.AddToClassList("inspector-list-down-button");
            if (index >= array.arraySize - 1)
                downButton.SetEnabled(false);
            removeButton = new Button();
            removeButton.name = "RemoveInspectorListItem";
            removeButton.AddToClassList("inspector-list-remove-button");

            if (showButtonsText)
            {
                upButton.text = "up";
                downButton.text = "down";
                removeButton.text = "-";
            }
            
            var property = array.GetArrayElementAtIndex(index);
            element.AddToClassList("inspector-list-item");
            element.Bind(property.serializedObject);
            Add(element);
            Add(buttonsArea);
           
            
        }
    }
}