using UnityEngine;

public class Fractal : MonoBehaviour{

    struct FractalPart{
        public Vector3 direction, worldPosition;
        public Quaternion rotation, worldRotation;
    }
    FractalPart[][] parts;

    Matrix4x4[][] matrices;

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

    FractalPart CreatePart(int childIndex) => new FractalPart{
            direction = directions[childIndex],
            rotation = rotations[childIndex]
        };

    void Awake(){
        parts = new FractalPart[depth][];
        matrices = new Matrix4x4[depth][];
        for(int i = 0, length = 1; i < parts.Length; i++, length *= 5){
            parts[i] = new FractalPart[length];
            matrices[i] = new Matrix4x4[length];
        }

        parts[0][0] = CreatePart(0);
        for(int li = 1; li < parts.Length; li++){
            FractalPart[] parentParts = parts[li - 1];
            FractalPart[] levelParts = parts[li];
            int childIndex = 0; // Index for children in the current level
            for(int fpi = 0; fpi < parentParts.Length; fpi++){
                for(int ci = 0; ci < directions.Length; ci++){
                    levelParts[childIndex] = CreatePart(ci);
                    childIndex++;
                }
            }
        }
    }

    void Update(){
        Quaternion deltaRotation = Quaternion.Euler(0f, 22.5f * Time.deltaTime, 0f);

        FractalPart rootPart = parts[0][0];
        rootPart.rotation *= deltaRotation;
        rootPart.worldRotation = rootPart.rotation;
        parts [0][0] = rootPart;
        float scale = 1f;
        for(int li = 1; li < parts.Length; li++){
            scale *= 0.5f;
            FractalPart[] parentParts = parts[li - 1];
            FractalPart[] levelParts = parts[li];
            for(int fpi = 0; fpi < levelParts.Length; fpi++){
                FractalPart parent = parentParts[fpi / 5];
                FractalPart part = levelParts[fpi];
                part.rotation *= deltaRotation;
                part.worldRotation = parent.worldRotation * part.rotation;
                part.worldPosition = 
                    parent.worldPosition +
                    parent.worldRotation * (1.5f * scale * part.direction);
                levelParts[fpi] = part;
            }
        }
    }
}