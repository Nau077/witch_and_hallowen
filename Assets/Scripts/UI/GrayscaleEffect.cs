using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class GrayscaleEffect : MonoBehaviour
{
    [Range(0f, 1f)]
    public float intensity = 0f;

    public Material material;

    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (material == null)
        {
            Graphics.Blit(src, dst);
            return;
        }

        material.SetFloat("_Intensity", intensity);
        Graphics.Blit(src, dst, material);
    }
}
