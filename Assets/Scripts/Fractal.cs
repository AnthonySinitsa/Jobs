using UnityEngine;

public class Fractal : MonoBehaviour{

    struct FractalPart{
        public Vector3 direction, worldPosition;
        public Quaternion rotation, worldRotation;
        public float spinAngle;
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

    ComputeBuffer[] matricesBuffers;

    void OnEnable(){
        parts = new FractalPart[depth][];
        matrices = new Matrix4x4[depth][];
        matricesBuffers = new ComputeBuffer[depth];
        int stride = 16 * 4;
        for(int i = 0, length = 1; i < parts.Length; i++, length *= 5){
            parts[i] = new FractalPart[length];
            matrices[i] = new Matrix4x4[length];
            matricesBuffers[i] = new ComputeBuffer(length, stride);
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

    void OnDisable(){
        for(int i = 0; i < matricesBuffers.Length; i++){
            matricesBuffers[i].Release();
        }
    }
    void Update(){
        float spinAngleDelta = 22.5f * Time.deltaTime;
        FractalPart rootPart = parts[0][0];
        rootPart.spinAngle += spinAngleDelta;
        rootPart.worldRotation = 
            rootPart.rotation * Quaternion.Euler(0f, rootPart.spinAngle, 0f);
        parts[0][0] = rootPart;
        matrices[0][0] = Matrix4x4.TRS(
            rootPart.worldPosition, rootPart.worldRotation, Vector3.one
        );
        float scale = 1f;
        for(int li = 1; li < parts.Length; li++){
            scale *= 0.5f;
            FractalPart[] parentParts = parts[li - 1];
            FractalPart[] levelParts = parts[li];
            Matrix4x4[] levelMatrices = matrices[li];
            for(int fpi = 0; fpi < levelParts.Length; fpi++){
                FractalPart parent = parentParts[fpi / 5];
                FractalPart part = levelParts[fpi];
                part.spinAngle += spinAngleDelta;
                part.worldRotation = 
                    parent.worldRotation * 
                    (part.rotation * Quaternion.Euler(0f, part.spinAngle, 0f));
                part.worldPosition = 
                    parent.worldPosition +
                    parent.worldRotation * (1.5f * scale * part.direction);
                levelParts[fpi] = part;
                levelMatrices[fpi] = Matrix4x4.TRS(
                    part.worldPosition, part.worldRotation, scale * Vector3.one
                );
            }
        }
    }
}