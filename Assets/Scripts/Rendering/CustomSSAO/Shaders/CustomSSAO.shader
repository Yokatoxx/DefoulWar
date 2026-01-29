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
    
    float InterleavedGradientNoise(float2 position)
    {
        float3 magic = float3(0.06711056, 0.00583715, 52.9829189);
        return frac(magic.z * frac(dot(position, magic.xy)));
    }
    
    float3 GetViewPos(float2 uv, float rawDepth)
    {
        float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
        float2 ndc = uv * 2.0 - 1.0;
        #if UNITY_UV_STARTS_AT_TOP
            ndc.y = -ndc.y;
        #endif
        float3 viewPos;
        viewPos.xy = ndc * linearDepth / float2(UNITY_MATRIX_P[0][0], UNITY_MATRIX_P[1][1]);
        viewPos.z = -linearDepth;
        return viewPos;
    }
    
    float MultiBounce(float ao)
    {
        // Approximation multi-bounce simple
        float a = 2.0404 * 0.5 - 0.3324;
        float b = -4.7951 * 0.5 + 0.6417;
        float c = 2.7552 * 0.5 + 0.6903;
        return max(ao, ((ao * a + b) * ao + c) * ao);
    }
    
    float4 FragSSAO(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        
        if (_DebugMode == 4) return float4(1, 1, 1, 1);
        
        float2 uv = input.texcoord;
        uint2 positionSS = uint2(uv * _ScreenSize.xy);
        
        float rawDepth = LoadCameraDepth(positionSS);
        
        if (_DebugMode == 5)
        {
            if (rawDepth > 0.5) return float4(1, 0, 0, 1);
            if (rawDepth > 0.1) return float4(1, 1, 0, 1);
            if (rawDepth > 0.01) return float4(0, 1, 0, 1);
            if (rawDepth > 0.0) return float4(0, 0, 1, 1);
            return float4(1, 0, 1, 1);
        }
        
        // Skip sky
        if (rawDepth < 0.0001 || rawDepth > 0.9999)
            return 1.0;
        
        float3 viewPos = GetViewPos(uv, rawDepth);
        float linearDepth = -viewPos.z;
        
        NormalData normalData;
        DecodeFromNormalBuffer(positionSS, normalData);
        float3 normalVS = normalize(mul((float3x3)UNITY_MATRIX_V, normalData.normalWS));
        
        float radiusWorld = _Radius;
        float radiusPixels = (radiusWorld * _ScreenSize.y) / linearDepth;
        radiusPixels = clamp(radiusPixels, 3.0, 200.0);
        
        float noise = InterleavedGradientNoise(positionSS) * 6.28318;
        
        float occlusion = 0.0;
        
        const int NUM_DIRECTIONS = 12;
        const int STEPS_PER_DIR = 4;
        
        for (int dir = 0; dir < NUM_DIRECTIONS; dir++)
        {
            float angle = (float(dir) + 0.5) / float(NUM_DIRECTIONS) * 6.28318 + noise;
            float2 direction = float2(cos(angle), sin(angle));
            
            float maxHorizon = -1.0;
            
            for (int step = 1; step <= STEPS_PER_DIR; step++)
            {
                float t = float(step) / float(STEPS_PER_DIR);
                float stepRadius = radiusPixels * t * t; // Distribution quadratique
                float2 sampleUV = uv + direction * stepRadius * _ScreenSize.zw;
                
                if (any(sampleUV < 0.005) || any(sampleUV > 0.995))
                    continue;
                
                uint2 samplePos = uint2(sampleUV * _ScreenSize.xy);
                float sampleRawDepth = LoadCameraDepth(samplePos);
                
                if (sampleRawDepth < 0.0001 || sampleRawDepth > 0.9999)
                    continue;
                
                float3 sampleViewPos = GetViewPos(sampleUV, sampleRawDepth);
                float3 deltaVec = sampleViewPos - viewPos;
                float deltaLen = length(deltaVec);
                
                if (deltaLen < 0.01 || deltaLen > _FalloffDistance)
                    continue;
                
                float3 deltaDir = deltaVec / deltaLen;
                float horizonCos = dot(normalVS, deltaDir);
                
                // Falloff smooth
                float falloff = 1.0 - smoothstep(0.0, _FalloffDistance, deltaLen);
                
                // Bias
                float biasedHorizon = horizonCos - _Bias * 0.1;
                
                if (biasedHorizon > maxHorizon)
                {
                    float contrib = (biasedHorizon - maxHorizon) * falloff;
                    occlusion += max(0, contrib);
                    maxHorizon = biasedHorizon;
                }
            }
        }
        
        occlusion = occlusion / float(NUM_DIRECTIONS);
        occlusion = pow(saturate(occlusion), 0.6) * _Intensity;
        
        float ao = saturate(1.0 - occlusion);
        ao = MultiBounce(ao);
        
        return ao;
    }
    
    ENDHLSL
    
    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }
        
        // Pass 0: SSAO (remplace les couleurs)
        Pass
        {
            Name "SSAO"
            ZWrite Off ZTest Always Blend Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragSSAO
            ENDHLSL
        }
        
        // Pass 1: unused
        Pass
        {
            Name "Unused"
            ZWrite Off ZTest Always Blend Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragSSAO
            ENDHLSL
        }
        
        // Pass 2: Composite (multiply)
        Pass
        {
            Name "Composite"
            ZWrite Off ZTest Always Blend DstColor Zero Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragSSAO
            ENDHLSL
        }
    }
    Fallback Off
}
