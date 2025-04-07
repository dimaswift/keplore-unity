Shader "ConsequenceCascade/Particle"
{
    SubShader
    {
        Tags { "Queue" = "Opaque" "RenderType" = "Opaque" }
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
          
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };
            struct FieldCell {  
                float2 position;
                float2 previousPosition;
                float siderealTime;
                float precessionalTime;
                float mass;
                float temperature;
                float balance;
            };
            
            StructuredBuffer<FieldCell> Particles;
            float Time;
            float Size;

            half4 HSVtoRGB(float h, float s, float v) {  
                // Ensure h is in [0,1]  
                h = frac(h);  
                
                half4 rgb;  
                
                float i = floor(h * 6.0);  
                float f = h * 6.0 - i;  
                float p = v * (1.0 - s);  
                float q = v * (1.0 - f * s);  
                float t = v * (1.0 - (1.0 - f) * s);  
                
                int iMod4 = int(i) % 6;  
                if (iMod4 == 0) rgb = half4(v, t, p, 1);  
                else if (iMod4 == 1) rgb = half4(q, v, p, 1);  
                else if (iMod4 == 2) rgb = half4(p, v, t, 1);  
                else if (iMod4 == 3) rgb = half4(p, q, v, 1);  
                else if (iMod4 == 4) rgb = half4(t, p, v, 1);  
                else rgb = half4(v, p, q, 1);  
                
                return rgb;  
            }  

            half4 Rainbow(float t) {  
                            // Map t from [0,1] to hue range [0, 0.833]  
                // 0.833 is approx 300° in 0-1 range (where 360° = 1.0)  
                // This gives us red at 0 and purple at 1  
                float hue = t * 0.833;  
                
                // Full saturation and value for vibrant colors  
                return HSVtoRGB(hue, 1.0, 1.0);  
            }  
            
            v2f vert(appdata_t i, uint instanceID : SV_InstanceID)
            {
                v2f o;
                const FieldCell cell = Particles[instanceID];
                float s = Size * 0.01;
                float4x4 translationMatrix = float4x4(
                    s, 0, 0, cell.position.x,
                    0, s, 0, cell.position.y,
                    0, 0, s, 0,
                    0, 0, 0, 1
                );
                
                float vel = length(cell.previousPosition - cell.position);
                float4 pos = mul(translationMatrix, i.vertex);
                o.vertex = UnityObjectToClipPos(pos);
                o.color = Rainbow(cell.mass);
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}