Shader "Custom/Terrain"
{
	// sets the default properties for the texture to a 1x1 white texture
	Properties
	{
		testTexture("Texture", 2D) = "white"{}
		testScale("Scale", Float) = 1
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		// physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// use shader model 3.0 target for nicer looking lighting
		#pragma target 3.0

		// the maximum allowed layers for the texture
		const static int maxLayerCount = 8;
		// really small value
		const static float epsilon = 1E-4;

		// the amount of used layers
		int layerCount;
		// stores the tints
		float3 baseColours[maxLayerCount];
		// stores the start heights
		float baseStartHeights[maxLayerCount];
		// stores the blend strengths
		float baseBlends[maxLayerCount];
		// stores the tint strengths
		float baseColourStrengths[maxLayerCount];
		// stores the texture scales
		float baseTextureScales[maxLayerCount];

		// minimum height of the terrain
		float minHeight;
		// maximum height of the terrain
		float maxHeight;

		// default texture
		sampler2D testTexture;
		// default scale
		float testScale;

		// stores the textures
		UNITY_DECLARE_TEX2DARRAY(baseTextures);

		// gets the world position and normal of the vertex
        struct Input
        {
			float3 worldPos;
			float3 worldNormal;
        };

		// gets the value as a percentage between a and b
		float inverseLerp(float a, float b, float value)
		{
			return saturate((value - a) / (b - a));
		}

		// projects the texture at texture index to the world position with blending based on the axis
		float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex)
		{
			// scales the world position
			float3 scaledWorldPos = worldPos / scale;

			// gets the projection of the texture along the axis
			float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
			float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
			float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
			// returns the sum of the colours
			return xProjection + yProjection + zProjection;
		}

		// processes the colour of each vertex
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
			// gets the vertex's y position as a percentage between the range of heights
			float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
			// the positive normalised vertex normal for blending textures based on the projection axis
			float3 blendAxes = abs(IN.worldNormal);
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

			for (int i = 0; i < layerCount; i++)
			{
				// gets the start height as a percentage between the blend strengths
				float drawStrength = inverseLerp(-baseBlends[i] / 2 - epsilon, baseBlends[i] / 2, heightPercent - baseStartHeights[i]);
				// the tint colour
				float3 baseColour = baseColours[i] * baseColourStrengths[i];
				// gets the colour of the texture at the vertex
				float3 textureColour = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i) * (1 - baseColourStrengths[i]);
				// sets the output colour as the sum of the tint colour, texture colour and strength of the colour
				o.Albedo = o.Albedo * (1 - drawStrength) + (baseColour + textureColour) * drawStrength;
			}
        }
        ENDCG
    }
    FallBack "Diffuse"
}