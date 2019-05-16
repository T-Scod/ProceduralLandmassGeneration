using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdatableData
{
    public Noise.NormaliseMode normaliseMode;

    public float noiseScale;

    [Range(0, 25)]
    public int octaves;
    [Range(0, 1)]
    public float persistence;
    [Range(0, 35)]
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    protected override void OnValidate()
    {
        if (noiseScale < 0.03f)
        {
            noiseScale = 0.03f;
        }

        base.OnValidate();
    }
}