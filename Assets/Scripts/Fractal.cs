using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using state Unity.Mathematics.math;
using float4x4 = Unity.Mathematics.float4x4;
using quaternion = Unity.Mathematics.quaternion;

public class Fractal : MonoBehaviour{

    [BurstCompile(CompileSynchronously = true)]
    struct UpdateFractalLevelJob : IJobFor{

        public float spinAngleDelta;
        public float scale;

        [ReadOnly]
        public NativeArray<FractalPart> parents;
        public NativeArray<FractalPart> parts;

        [WriteOnly]
        public NativeArray<float4x4> matrices;

        public void Execute(int i){
            FractalPart parent = parents[i / 5];
			FractalPart part = parts[i];
			part.spinAngle += spinAngleDelta;
			part.worldRotation =
				parent.worldRotation *
				(part.rotation * quaternion.Euler(0f, part.spinAngle, 0f));
			part.worldPosition =
				parent.worldPosition +
				parent.worldRotation * (1.5f * scale * part.direction);
			parts[i] = part;

			matrices[i] = float4x4.TRS(
				part.worldPosition, part.worldRotation, scale * float3.one
			);
        }
    }

    struct FractalPart{
        public float3 direction, worldPosition;
        public quaternion rotation, worldRotation;
        public float spinAngle;
    }

    static readonly int matricesId = Shader.PropertyToID("_Matrices");

    static MaterialPropertyBlock propertyBlock;

    NativeArray<FractalPart>[] parts;

    NativeArray<float4x4>[] matrices;

    [SerializeField, Range(1, 8)]
    int depth = 4;

    [SerializeField]
    Mesh mesh;

    [SerializeField]
    Material material;

    static float3[] directions = {
        float3.up, float3.right, float3.left, float3.forward, float3.back
    };

    static quaternion[] rotations = {
        quaternion.identity,
        quaternion.Euler(0f, 0f, -90f), quaternion.Euler(0f, 0f, 90f),
        quaternion.Euler(90f, 0f, 0f), quaternion.Euler(-90f, 0f, 0f)
    };

    FractalPart CreatePart(int childIndex) => new FractalPart{
            direction = directions[childIndex],
            rotation = rotations[childIndex]
        };

    ComputeBuffer[] matricesBuffers;

    void OnEnable(){
        parts = new NativeArray<FractalPart>[depth];
        matrices = new NativeArray<float4x4>[depth];
        matricesBuffers = new ComputeBuffer[depth];
        int stride = 16 * 4;
        for(int i = 0, length = 1; i < parts.Length; i++, length *= 5){
            parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
            matrices[i] = new NativeArray<float4x4>(length, Allocator.Persistent);
            matricesBuffers[i] = new ComputeBuffer(length, stride);
        }

        parts[0][0] = CreatePart(0);
        for (int li = 1; li < parts.Length; li++) {
			NativeArray<FractalPart> levelParts = parts[li];
			for (int fpi = 0; fpi < levelParts.Length; fpi += 5) {
				for (int ci = 0; ci < 5; ci++) {
					levelParts[fpi + ci] = CreatePart(ci);
				}
			}
		}
        propertyBlock ??= new MaterialPropertyBlock();
    }

    void OnDisable(){
        for(int i = 0; i < matricesBuffers.Length; i++){
            matricesBuffers[i].Release();
            parts[i].Dispose();
            matrices[i].Dispose();
        }
        parts = null;
        matrices = null;
        matricesBuffers = null;
    }

    void OnValidate(){
        if(parts != null && enabled){
            OnDisable();
            OnEnable();
        }
    }

    void Update(){
        float spinAngleDelta = 22.5f * Time.deltaTime;
        FractalPart rootPart = parts[0][0];
        rootPart.spinAngle += spinAngleDelta;
        rootPart.worldRotation = 
            transform.rotation * 
            (rootPart.rotation * quaternion.Euler(0f, rootPart.spinAngle, 0f));
        rootPart.worldPosition = transform.position;
        parts[0][0] = rootPart;
        float objectScale = transform.lossyScale.x;
        matrices[0][0] = float4x4.TRS(
            rootPart.worldPosition, rootPart.worldRotation, objectScale * float3.one
        );
        float scale = objectScale;
        JobHandle jobHandle = default;
        for(int li = 1; li < parts.Length; li++){
            scale *= 0.5f;
            jobHandle = new UpdateFractalLevelJob{
                spinAngleDelta = spinAngleDelta,
                scale = scale,
                parents = parts[li - 1],
                parts = parts[li],
                matrices = matrices[li]
            }.Schedule(parts[li].Length, jobHandle);
        }
        jobHandle.Complete();
        var bounds = new Bounds(rootPart.worldPosition, 3f * objectScale * Vector3.one);
        for(int i = 0; i < matricesBuffers.Length; i++){
            ComputeBuffer buffer = matricesBuffers[i];
            buffer.SetData(matrices[i]);
            propertyBlock.SetBuffer(matricesId, buffer);
            Graphics.DrawMeshInstancedProcedural(
                mesh, 0, material, bounds, buffer.count, propertyBlock
            );
        }
    }
}