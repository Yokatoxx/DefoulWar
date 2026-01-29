Shader "Hidden/CustomSSAOBlur"
{
    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
    
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    
    int _BlurSize;
    
    TEXTURE2D_X(_AOTexture);
    SAMPLER(sampler_AOTexture);
    
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
    
    // Blur horizontal
    float4 FragBlurH(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        
        float2 uv = input.texcoord;
        float2 texelSize = _ScreenSize.zw;
        
        float result = 0.0;
        float totalWeight = 0.0;
        
        // Kernel gaussien simplifié
        for (int i = -_BlurSize; i <= _BlurSize; i++)
        {
            float weight = 1.0 - abs(i) / (float)(_BlurSize + 1);
            float2 offset = float2(i * texelSize.x, 0);
            result += SAMPLE_TEXTURE2D_X(_AOTexture, sampler_AOTexture, uv + offset).r * weight;
            totalWeight += weight;
        }
        
        return result / totalWeight;
    }
    
    // Blur vertical
    float4 FragBlurV(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        
        float2 uv = input.texcoord;
        float2 texelSize = _ScreenSize.zw;
        
        float result = 0.0;
        float totalWeight = 0.0;
        
        for (int i = -_BlurSize; i <= _BlurSize; i++)
        {
            float weight = 1.0 - abs(i) / (float)(_BlurSize + 1);
            float2 offset = float2(0, i * texelSize.y);
            result += SAMPLE_TEXTURE2D_X(_AOTexture, sampler_AOTexture, uv + offset).r * weight;
            totalWeight += weight;
        }
        
        return result / totalWeight;
    }
    
    ENDHLSL
    
    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }
        
        // Pass 0: Blur Horizontal
        Pass
        {
            Name "Blur Horizontal"
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBlurH
            ENDHLSL
        }
        
        // Pass 1: Blur Vertical
        Pass
        {
            Name "Blur Vertical"
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBlurV
            ENDHLSL
        }
    }
    
    Fallback Off
}
