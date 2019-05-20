using UnityEngine;
using UnityEditor;

/// <summary>
/// Adds a button to the inspector for the map preview script that it to be manually updated.
/// </summary>
[CustomEditor(typeof(MapPreview))]
public class MapPreviewEditor : Editor
{
    /// <summary>
    /// Adds the button to the inspector GUI.
    /// </summary>
    public override void OnInspectorGUI()
    {
        // the object being inspected
        MapPreview mapPreview = (MapPreview)target;

        // draws the built in inspector
        if (DrawDefaultInspector())
        {
            // checks if the scene is supposed to auto update when values are changed
            if (mapPreview.autoUpdate)
            {
                // redraws the map
                mapPreview.DrawMapInEditor();
            }
        }

        // checks if the generate button was pressed
        if (GUILayout.Button("Generate"))
        {
            // redraws the map
            mapPreview.DrawMapInEditor();
        }
    }
}