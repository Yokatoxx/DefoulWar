using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[System.Serializable]
public class CustomSSAOPass : CustomPass
{
    public enum DebugMode
    {
        Off = 0,
        ShowDepth = 1,
        ShowNormals = 2,
        ShowAO = 3,
        TestWhite = 4,
        DepthColors = 5
    }
    
    [Header("SSAO Settings")]
    public float intensity = 5f;
    public float radius = 2f;
    public float bias = 0.5f;
    public float falloffDistance = 2f;
    
    [Header("Debug")]
    public DebugMode debugMode = DebugMode.Off;
    
    private Material ssaoMat;

    protected override bool executeInSceneView => true;

    protected override void Setup(ScriptableRenderContext ctx, CommandBuffer cmd)
    {
        var shader = Shader.Find("Hidden/CustomSSAO");
        if (shader != null)
            ssaoMat = CoreUtils.CreateEngineMaterial(shader);
    }

    protected override void Execute(CustomPassContext ctx)
    {
        if (ssaoMat == null) return;
        
        ssaoMat.SetInt("_DebugMode", (int)debugMode);
        ssaoMat.SetFloat("_Intensity", intensity);
        ssaoMat.SetFloat("_Radius", radius);
        ssaoMat.SetFloat("_Bias", bias);
        ssaoMat.SetFloat("_FalloffDistance", falloffDistance);
        
        if (debugMode == DebugMode.Off)
        {
            // Mode normal: utilise le pass multiplicatif (pass 1)
            ssaoMat.SetInt("_DebugMode", 0);
            CoreUtils.DrawFullScreen(ctx.cmd, ssaoMat, ctx.cameraColorBuffer, shaderPassId: 1);
        }
        else
        {
            // Modes debug: utilise le pass normal (pass 0)
            if (debugMode == DebugMode.ShowAO)
                ssaoMat.SetInt("_DebugMode", 0);
                
            CoreUtils.DrawFullScreen(ctx.cmd, ssaoMat, ctx.cameraColorBuffer, shaderPassId: 0);
        }
    }

    protected override void Cleanup()
    {
        CoreUtils.Destroy(ssaoMat);
    }
}
