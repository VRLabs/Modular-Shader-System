Shader "Example/ExampleShader"
{
	Properties
	{
		[Enum(UnityEngine.Rendering.CompareFunction)][Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Depth test", Float) = 4
		_ZWrite("Depth write", Float) = 0
		[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2
		_MyColor("My Color", Color) = (1, 1, 1, 1)
		_MyTexture("My Texture", 2D) = "white" {}
	}
	SubShader
	{
		ZTest[_ZTest]
		ZWrite[_ZWrite]
		Cull[_CullMode]
		
		Pass
		{
			Tags
			{
				"LightMode" = "ForwardBase"
			}
			
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex Vertex
			#pragma fragment Fragment
			
			#include "UnityStandardUtils.cginc"
			
			struct VertexData
			{
				float4 vertex     : POSITION;
				float2 uv         : TEXCOORD0;
				float3 normal     : NORMAL;
			};
			
			struct FragmentData
			{
				float4 pos        : SV_POSITION;
				float3 normal     : NORMAL;
				float2 uv         : TEXCOORD0;
				float3 worldPos   : TEXCOORD1;
			};
			
			FragmentData FragData;
			float4 FinalColor;
			
			float4 _MyColor;
			float4 _MyTexture_ST;
			UNITY_DECLARE_TEX2D(_MyTexture);
			
			void ApplyColor()
			{
				FinalColor = _MyColor;
			}
			void ApplyTexture()
			{
				FinalColor *= UNITY_SAMPLE_TEX2D(_MyTexture, TRANSFORM_TEX(FragData.uv, _MyTexture));
			}
			
			FragmentData Vertex (VertexData v)
			{
				FragmentData i;
				UNITY_INITIALIZE_OUTPUT(FragmentData, i);
				
				i.pos        = UnityObjectToClipPos(v.vertex);
				i.normal     = UnityObjectToWorldNormal(v.normal);
				i.worldPos   = mul(unity_ObjectToWorld, v.vertex);
				i.uv         = v.uv;
				
				return i;
			}
			
			float4 Fragment (FragmentData i) : SV_TARGET
			{
				FragData = i;
				FinalColor = float4(0,0,0,0);
				
				ApplyColor();
				ApplyTexture();
				
				return FinalColor;
			}
			
			ENDCG
		}
		
	}
}
