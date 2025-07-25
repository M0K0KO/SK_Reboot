using UnityEngine;
using System.Collections.Generic;

// C#과 셰이더에서 동일하게 사용할 구조체 정의
[System.Serializable]
public struct AnimationMeta
{
    public int animationID;
    public int startFrameIndex;
    public int frameCount;
    public float playbackSpeed; // 16바이트 정렬
}

public struct UnitData
{
    public Vector3 position;
    public Quaternion rotation;
    public float animationFrame;
    public int animationID;
    public Vector3 padding;
}

// JSON 역직렬화를 위한 래퍼 클래스
[System.Serializable]
public class AnimationMetaListWrapper { public List<AnimationMeta> metas; }


public class UnitGPURenderer : MonoBehaviour
{
    private const int MAX_UNITS = 100000; // 최대 유닛 수
    private Camera mainCam;

    [Header("Assets")]
    public Mesh unitMesh;
    public Material unitMaterial;
    public ComputeShader packingComputeShader;
    public ComputeShader animationComputeShader;
    public ComputeShader cullingComputeShader;
    public TextAsset bakedAnimationData;
    public TextAsset bakedAnimationMeta;

    // 분리된 데이터 버퍼 (CPU -> GPU)
    private ComputeBuffer _positionBuffer;
    private ComputeBuffer _rotationBuffer;
    private ComputeBuffer _animationIDBuffer;

    // 통합 데이터 버퍼 (GPU 내부 처리용)
    private ComputeBuffer _unitDataBuffer;

    // 애니메이션 데이터 버퍼
    private ComputeBuffer _vertexAnimationBuffer;
    private ComputeBuffer _animationMetaBuffer;
    
    // 렌더링용 버퍼
    private ComputeBuffer _visibleInstanceIndexBuffer;
    private ComputeBuffer _argsBuffer;
    private uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };
    
    // 커널 ID
    private int _packingKernelId;
    private int _animationKernelId;
    private int _cullingKernelId;
    
    void Awake()
    {
        mainCam = Camera.main;
        
        /*if(unitMesh != null)
        {
            Debug.Log("Target Mesh: " + unitMesh.name + ", Vertex Count: " + unitMesh.vertexCount);
        }
        else
        {
            Debug.LogError("Unit Mesh가 할당되지 않았습니다!");
        }*/
        
        LoadAnimationData();
        InitializeBuffers();
        SetShaderParameters();
    }

    void LoadAnimationData()
    {
        // Meta.json 로드
        var metaWrapper = JsonUtility.FromJson<AnimationMetaListWrapper>(bakedAnimationMeta.text);
        List<AnimationMeta> animationMetas = metaWrapper.metas;

        // VertexAnimation.bytes 로드
        byte[] rawVertexBytes = bakedAnimationData.bytes;
        int vertexDataCount = rawVertexBytes.Length / (sizeof(float) * 4);
        _vertexAnimationBuffer = new ComputeBuffer(vertexDataCount, sizeof(float) * 4, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        _vertexAnimationBuffer.SetData(rawVertexBytes);
        
        // 메타데이터 버퍼 생성
        _animationMetaBuffer = new ComputeBuffer(animationMetas.Count, sizeof(int) * 4, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        _animationMetaBuffer.SetData(animationMetas);
    }

    void InitializeBuffers()
    {
        // 입력 버퍼 (SoA)
        _positionBuffer = new ComputeBuffer(MAX_UNITS, sizeof(float) * 3);
        _rotationBuffer = new ComputeBuffer(MAX_UNITS, sizeof(float) * 4);
        _animationIDBuffer = new ComputeBuffer(MAX_UNITS, sizeof(int));
        
        // 처리용 통합 버퍼 (AoS)
        _unitDataBuffer = new ComputeBuffer(MAX_UNITS, sizeof(float) * 12); // UnitData (48바이트)

        // 컬링 결과 버퍼
        _visibleInstanceIndexBuffer = new ComputeBuffer(MAX_UNITS, sizeof(uint), ComputeBufferType.Append);
        
        // Indirect 렌더링 인자 버퍼
        _argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        _args[0] = unitMesh.GetIndexCount(0);
        _args[1] = 0;
        _args[2] = unitMesh.GetIndexStart(0);
        _args[3] = unitMesh.GetBaseVertex(0);
        _argsBuffer.SetData(_args);
        
        // 커널 ID 찾기
        _packingKernelId = packingComputeShader.FindKernel("PackData");
        _animationKernelId = animationComputeShader.FindKernel("UpdateAnimation");
        _cullingKernelId = cullingComputeShader.FindKernel("Cull");
    }

    void SetShaderParameters()
    {
        // Packing 셰이더
        packingComputeShader.SetBuffer(_packingKernelId, "_PositionBuffer", _positionBuffer);
        packingComputeShader.SetBuffer(_packingKernelId, "_RotationBuffer", _rotationBuffer);
        packingComputeShader.SetBuffer(_packingKernelId, "_AnimationIDBuffer", _animationIDBuffer);
        packingComputeShader.SetBuffer(_packingKernelId, "_UnitDataBufferRW", _unitDataBuffer);

        // Animation 셰이더
        animationComputeShader.SetBuffer(_animationKernelId, "_UnitDataBufferRW", _unitDataBuffer);
        animationComputeShader.SetBuffer(_animationKernelId, "_AnimationMetaBuffer", _animationMetaBuffer);

        // Culling 셰이더
        cullingComputeShader.SetBuffer(_cullingKernelId, "_UnitDataBuffer", _unitDataBuffer);
        cullingComputeShader.SetBuffer(_cullingKernelId, "_VisibleInstanceIndices", _visibleInstanceIndexBuffer);
        
        // 렌더링 Material
        unitMaterial.SetBuffer("_UnitDataBuffer", _unitDataBuffer);
        unitMaterial.SetBuffer("_VisibleInstanceIndices", _visibleInstanceIndexBuffer);
        unitMaterial.SetBuffer("_VertexAnimationBuffer", _vertexAnimationBuffer);
        unitMaterial.SetBuffer("_AnimationMetaBuffer", _animationMetaBuffer);
        unitMaterial.SetInt("_VertexCount", unitMesh.vertexCount);
        unitMaterial.SetInt("_TotalVertexDataCount", _vertexAnimationBuffer.count);
    }

    void LateUpdate()
    {
        // UnitDataManager가 NativeArray들을 가지고 있다고 가정
        int unitCount = UnitDataManager.Instance.activeUnitCount;
        if (unitCount == 0) 
        {
            _args[1] = 0;
            _argsBuffer.SetData(_args);
            return;
        }

        // --- 1. 데이터 복사 (CPU -> GPU) ---
        _positionBuffer.SetData(UnitDataManager.Instance.positions, 0, 0, unitCount);
        _rotationBuffer.SetData(UnitDataManager.Instance.rotations, 0, 0, unitCount);
        _animationIDBuffer.SetData(UnitDataManager.Instance.unitAnimationID, 0, 0, unitCount);

        int threadGroups = Mathf.CeilToInt(unitCount / 64.0f);

        // --- 2. 데이터 패킹 (SoA -> AoS) ---
        packingComputeShader.SetInt("_InstanceCount", unitCount);
        packingComputeShader.Dispatch(_packingKernelId, threadGroups, 1, 1);

        // --- 3. 애니메이션 업데이트 ---
        animationComputeShader.SetFloat("_DeltaTime", Time.deltaTime);
        animationComputeShader.SetInt("_InstanceCount", unitCount);
        animationComputeShader.Dispatch(_animationKernelId, threadGroups, 1, 1);
        
        // --- 4. 프러스텀 컬링 ---
        _visibleInstanceIndexBuffer.SetCounterValue(0);
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCam);
        Vector4[] planeData = new Vector4[6];
        for (int i = 0; i < 6; i++) { planeData[i] = new Vector4(frustumPlanes[i].normal.x, frustumPlanes[i].normal.y, frustumPlanes[i].normal.z, frustumPlanes[i].distance); }
        
        cullingComputeShader.SetVectorArray("_FrustumPlanes", planeData);
        cullingComputeShader.SetInt("_InstanceCount", unitCount);
        cullingComputeShader.Dispatch(_cullingKernelId, threadGroups, 1, 1);
        
        // --- 5. 렌더링 ---
        ComputeBuffer.CopyCount(_visibleInstanceIndexBuffer, _argsBuffer, sizeof(uint));
        Graphics.DrawMeshInstancedIndirect(unitMesh, 0, unitMaterial, new Bounds(Vector3.zero, Vector3.one * 1000f), _argsBuffer);
    }

    void OnDestroy()
    {
        _positionBuffer?.Release();
        _rotationBuffer?.Release();
        _animationIDBuffer?.Release();
        _unitDataBuffer?.Release();
        _vertexAnimationBuffer?.Release();
        _animationMetaBuffer?.Release();
        _visibleInstanceIndexBuffer?.Release();
        _argsBuffer?.Release();
    }
}