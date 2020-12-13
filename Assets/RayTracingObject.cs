//using System.Collections;
//using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class RayTracingObject : MonoBehaviour
{
    public Material material;
    private void OnEnable()
    {
        RayTracingMaster.RegisterObject(this);
    }
    private void OnDisable()
    {
        RayTracingMaster.UnregisterObject(this);
    }
    public Material GetMaterial()
    {
        return material;
    }
}
