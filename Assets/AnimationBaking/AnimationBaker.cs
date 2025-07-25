using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

// 이 스크립트는 게임 로직이 아니라, Unity 에디터에서 사용할 유틸리티 도구입니다.
public class AnimationBaker : MonoBehaviour
{
    [Header("베이킹 대상")]
    public GameObject sourceModelPrefab; // SkinnedMeshRenderer와 Animator가 포함된 원본 모델
    public List<AnimationClip> sourceClips; // 베이킹할 애니메이션 클립 리스트
    public List<float> playbackSpeeds;

    [Header("저장 경로")]
    public string saveDataPath = "Assets/BakedAnimationData.bytes";
    public string saveMetaPath = "Assets/BakedAnimationMeta.json";

    /// <summary>
    /// Inspector에서 이 컴포넌트를 우클릭하여 실행할 수 있습니다.
    /// </summary>
    [ContextMenu("Execute Bake Animations (Multiple)")]
    public void BakeMultiple()
    {
        if (sourceModelPrefab == null || sourceClips == null || sourceClips.Count == 0)
        {
            Debug.LogError("모델과 클립을 할당해주세요!");
            return;
        }

        // --- 1. 베이킹을 위한 임시 모델 생성 ---
        GameObject modelInstance = Instantiate(sourceModelPrefab);
        SkinnedMeshRenderer smr = modelInstance.GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr == null)
        {
            Debug.LogError("모델에서 SkinnedMeshRenderer를 찾을 수 없습니다.");
            DestroyImmediate(modelInstance);
            return;
        }
        // Animator가 없으면 추가해서라도 진행
        Animator animator = modelInstance.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            animator = modelInstance.AddComponent<Animator>();
        }
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // --- 2. 데이터 저장을 위한 리스트 초기화 ---
        List<Vector4> allFramesVertexData = new List<Vector4>();
        List<AnimationMeta> allMetas = new List<AnimationMeta>();
        int totalVertexOffset = 0;
        int vertexCount = smr.sharedMesh.vertexCount;

        // --- 3. 모든 클립을 순회하며 베이킹 ---
        for (int i = 0; i < sourceClips.Count; i++)
        {
            AnimationClip clip = sourceClips[i];
            if (clip == null) continue;

            int frameCount = Mathf.NextPowerOfTwo((int)(clip.frameRate * clip.length));
            float perFrameTime = clip.length / frameCount;
            float speed = (i < playbackSpeeds.Count) ? playbackSpeeds[i] : 1.0f;

            // 메타데이터 생성 및 기록
            AnimationMeta meta = new AnimationMeta
            {
                animationID = i,
                startFrameIndex = totalVertexOffset,
                frameCount = frameCount,
                playbackSpeed = speed,
            };
            allMetas.Add(meta);

            Debug.Log($"Baking Clip: '{clip.name}', ID: {i}, Frames: {frameCount}, StartIndex: {totalVertexOffset}");

            Mesh bakedMesh = new Mesh();
            for (int frame = 0; frame < frameCount; frame++)
            {
                clip.SampleAnimation(modelInstance, frame * perFrameTime);
                smr.BakeMesh(bakedMesh, true);
                
                // 베이킹된 정점 데이터를 거대 리스트에 추가
                allFramesVertexData.AddRange(bakedMesh.vertices.ToVector4Array());
            }
            
            // 다음 클립의 시작 인덱스를 위해 오프셋 누적
            totalVertexOffset += frameCount;
        }

        DestroyImmediate(modelInstance);

        // --- 4. 파일로 저장 ---
        SaveVertexDataToFile(allFramesVertexData.ToArray());
        SaveMetaToFile(allMetas);

        Debug.Log("모든 애니메이션 베이킹 완료!");
    }

    private void SaveVertexDataToFile(Vector4[] data)
    {
        // Vector4 배열을 byte 배열로 안전하게 변환
        NativeArray<Vector4> nativeData = new NativeArray<Vector4>(data, Allocator.Temp);
        NativeArray<byte> byteData = nativeData.Reinterpret<byte>(sizeof(float) * 4);

        string fullPath = Application.dataPath + saveDataPath.Substring("Assets".Length);
        System.IO.File.WriteAllBytes(fullPath, byteData.ToArray());
        
        nativeData.Dispose();
        
        Debug.Log($"정점 데이터 저장 완료: {saveDataPath}");
    }

    private void SaveMetaToFile(List<AnimationMeta> metas)
    {
        // C# 객체를 JSON 문자열로 변환하여 저장
        AnimationMetaListWrapper wrapper = new AnimationMetaListWrapper { metas = metas };
        string json = JsonUtility.ToJson(wrapper, true);
        string fullPath = Application.dataPath + saveMetaPath.Substring("Assets".Length);
        System.IO.File.WriteAllText(fullPath, json);

        Debug.Log($"메타데이터 저장 완료: {saveMetaPath}");
    }
    
    [ContextMenu("Check First Vertex Data")]
    public void CheckFirstVertexData()
    {
        if (System.IO.File.Exists(saveDataPath))
        {
            byte[] bytes = System.IO.File.ReadAllBytes(saveDataPath);
            if (bytes.Length >= 16)
            {
                float x = System.BitConverter.ToSingle(bytes, 0);
                float y = System.BitConverter.ToSingle(bytes, 4);
                float z = System.BitConverter.ToSingle(bytes, 8);
                Debug.Log($"파일에서 읽은 첫 정점의 위치: ({x}, {y}, {z})");
            }
        }
        else
        {
            Debug.LogError("베이킹된 데이터 파일을 찾을 수 없습니다.");
        }
    }
}

// Vector3 배열을 Vector4 배열로 변환하는 확장 메소드
public static class Vector3ArrayExtensions
{
    public static Vector4[] ToVector4Array(this Vector3[] v3)
    {
        Vector4[] v4 = new Vector4[v3.Length];
        for (int i = 0; i < v3.Length; i++)
        {
            v4[i] = v3[i];
        }
        return v4;
    }
}