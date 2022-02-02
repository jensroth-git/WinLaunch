/// <description>A simple color blending shader for WPF.</description>
/// <target>WPF</target>
/// <profile>ps_2_0</profile>

//-----------------------------------------------------------------------------
// Constants
//-----------------------------------------------------------------------------

/// <summary>The darkness offset.</summary>
/// <type>float</type>
/// <defaultValue>0.0</defaultValue>
float Darkness : register(c0);

//-----------------------------------------------------------------------------
// Samplers
//-----------------------------------------------------------------------------

/// <summary>The implicit input sampler passed into the pixel shader by WPF.</summary>
/// <samplingMode>Auto</samplingMode>
sampler2D Input : register(s0);

//-----------------------------------------------------------------------------
// Pixel Shader
//-----------------------------------------------------------------------------

float4 main(float2 uv : TEXCOORD) : COLOR
{
	// TODO: add your pixel shader code here.
	float4 newcolor = tex2D(Input, uv);
		newcolor.r *= (1 - Darkness);
	newcolor.g *= (1 - Darkness);
	newcolor.b *= (1 - Darkness);

	return newcolor;
}