#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// ListToggle<TItem> drawer:
    /// - Foldout triangle on the left
    /// - Label
    /// - Enabled checkbox placed right next to the label area (no overlap)
    /// - Draws the underlying list as "List" when Enabled && expanded
    /// </summary>
    [CustomPropertyDrawer(typeof(Watermelon.ListToggle<>), true)]
    public class ListToggleDrawer : PropertyDrawer
    {
        private const float VSpace = 2f;
        private const float ToggleWidth = 18f;
        private const float ToggleGap = 4f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var enabledProp = property.FindPropertyRelative("enabled");
            var valueProp   = property.FindPropertyRelative("value"); // this is the List<T>

            if (enabledProp == null || valueProp == null)
                return EditorGUIUtility.singleLineHeight;

            float h = EditorGUIUtility.singleLineHeight;

            if (enabledProp.boolValue && property.isExpanded)
                h += VSpace + EditorGUI.GetPropertyHeight(valueProp, includeChildren: true);

            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var enabledProp = property.FindPropertyRelative("enabled");
            var valueProp   = property.FindPropertyRelative("value");

            if (enabledProp == null || valueProp == null)
            {
                EditorGUI.PropertyField(position, property, label, includeChildren: true);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            // Header
            var headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            float foldoutW = EditorGUIUtility.singleLineHeight;
            var foldoutRect = new Rect(headerRect.x, headerRect.y, foldoutW, headerRect.height);
            var labelLineRect = new Rect(headerRect.x + foldoutW, headerRect.y, headerRect.width - foldoutW, headerRect.height);

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, toggleOnLabelClick: false);

            int id = GUIUtility.GetControlID(FocusType.Passive);
            Rect afterLabelRect = EditorGUI.PrefixLabel(labelLineRect, id, label);

            // Toggle sits next to label (in remaining space), never overlaps the label
            var toggleRect = new Rect(afterLabelRect.x + ToggleGap, headerRect.y, ToggleWidth, headerRect.height);
            enabledProp.boolValue = EditorGUI.Toggle(toggleRect, enabledProp.boolValue);

            if (!enabledProp.boolValue)
                property.isExpanded = false;

            // Children
            if (enabledProp.boolValue && property.isExpanded)
            {
                EditorGUI.indentLevel++;

                float listH = EditorGUI.GetPropertyHeight(valueProp, includeChildren: true);
                var listRect = new Rect(position.x, headerRect.yMax + VSpace, position.width, listH);

                // Force label "List" for consistency with CompositeToggle
                EditorGUI.PropertyField(listRect, valueProp, new GUIContent("List"), includeChildren: true);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }

    /// <summary>
    /// CompositeToggle<TValue, TItem> drawer:
    /// - Foldout triangle on the left
    /// - Label
    /// - Enabled checkbox placed right next to the label area (no overlap)
    /// - Shows Value + List (+ optional ensure flag) only when Enabled && expanded
    /// </summary>
    [CustomPropertyDrawer(typeof(Watermelon.CompositeToggle<,>), true)]
    public class CompositeToggleDrawer : PropertyDrawer
    {
        private const float VSpace = 2f;
        private const float ToggleWidth = 18f;
        private const float ToggleGap = 4f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var enabledProp = property.FindPropertyRelative("enabled");
            var valueProp   = property.FindPropertyRelative("value");
            var listProp    = property.FindPropertyRelative("list");
            var ensureProp  = property.FindPropertyRelative("ensureNonNullListResult");

            if (enabledProp == null || valueProp == null || listProp == null)
                return EditorGUIUtility.singleLineHeight;

            float h = EditorGUIUtility.singleLineHeight;

            if (enabledProp.boolValue && property.isExpanded)
            {
                h += VSpace + EditorGUI.GetPropertyHeight(valueProp, includeChildren: true);
                h += VSpace + EditorGUI.GetPropertyHeight(listProp, includeChildren: true);

                if (ensureProp != null)
                    h += VSpace + EditorGUIUtility.singleLineHeight;
            }

            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var enabledProp = property.FindPropertyRelative("enabled");
            var valueProp   = property.FindPropertyRelative("value");
            var listProp    = property.FindPropertyRelative("list");
            var ensureProp  = property.FindPropertyRelative("ensureNonNullListResult");

            if (enabledProp == null || valueProp == null || listProp == null)
            {
                EditorGUI.PropertyField(position, property, label, includeChildren: true);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            // Header
            var headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            float foldoutW = EditorGUIUtility.singleLineHeight;
            var foldoutRect = new Rect(headerRect.x, headerRect.y, foldoutW, headerRect.height);
            var labelLineRect = new Rect(headerRect.x + foldoutW, headerRect.y, headerRect.width - foldoutW, headerRect.height);

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, toggleOnLabelClick: false);

            int id = GUIUtility.GetControlID(FocusType.Passive);
            Rect afterLabelRect = EditorGUI.PrefixLabel(labelLineRect, id, label);

            var toggleRect = new Rect(afterLabelRect.x + ToggleGap, headerRect.y, ToggleWidth, headerRect.height);
            enabledProp.boolValue = EditorGUI.Toggle(toggleRect, enabledProp.boolValue);

            if (!enabledProp.boolValue)
                property.isExpanded = false;

            if (enabledProp.boolValue && property.isExpanded)
            {
                EditorGUI.indentLevel++;

                float y = headerRect.yMax;

                // Value
                y += VSpace;
                float valueH = EditorGUI.GetPropertyHeight(valueProp, includeChildren: true);
                var valueRect = new Rect(position.x, y, position.width, valueH);
                EditorGUI.PropertyField(valueRect, valueProp, new GUIContent("Value"), includeChildren: true);
                y = valueRect.yMax;

                // List
                y += VSpace;
                float listH = EditorGUI.GetPropertyHeight(listProp, includeChildren: true);
                var listRect = new Rect(position.x, y, position.width, listH);
                EditorGUI.PropertyField(listRect, listProp, new GUIContent("List"), includeChildren: true);
                y = listRect.yMax;

                // Optional ensure flag
                if (ensureProp != null)
                {
                    y += VSpace;
                    var ensureRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(ensureRect, ensureProp, new GUIContent("Ensure Non-Null List"));
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif
