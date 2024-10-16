using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "BlockGroupPrefabDatabase", menuName = "ScriptableObjects/BlockGroupPrefabDatabase")]
public class BlockGroupPrefabDatabase : ScriptableObject
{
    public enum SpawnProbability
    {
        VeryLow = 1,
        Low = 3,
        Medium = 5,
        High = 7,
        VeryHigh = 9,
        Debug = 9999 // 디버그 용도, 특정 블록 그룹을 강제로 스폰하고 싶을 때 사용
    }

    [Header("블록 그룹 폴더")]
    public List<BlockGroupFolder> blockGroupFolders = new();

    [SerializeField]
    [Tooltip("저장하고 있는 최근 프리팹의 최대 수")]
    private int maxRecentPrefabsCount = 3;

    private readonly Queue<GameObject> recentPrefabs = new();
    private readonly List<BlockGroupEntry> reusableSpawnableEntries = new();

    /// <summary>
    ///     현재 높이에 맞는 스폰 가능한 블록 그룹 프리팹을 우선순위에 따라 랜덤 반환한다.
    /// </summary>
    /// <param name="curHeight"></param>
    /// <returns></returns>
    public GameObject GetSpawnableBlockGroupPrefabWithHeight(float curHeight)
    {
        // 매번 새로운 리스트를 생성하는 대신 기존 리스트를 재사용합니다.
        reusableSpawnableEntries.Clear();

        var spaceShipHeightAdjustment = InGameManager.Instance.SpaceShipExecuted ? Statics.SpaceShipHeight : 0;

        foreach (var folder in blockGroupFolders)
        foreach (var entry in folder.entries)
        {
            var adjustedMinHeight = (int)entry.minHeight - spaceShipHeightAdjustment;
            var adjustedMaxHeight = (int)entry.maxHeight - spaceShipHeightAdjustment;

            if (curHeight >= adjustedMinHeight && curHeight < adjustedMaxHeight &&
                !recentPrefabs.Contains(entry.prefab)) reusableSpawnableEntries.Add(entry);
        }

        if (reusableSpawnableEntries.Count > 0)
        {
            var totalWeight = 0;
            foreach (var entry in reusableSpawnableEntries) totalWeight += (int)entry.probability;

            var randomWeight = Random.Range(0, totalWeight);
            var accumulatedWeight = 0;

            foreach (var entry in reusableSpawnableEntries)
            {
                accumulatedWeight += (int)entry.probability;
                if (randomWeight < accumulatedWeight)
                {
                    var selectedPrefab = entry.prefab;
                    UpdateRecentPrefabs(selectedPrefab);
                    return selectedPrefab;
                }
            }
        }

        // 새로운 적합한 프리팹을 찾지 못한 경우 최근 프리팹을 사용함
        if (recentPrefabs.Count > 0) return recentPrefabs.Peek(); // 큐의 상태를 유지하기 위해 Peek을 사용함

        return null;
    }

    private void UpdateRecentPrefabs(GameObject prefab)
    {
        recentPrefabs.Enqueue(prefab);
        if (recentPrefabs.Count > maxRecentPrefabsCount) recentPrefabs.Dequeue();
    }

    [Serializable] public class BlockGroupEntry
    {
        [Tooltip("프리팹을 담을 변수")]
        public GameObject prefab;

        [Tooltip("스폰 가능한 최소 테마 높이")]
        public ThemeHeight minHeight;

        [Tooltip("스폰 가능한 최대 테마 높이")]
        public ThemeHeight maxHeight;

        [Tooltip("높이")]
        public int floorSize;

        [Tooltip("확률")]
        public SpawnProbability probability;

        public BlockGroupEntry()
        {
            minHeight = ThemeHeight.Forest;
            maxHeight = ThemeHeight.Infinity;
            floorSize = 3;
            probability = SpawnProbability.Medium;
        }
    }

    [Serializable] public class BlockGroupFolder
    {
        [Tooltip("사용자 정의 폴더 이름")]
        public string folderName;

        [Tooltip("폴더에 들어갈 블록 그룹 프리팹 리스트")]
        public List<BlockGroupEntry> entries = new();
    }
}