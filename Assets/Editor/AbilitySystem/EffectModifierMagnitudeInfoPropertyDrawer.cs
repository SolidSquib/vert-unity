using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(EffectModifierMagnitudeInfo))]
public class EffectModifier : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty calculationType = property.FindPropertyRelative("magnitudeCalculation");
        EModifierCalculation value = (EModifierCalculation)calculationType.enumValueIndex;
        List<SerializedProperty> relevantProperties = GetRelevantProperties(property, value);

        EditorGUI.BeginProperty(position, label, property);

        Rect indentedPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        indentedPosition.height = EditorGUI.GetPropertyHeight(calculationType);
        EditorGUI.PropertyField(indentedPosition, calculationType, GUIContent.none);

        EditorGUI.indentLevel = 1;
        Rect rect = new Rect(position.x, position.y + EditorGUI.GetPropertyHeight(calculationType), position.width, position.height);

        foreach (var prop in relevantProperties)
        {
            rect.height = EditorGUI.GetPropertyHeight(prop);
            EditorGUI.PropertyField(rect, prop);
            rect.y += rect.height;
        }

        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }

    protected List<SerializedProperty> GetRelevantProperties(SerializedProperty parentProperty, EModifierCalculation calculationMethod)
    {
        List<SerializedProperty> returnValue = new List<SerializedProperty>();
        switch (calculationMethod)
        {
            case EModifierCalculation.ScalableFloat:
                returnValue.Add(parentProperty.FindPropertyRelative("baseMagnitude"));
                break;
            case EModifierCalculation.AttributeBased:
                returnValue.Add(parentProperty.FindPropertyRelative("attribute"));
                break;
            case EModifierCalculation.CustomCalculationClass:
                returnValue.Add(parentProperty.FindPropertyRelative("customCalculationClass"));
                break;
            case EModifierCalculation.SetByCaller:
                returnValue.Add(parentProperty.FindPropertyRelative("setByCallerTag"));
                break;
        }

        return returnValue;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty calculationType = property.FindPropertyRelative("magnitudeCalculation");
        EModifierCalculation value = (EModifierCalculation)calculationType.enumValueIndex;
        List<SerializedProperty> relevantProperties = GetRelevantProperties(property, value);
        relevantProperties.Add(calculationType);

        float totalHeight = 0;
        foreach (var prop in relevantProperties)
        {
            totalHeight += EditorGUI.GetPropertyHeight(prop, label, true) + EditorGUIUtility.standardVerticalSpacing;
        }

        return totalHeight;
    }
}
