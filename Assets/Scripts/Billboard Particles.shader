Shader "Custom/Billboard Particles"
{
	Properties
	{
		_MainTex("Particle Sprite", 2D) = "white" {}
		_SizeMul("Size Multiplier", Float) = 1
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _AOStrength("AO Strength", Range(0,1)) = 1
        _AORadius("AO Radius", Range(0,1)) = 0.5


	}

		SubShader
		{
			Pass
			{
				Cull Back
				Lighting On
				Zwrite On

			//Blend SrcAlpha OneMinusSrcAlpha
			Blend One OneMinusSrcAlpha
			//Blend One One
			//Blend OneMinusDstColor One

			LOD 200

			Tags
			{
				"RenderType" = "Transparent"
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"LightMode" = "ForwardBase"
			}

			CGPROGRAM

			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase

			#include "UnityCG.cginc"
			#include "Lighting.cginc"
            #include "AutoLight.cginc"
			

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

				int _static;
              
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
                float3 worldPos : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float3 normal : NORMAL;
                SHADOW_COORDS(3)
            };

            v2f vert(uint id : SV_VertexID, uint inst : SV_InstanceID)
            {
                v2f o;

                float3 q = quad[id];

                float3 offset = particles[inst].position - worldPosTransform;
                float4x4 translation = 
                {
                    1,0,0,0,
                    0,1,0,0,
                    0,0,1,0,
                    worldPosTransform.x,worldPosTransform.y,worldPosTransform.z,1
                };

                o.pos = mul(UNITY_MATRIX_P, mul(UNITY_MATRIX_V, mul(l2w, float4(particles[inst].position.x/dimensions.x, particles[inst].position.y/dimensions.y, particles[inst].position.z/dimensions.z, 1.0f))) + float4(q, 0.0f) * _SizeMul * particles[inst].radius);
                
                o.uv = q + 0.5;
                o.worldPos = mul(l2w, float4(particles[inst].position.x/dimensions.x, particles[inst].position.y/dimensions.y, particles[inst].position.z/dimensions.z, 1.0f)).xyz;
                o.viewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));
                o.normal = normalize(q);

                float density = particles[inst].density;
                float d_value = density / maxDensity;
                float v_value = length(particles[inst].velocity) / maxVelocity;
                float p_value = particles[inst].pressure / maxPressure;

                switch(visMode)
                {
                    case 0: o.col = float4(d_value, 0, 1-d_value, 1); break;
                    case 1: o.col = float4(0, v_value, 1-v_value, 1); break;
                    case 2: o.col = float4(1-p_value, p_value, 0, 1); break;
                    case 3: o.col = float4(particles[inst].color, 1); break;
                }

                TRANSFER_SHADOW(o);
                return o;
            }

            float _Glossiness;
            float _Metallic;

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centeredUV = i.uv * 2 - 1;
                float distSquared = dot(centeredUV, centeredUV);
                if (distSquared > 1) discard;

                float3 normal = float3(centeredUV.x, centeredUV.y, sqrt(1 - distSquared));
                normal = normalize(mul(normal, (float3x3)UNITY_MATRIX_V));

                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 viewDir = normalize(i.viewDir);
                float3 halfVector = normalize(lightDir + viewDir);

                float NdotL = max(dot(normal, lightDir), 0);
                float NdotH = max(dot(normal, halfVector), 0);

                float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * i.col.rgb;
                float3 diffuse = _LightColor0.rgb * i.col.rgb * NdotL;
                float3 specular = _LightColor0.rgb * pow(NdotH, _Glossiness * 100) * _Metallic;

                UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos);

                float3 finalColor = ambient + (diffuse + specular) * attenuation;

                return fixed4(finalColor, 1);
            }

            ENDCG
        }

        // Shadow caster pass
        Pass
        {
            Tags {"LightMode"="ShadowCaster"}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f { 
                V2F_SHADOW_CASTER;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.uv = v.texcoord;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 centeredUV = i.uv * 2 - 1;
                float distSquared = dot(centeredUV, centeredUV);
                if (distSquared > 1) discard;
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }


		 Pass
        {
            Name "AO"
            Tags {"LightMode"="Always"}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float3 viewSpacePos : TEXCOORD1;
            };

            sampler2D _CameraDepthTexture;
            float4 _CameraDepthTexture_TexelSize;
            float _AOStrength;
            float _AORadius;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.viewSpacePos = UnityObjectToViewPos(v.vertex);
                return o;
            }

            float3 getViewPos(float2 uv)
            {
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
                float3 viewSpacePos = float3(uv * 2 - 1, depth);
                viewSpacePos = mul(unity_CameraInvProjection, float4(viewSpacePos, 1)).xyz;
                return viewSpacePos;
            }

            half4 frag (v2f i) : SV_Target
            {
                float3 viewSpacePos = i.viewSpacePos;
                float3 viewSpaceNormal = normalize(cross(ddy(viewSpacePos), ddx(viewSpacePos)));

                float ao = 0;
                float2 rand = frac(i.texcoord * 1023.5);
                float3 sample = float3(0,0,0);

                [unroll]
                for(int j = 0; j < 16; j++)
                {
                    float2 offset = frac(rand + j * 0.0625) * 2 - 1;
                    float3 samplePos = viewSpacePos + (viewSpaceNormal + float3(offset, 0)) * _AORadius;
                    float4 sampleUV = mul(unity_CameraProjection, float4(samplePos, 1));
                    sampleUV.xy = sampleUV.xy * 0.5 + 0.5;

                    float3 sampleViewPos = getViewPos(sampleUV.xy);
                    float3 sampleDir = normalize(sampleViewPos - viewSpacePos);

                    float d = distance(sampleViewPos, viewSpacePos);
                    float occlusion = max(0, dot(viewSpaceNormal, sampleDir)) * (1 - smoothstep(0, _AORadius, d));
                    ao += occlusion;
                }

                ao = 1 - (ao / 16) * _AOStrength;
                return half4(ao, ao, ao, 1);
            }
            ENDCG
        }
    
    }

	Fallback "Diffuse"

}