using UnityEngine;

// 피격 연출용 런타임 머티리얼(번쩍, 조각)
// !!) 빌드에서 셰이더가 빠질 수 있다. 실제 빌드 전에는 머티리얼 에셋을 만들어 연결할 것
public static class HitMaterials
{
    public static Material CreateUnlit(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader) { name = materialName, enableInstancing = true };
        material.color = color;
        return material;
    }
}
