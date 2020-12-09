using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostProcessEffectScript : MonoBehaviour
{
	public Material mat;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
    	//src is the rendered scene input
    	Graphics.Blit(src, dest, mat);
    }
}
