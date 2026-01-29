Shader "Hidden/CustomSSAO"
{
    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
    
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
    
    int _DebugMode;
    float _Intensity;
    float _Radius;
    float _Bias;
    float _FalloffDistance;
    
    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    
    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };
    
    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }
    
    float Hash(float2 p)
    {
        float3 p3 = frac(float3(p.xyx) * 0.1031);
        p3 += dot(p3, p3.yzx + 33.33);
        return frac((p3.x + p3.y) * p3.z);
    }
    
    // Reconstruit la position view space depuis UV et depth
    float3 GetViewPos(float2 uv, float rawDepth)
    {
        float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
        
        // UV vers NDC
        float2 ndc = uv * 2.0 - 1.0;
        #if UNITY_UV_STARTS_AT_TOP
            ndc.y = -ndc.y;
        #endif
        
        // Position en view space
        float3 viewPos;
        viewPos.xy = ndc * linearDepth / float2(UNITY_MATRIX_P[0][0], UNITY_MATRIX_P[1][1]);
        viewPos.z = -linearDepth;
        
        return viewPos;
    }
    
    float4 FragMain(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        
        if (_DebugMode == 4) return float4(1, 1, 1, 1);
        if (_DebugMode == 8) return float4(1, 0, 0, 1);
        
        float2 uv = input.texcoord;
        uint2 positionSS = uint2(uv * _ScreenSize.xy);
        
        float rawDepth = LoadCameraDepth(positionSS);
        
        // Debug modes
        if (_DebugMode == 1)
            return float4(rawDepth, rawDepth, rawDepth, 1.0);
        
        if (_DebugMode == 5)
        {
            if (rawDepth > 0.9) return float4(1, 0, 0, 1);
            if (rawDepth > 0.5) return float4(1, 0.5, 0, 1);
            if (rawDepth > 0.1) return float4(1, 1, 0, 1);
            if (rawDepth > 0.01) return float4(0, 1, 0, 1);
            if (rawDepth > 0.0) return float4(0, 0, 1, 1);
            return float4(1, 0, 1, 1);
        }
        
        if (_DebugMode == 2)
        {
            NormalData normalData;
            DecodeFromNormalBuffer(positionSS, normalData);
            return float4(normalData.normalWS * 0.5 + 0.5, 1.0);
        }
        
        // === HBAO-style SSAO ===
        
        if (rawDepth < 0.0001 || rawDepth > 0.9999)
            return 1.0;
        
        // Position du pixel central en view space
        float3 viewPos = GetViewPos(uv, rawDepth);
        float linearDepth = -viewPos.z;
        
        // Normale en view space
        NormalData normalData;
        DecodeFromNormalBuffer(positionSS, normalData);
        float3 normalVS = mul((float3x3)UNITY_MATRIX_V, normalData.normalWS);
        
        // Rayon en world units, converti en pixels
        float radiusWorld = _Radius;
        float radiusPixels = (radiusWorld * _ScreenSize.y * 0.5) / linearDepth;
        radiusPixels = clamp(radiusPixels, 4.0, 128.0);
        
        // Rotation aléatoire
        float randomAngle = Hash(uv * 1000.0 + _Time.y * 0.01) * 6.28318;
        
        float occlusion = 0.0;
        
        // 8 directions, 2 steps par direction (HBAO-style)
        const int NUM_DIRECTIONS = 8;
        const int STEPS_PER_DIR = 3;
        
        for (int dir = 0; dir < NUM_DIRECTIONS; dir++)
        {
            float angle = (float(dir) / float(NUM_DIRECTIONS)) * 6.28318 + randomAngle;
            float2 direction = float2(cos(angle), sin(angle));
            
            float horizonAngle = -1.0; // Angle d'horizon le plus bas trouvé
            
            for (int step = 1; step <= STEPS_PER_DIR; step++)
            {
                float stepRadius = radiusPixels * (float(step) / float(STEPS_PER_DIR));
                float2 sampleUV = uv + direction * stepRadius * _ScreenSize.zw;
                
                // Bounds check
                if (sampleUV.x < 0.01 || sampleUV.x > 0.99 || sampleUV.y < 0.01 || sampleUV.y > 0.99)
                    continue;
                
                uint2 samplePos = uint2(sampleUV * _ScreenSize.xy);
                float sampleRawDepth = LoadCameraDepth(samplePos);
                
                if (sampleRawDepth < 0.0001 || sampleRawDepth > 0.9999)
                    continue;
                
                // Position du sample en view space
                float3 sampleViewPos = GetViewPos(sampleUV, sampleRawDepth);
                
                // Vecteur du pixel central vers le sample
                float3 deltaVec = sampleViewPos - viewPos;
                float deltaLen = length(deltaVec);
                
                // Skip si trop loin
                if (deltaLen > _FalloffDistance || deltaLen < 0.001)
                    continue;
                
                float3 deltaDir = deltaVec / deltaLen;
                
                // Angle entre la direction du sample et la normale
                // cos(theta) = dot(normal, sampleDir)
                float cosAngle = dot(normalVS, deltaDir);
                
                // L'horizon est la tangente de la surface
                // On cherche des samples qui sont "au-dessus" de notre horizon actuel
                // mais "en-dessous" de la normale (dans l'hémisphère)
                
                // Mise à jour de l'angle d'horizon
                if (cosAngle > horizonAngle)
                {
                    // Ce sample définit un nouvel horizon
                    // L'occlusion est proportionnelle à la différence
                    float angleDiff = cosAngle - horizonAngle;
                    
                    // Falloff par distance
                    float distFalloff = 1.0 - (deltaLen / _FalloffDistance);
                    distFalloff = distFalloff * distFalloff;
                    
                    // Contribution
                    float contrib = angleDiff * distFalloff;
                    
                    // Bias: ignore les petites variations
                    if (cosAngle > _Bias * 0.1)
                    {
                        occlusion += max(0, contrib);
                    }
                    
                    horizonAngle = cosAngle;
                }
            }
        }
        
        // Normalise par le nombre de directions
        occlusion = occlusion / float(NUM_DIRECTIONS);
        
        // Applique l'intensité
        occlusion = saturate(occlusion * _Intensity);
        
        // AO final
        float ao = 1.0 - occlusion;
        
        // Courbe pour contraste
        ao = pow(ao, 1.5);
        
        return ao;
    }
    
    ENDHLSL
    
    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }
        
        Pass
        {
            Name "SSAO"
            ZWrite Off ZTest Always Blend Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragMain
            ENDHLSL
        }
        
        Pass
        {
            Name "SSAO Multiply"
            ZWrite Off ZTest Always Blend DstColor Zero Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragMain
            ENDHLSL
        }
    }
    Fallback Off
}
