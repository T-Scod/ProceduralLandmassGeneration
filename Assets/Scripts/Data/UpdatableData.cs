using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A data object that can be updated out of play.
/// </summary>
public class UpdatableData : ScriptableObject
{
    /// <summary>
    /// Updates values.
    /// </summary>
    public event System.Action OnValuesUpdated;
    /// <summary>
    /// Checks if the user wants data to update automatically.
    /// </summary>
    public bool autoUpdate;

#if UNITY_EDITOR
    /// <summary>
    /// Notifies if there are updated values each frame.
    /// </summary>
    protected virtual void OnValidate()
    {
        // checks if the user wants Unity to notify if there are any updated values each frame
        if (autoUpdate)
        {
            // subscribes the notify callback to the Unity editor update callback
            UnityEditor.EditorApplication.update += NotifyOfUpdatedValues;
        }
    }

    /// <summary>
    /// Notifies if there are updated values.
    /// </summary>
    public void NotifyOfUpdatedValues()
    {
        // unsubscribes the callback
        UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
        // checks if there are updated values
        if (OnValuesUpdated != null)
        {
            OnValuesUpdated();
        }
    }
#endif
}