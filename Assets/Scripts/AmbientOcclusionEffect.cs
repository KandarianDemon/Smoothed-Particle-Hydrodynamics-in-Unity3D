using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AmbientOcclusionEffect : MonoBehaviour
{
    public Shader aoShader;
    private Material aoMaterial;

    void OnEnable()
    {
        GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
        aoMaterial = new Material(aoShader);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        RenderTexture aoRT = RenderTexture.GetTemporary(source.width, source.height, 0);
        Graphics.Blit(source, aoRT, aoMaterial, 2); // Use pass index 2 for AO

        aoMaterial.SetTexture("_AOTex", aoRT);
        Graphics.Blit(source, destination, aoMaterial, 3); // Use a new pass to combine AO with the original image

        RenderTexture.ReleaseTemporary(aoRT);
    }
}