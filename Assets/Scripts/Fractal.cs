using UnityEngine;

public class Fractal : MonoBehaviour{

    struct FractalPart{
        public Vector3 direction;
        public Quaternion rotation;
        public Transform transform;
    }
    FractalPart[][] parts;

    [SerializeField, Range(1, 8)]
    int depth = 4;

    [SerializeField]
    Mesh mesh;

    [SerializeField]
    Material material;

    static Vector3[] directions = {
        Vector3.up, Vector3.right, Vector3.left, Vector3.forward, Vector3.back
    };

    static Quaternion[] rotations = {
        Quaternion.identity,
        Quaternion.Euler(0f, 0f, -90f), Quaternion.Euler(0f, 0f, 90f),
        Quaternion.Euler(90f, 0f, 0f), Quaternion.Euler(-90f, 0f, 0f)
    };

    void CreatePart(int levelIndex, int childIndex){
        var go = new GameObject("Fractal Part L" + levelIndex + " C" + childIndex);
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().mesh = mesh;
        go.AddComponent<MeshRenderer>().material = material;
    }

    void Awake(){
        parts = new FractalPart[depth][];
        for(int i = 0, length = 1; i < parts.Length; i++, length *= 5){
            parts[i] = new FractalPart[length];
        }

        CreatePart(0, 0);
        for(int li = 1; li < parts.Length; li++){
            FractalPart[] levelParts = parts[li];
            for(int fpi = 0; fpi < levelParts.Length; fpi++){
                for(int ci = 0; ci < 5; ci++){
                    CreatePart(li, ci);
                }
            }
        }
    }
}