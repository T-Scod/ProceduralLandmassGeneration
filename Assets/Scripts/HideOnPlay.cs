using System.Collections;
using UnityEngine;

/// <summary>
/// Hides a game object when the user presses play.
/// </summary>
public class HideOnPlay : MonoBehaviour
{
    /// <summary>
    /// Called when game starts.
    /// </summary>
    private void Start()
    {
        // sets the game object to inactive
        gameObject.SetActive(false);
    }
}