using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[System.Serializable]
public class CustomSSAOPass : CustomPass
{
    public enum DebugMode
    {
        Off = 0,
        ShowAO = 3,
        TestWhite = 4,
        DepthColors = 5
    }
    
    [Header("SSAO Quality")]
    public float intensity = 3f;
    public float radius = 0.8f;
    public float bias = 0.2f;
    public float falloffDistance = 3f;
    
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
        
        // Debug modes et ShowAO: rendu direct
        if (debugMode != DebugMode.Off)
        {
            if (debugMode == DebugMode.ShowAO)
                ssaoMat.SetInt("_DebugMode", 0);
            CoreUtils.DrawFullScreen(ctx.cmd, ssaoMat, ctx.cameraColorBuffer, shaderPassId: 0);
            return;
        }
        
        // Mode Off: composite multiplicatif direct (pass 2)
        ssaoMat.SetInt("_DebugMode", 0);
        CoreUtils.DrawFullScreen(ctx.cmd, ssaoMat, ctx.cameraColorBuffer, shaderPassId: 2);
    }

    protected override void Cleanup()
    {
        CoreUtils.Destroy(ssaoMat);
    }
}
