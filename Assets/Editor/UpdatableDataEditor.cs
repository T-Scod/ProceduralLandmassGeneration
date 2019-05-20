using UnityEngine;
using UnityEditor;

/// <summary>
/// Adds a button to the inspector for updatable data scripts that allows them to be manually updated.
/// </summary>
[CustomEditor(typeof(UpdatableData), true)]
public class UpdatableDataEditor : Editor
{
    /// <summary>
    /// Adds the button to the inspector GUI.
    /// </summary>
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        // the object being inspected
        UpdatableData data = (UpdatableData)target;

        // checks if the button was pressed
        if (GUILayout.Button("Update"))
        {
            // updates the settings
            data.NotifyOfUpdatedValues();
            EditorUtility.SetDirty(target);
        }
    }
}