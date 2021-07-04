using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor {
    
    /// <summary>
    /// Drawer for the list wrapper class, which allows lists to be displayed as reorderable lists.
    /// </summary>
    [CustomPropertyDrawer(typeof(ListWrapper), true)]
    public class ListWrapperDrawer : PropertyDrawer {

        [SerializeField]    private SerializedProperty  _property;
        [SerializeField]    private ReorderableList     _list;
        [SerializeField]    private int                 _selection      = -1;


        public SerializedProperty   property        { get { return _property; }     private set { _property     = value; } }
        public ReorderableList      list            { get { return _list; }         private set { _list         = value; } }
        public bool                 initialized     { get { return property != null && list != null && list.count == property.FindPropertyRelative("_list").arraySize; } }
        public int                  selection       { get { return _selection; }    protected set { _selection    = value; } }

        public virtual bool displayFoldout      { get { return true; } }
        public virtual bool draggable           { get { return true; } }
        public virtual bool displayHeader       { get { return true; } }
        public virtual bool displayAddButton    { get { return true; } }
        public virtual bool displayRemoveButton { get { return true; } }


        //Height of each property.
        public override float GetPropertyHeight(SerializedProperty spProperty, GUIContent oLabel) {
            if (!initialized) { Initialize(spProperty); }
            
            if (initialized) {
                if (displayFoldout) {
                    if (spProperty.isExpanded) { return EditorGUIUtility.singleLineHeight + list.GetHeight(); }
                    else { return EditorGUIUtility.singleLineHeight; }
                }
                else { return list.GetHeight(); }
            }
            return EditorGUIUtility.singleLineHeight;
        }

        //Display list and foldout.
        public override void OnGUI(Rect rPosition, SerializedProperty spProperty, GUIContent oLabel) {
            if (!initialized) { Initialize(spProperty); }

            if (initialized) {
                if (displayFoldout) {
                    Rect rFoldout = new Rect(rPosition.x, rPosition.y, rPosition.width, EditorGUIUtility.singleLineHeight);
                    if (spProperty.isExpanded = EditorGUI.Foldout(rFoldout, spProperty.isExpanded, new GUIContent(spProperty.displayName))) {
                        
                        Rect    rList   = new Rect(rPosition.x, rPosition.y + EditorGUIUtility.singleLineHeight, rPosition.width, list.GetHeight());
                                rList   = EditorGUI.IndentedRect(rList);

                        list.DoList(rList);
                    }
                }
                else {
                    list.DoList(rPosition);
                }
            }
        }


        //Initialization.
        protected void Initialize(SerializedProperty spProperty) {
            SerializedProperty spList = spProperty.FindPropertyRelative("_list");

            if (spList != null && spList.isArray) {
                property        = spProperty;

                List<SerializedProperty> oElements = new List<SerializedProperty>();
                if (spList.isArray) {
                    for (int i = 0; i < spList.arraySize; ++i) {
                        oElements.Add(spList.GetArrayElementAtIndex(i));
                    }
                }

                if (oElements != null) {
                    list = new ReorderableList(oElements, typeof(SerializedProperty), draggable, displayHeader, displayAddButton, displayRemoveButton);

                    list.drawHeaderCallback     = DisplayHeaderCallback;
                    list.drawElementCallback    = DrawElementCallback;
                    list.onAddCallback          = AddCallback;
                    list.onRemoveCallback       = RemoveCallback;
                    list.onCanAddCallback       = CanAddCallback;
                    list.onCanRemoveCallback    = CanRemoveCallback;
                    list.onReorderCallback      = ReorderCallback;
                    list.elementHeightCallback  = ElementHeightCallback;
                    list.onSelectCallback       = SelectCallback;
                }

                OnInitialize(spProperty);
            }
        }


        //Initialization function.
        protected virtual void OnInitialize(SerializedProperty spProperty) { }



        #region REORDERABLE_LIST_CALLBACKS
        protected virtual void DisplayHeaderCallback (Rect rRect) {
            if (property == null) { return; }
            int iIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.LabelField(rRect, property.displayName);
            EditorGUI.indentLevel = iIndent;
        }

        protected virtual void DrawElementCallback (Rect rRect, int iIndex, bool bActive, bool bFocused) {
            SerializedProperty spList = property.FindPropertyRelative("_list");
            int iIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;
            EditorGUI.PropertyField(new Rect (rRect.x, rRect.y + 1, rRect.width, rRect.height - 1.0f), spList.GetArrayElementAtIndex(iIndex), true);
            EditorGUI.indentLevel = iIndent;
        }

        protected virtual void AddCallback(ReorderableList oList) {
            SerializedProperty spList = property.FindPropertyRelative("_list");
            spList.arraySize++;
            spList.serializedObject.ApplyModifiedProperties();
            Initialize(property);
        }

        protected virtual void RemoveCallback (ReorderableList oList) {
            SerializedProperty spList = property.FindPropertyRelative("_list");
            spList.DeleteArrayElementAtIndex (oList.index);
            spList.serializedObject.ApplyModifiedProperties();
            Initialize(property);
        }

        protected virtual bool CanAddCallback (ReorderableList oList) {
            SerializedProperty spList = property.FindPropertyRelative("_list");
            return true;
        }

        protected virtual bool CanRemoveCallback (ReorderableList oList) {
            SerializedProperty spList = property.FindPropertyRelative("_list");
            return true;
        }

        protected virtual void ReorderCallback(ReorderableList oList) {
            SerializedProperty spList = property.FindPropertyRelative("_list");
            spList.MoveArrayElement(selection, oList.index);
        }

        protected virtual float ElementHeightCallback (int iIndex) {
            SerializedProperty spList = property.FindPropertyRelative("_list");
            return EditorGUI.GetPropertyHeight(spList.GetArrayElementAtIndex(iIndex), true) + 2.0f;
        }

        protected virtual void SelectCallback (ReorderableList oList) {
            selection = oList.index;
        }
        #endregion
    }

}