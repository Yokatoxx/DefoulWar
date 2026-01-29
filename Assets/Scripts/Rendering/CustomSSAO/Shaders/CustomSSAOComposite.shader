Shader "Hidden/CustomSSAOComposite"
{
    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
    
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    
    float _Intensity;
    
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
    
    float4 FragComposite(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        
        float ao = SAMPLE_TEXTURE2D_X(_AOTexture, sampler_AOTexture, input.texcoord).r;
        
        // Lerp entre 1.0 (pas d'effet) et la valeur AO selon l'intensité
        ao = lerp(1.0, ao, _Intensity);
        
        return float4(ao, ao, ao, 1.0);
    }
    
    ENDHLSL
    
    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }
        
        Pass
        {
            Name "SSAO Composite"
            ZWrite Off
            ZTest Always
            Blend DstColor Zero  // Multiplie la couleur destination par le résultat
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite
            ENDHLSL
        }
    }
    
    Fallback Off
}
