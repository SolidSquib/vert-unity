using UnityEditor;

[CustomEditor(typeof(EffectModifierMagnitudeInfo))]
public class EffectModifierMagnitudeInfoEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SerializedProperty property = serializedObject.FindProperty("magnitudeCalculation");
        EModifierCalculation value = (EModifierCalculation)property.enumValueIndex;

        switch (value)
        {
            case EModifierCalculation.ScalableFloat:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("baseMagnitude"));
                break;
            case EModifierCalculation.AttributeBased:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("attribute"));
                break;
            case EModifierCalculation.CustomCalculationClass:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("customCalculationClass"));
                break;
            case EModifierCalculation.SetByCaller:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("setByCallerTag"));
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
