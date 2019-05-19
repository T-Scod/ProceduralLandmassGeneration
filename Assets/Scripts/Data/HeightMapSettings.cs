using System.Collections;
using UnityEngine;

/// <summary>
/// Contains different options for height variation.
/// </summary>
[CreateAssetMenu()]
public class HeightMapSettings : UpdatableData
{
    /// <summary>
    /// Contains the noise settings.
    /// </summary>
    public NoiseSettings noiseSettings;
    /// <summary>
    /// Determines if a falloff map is being used.
    /// </summary>
    public bool useFalloff;

    /// <summary>
    /// Scales the generated heights.
    /// </summary>
    public float heightMultiplier;
    /// <summary>
    /// Scales the generated heights based on their value.
    /// </summary>
    public AnimationCurve heightCurve;

    /// <summary>
    /// Lowest possible height.
    /// </summary>
    public float MinHeight
    {
        get
        {
            return heightMultiplier * heightCurve.Evaluate(0);
        }
    }
    /// <summary>
    /// Highest possible height.
    /// </summary>
    public float MaxHeight
    {
        get
        {
            return heightMultiplier * heightCurve.Evaluate(1);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Ensures that settings are clamped to certain values.
    /// </summary>
    protected override void OnValidate()
    {
        noiseSettings.ValidateValues();
        if (heightMultiplier <= 0.0f)
        {
            heightMultiplier = 0.01f;
        }
        base.OnValidate();
    }
#endif
}