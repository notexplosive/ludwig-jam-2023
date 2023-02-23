#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
    // Color passed to the SpriteBatch
	float4 Color : COLOR0;
	
	// Where on the source texture we're drawing
	float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 color = tex2D(SpriteTextureSampler, input.TextureCoordinates);
	
	// by default, render nothing
	float4 result = float4(0,0,0,0);

	// ideally this means we don't draw shadows on partly transparent things but this doesn't work
	if(color.a == 1)
	{
		// result is black
	    result = float4(0,0,0,1); 
	}
	return result;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};