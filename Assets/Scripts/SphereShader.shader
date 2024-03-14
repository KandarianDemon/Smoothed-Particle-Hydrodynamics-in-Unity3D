Shader "Custom/Sphere Particle Shader"
{
	Properties
	{
		_MainTex("Particle Sprite", 2D) = "white" {}
		_SizeMul("Size Multiplier", Float) = 1


	}

		SubShader
		{
			Pass
			{
				Cull Back
				Lighting Off
				Zwrite Off

			//Blend SrcAlpha OneMinusSrcAlpha
			//Blend One OneMinusSrcAlpha
			Blend One One
			//Blend OneMinusDstColor One

			LOD 200

			Tags
			{
				"RenderType" = "Transparent"
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
			}

			CGPROGRAM

			#pragma target 4.5
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			

            #ifndef PARTICLE_STRUCT
            #define PARTICLE_STRUCT

            struct Particle
            {
                
				float3 color;
                float3 position;
                float3 velocity;
				float3 offset;
				float3 predictedPosition;

				
				float pressure;
				float density;
				float radius;
				float mass;

				int hash;
				int index;
              
            };

#endif

			uniform sampler2D _MainTex;
			float _SizeMul;

			StructuredBuffer<Particle> particles;
			StructuredBuffer<float3> quad;

			float max_dist;
			float3 worldPosTransform;
			float4x4 l2w;
			float3 dimensions;

			int cellOfInterest;

			int numberOfCells;
			float v_max;

			int visMode;
			float maxVelocity;
			float maxDensity;
			float maxPressure;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 col : COLOR;
				float3 center: TEXCOORD1;
				float radius: TEXCOORD2;
				float3 normal: NORMAL;
			};

			v2f vert(appdata_full v, uint inst : SV_InstanceID)
			{
				v2f o;

				float3 downScalePosition = float3(particles[inst].position.x/dimensions.x,particles[inst].position.y/dimensions.y, particles[inst].position.z/dimensions.z);
				float3 localPosition = v.vertex.xyz*_SizeMul;

				float3 worldPosition = mul(l2w,float4(downScalePosition + localPosition,1.0f)).xyz;

			

				o.normal = v.normal;
				o.pos = mul(UNITY_MATRIX_VP, float4(worldPosition,1.0f));

				float density = particles[inst].density;
				
				
				float idxClamped = particles[inst].pressure;

				float d_value = density/maxDensity;
				float v_value = length(particles[inst].velocity)/maxVelocity;
				float p_value = particles[inst].pressure/maxPressure;
				if(d_value > 1) d_value = 1;
				// o.col = (density< 500) ? float4(0,0,1,1):float4(0,0.6,1,1)  ;
				// o.col = (density >= 500 & density <= 1100) ? float4(0.2,0.7,0.3,1): o.col;
				// o.col = (density > 1100) ? float4(particles[inst].color,1): o.col;
				//o.col = float4(idxClamped,0,0,1);
				//o.col = float4(d_value,0,1-d_value,1);

				o.col = (visMode == 1) ? float4(0,v_value,1-v_value,1):float4(d_value,0,1-d_value,1);

				switch(visMode)
				{
					case 0: o.col = float4(d_value,0,1-d_value,0);
									break;
					case 1: o.col =  float4(0,v_value,1-v_value,0);
									break;
					case 2: o.col =  float4(1-p_value,p_value,0,0);
									break;
				}
				//o.col = float4(0,v_value,1-v_value,1);
				//o.col = (length(particles[inst].velocity) >= v_max) ? float4(1,1,0,1):float4(1,0,0,1);
				o.radius =particles[inst].radius;
				return o;
			}

			fixed4 frag(v2f i) : COLOR
			{

				float shading = saturate(dot(_WorldSpaceLightPos0.xyz,i.normal));
				shading = (shading + 0.6)/1.4;
				
				

				return float4(float3(1,0,0)*shading,1);
			}

			ENDCG
		}
		}
}