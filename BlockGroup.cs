using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TarodevController;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class BlockGroup : MonoBehaviour, IPoolable
{
    public SpriteRenderer[] ChildRenderers;
    private bool isVisible = true;
    private BlockSpawner blockSpawner;
    private LaserSpawner laserSpawner;
    private EnemySpawner enemySpawner;
    private float spawnHeight => blockSpawner.SpawnHeight;
    private List<BlockTypeDistribution> blockTypeDistributions;
    [SerializeField] private bool isRandom;
    public bool IsRandom => isRandom;
    [SerializeField] private List<BlockTypeEnum> excludedBlockTypes = new List<BlockTypeEnum>();
    public List<BlockTypeEnum> ExcludedBlockTypes => excludedBlockTypes;
    [SerializeField] private List<GameObject> lasers;
    public List<GameObject> Lasers => lasers;
    private int remainSwitch = 2;
    private float _potionProbability = 0.1f;
    private Coroutine visibilityCoroutine;
    private static readonly WaitForSeconds wait = new WaitForSeconds(.2f);
    private static readonly WaitForSeconds wait2 = new WaitForSeconds(.02f);
    private static readonly WaitForSeconds sec3 = new WaitForSeconds(3f);
    
    private System.Random random;
    public void InitializeRandom(int seed)
    {
        random = new System.Random(seed);
    }

    #region Visibility Check
    
    void OnEnable()
    {
        StartVisibilityCheck();
    }
    
    public void OnDisable()
    {
        StopVisibilityCheck();
    }
    
    public void StartVisibilityCheck()
    {
        if (visibilityCoroutine != null)
        {
            StopCoroutine(visibilityCoroutine);
        }
        visibilityCoroutine = StartCoroutine(CheckVisibility());
    }
    
    public void StopVisibilityCheck()
    {
        if (visibilityCoroutine != null)
        {
            StopCoroutine(visibilityCoroutine);
            visibilityCoroutine = null;
        }
    }
    
    private IEnumerator CheckVisibility()
    {
        while (true)
        {
            if (InGameManager.Instance.PlayerObj == null)
            {
                yield return null;
                continue;
            }
            bool isVisible = IsVisible();
            SetChildRendererEnabled(isVisible);
            if (InGameManager.Instance.PlayerController.CurrentState != PlayerState.Normal || InGameManager.Instance.PlayerController.IsUsingRocketAndNotGrounded)
            {
                yield return null;
            }
            else
            {
                yield return wait;
            }
        }
    }
    
    private bool IsVisible()
    {
        return Mathf.Abs(InGameManager.Instance.PlayerObj.transform.position.y - transform.position.y) < 30f;
    }
    
    public void SetChildRendererEnabled(bool enabled)
    {
        foreach (var renderer in ChildRenderers)
        {
            renderer.enabled = enabled;
        }
    }
    
    #endregion
    
    public void OnSpawn()
    {
        if (blockSpawner == null || laserSpawner == null || enemySpawner == null) 
        {
            blockSpawner = InGameManager.Instance.BlockSpawner;
            laserSpawner = InGameManager.Instance.LaserSpawner;
            enemySpawner = InGameManager.Instance.EnemySpawner;
        }
        if (blockTypeDistributions == null || blockTypeDistributions.Count == 0)
        {
            blockTypeDistributions = blockSpawner.ActiveDistributions;
        }
        remainSwitch = 2;
        HandleMeterText();

        for (int i = 0; i < transform.childCount - 1; i++)
        { 
            BlockTypeEnum tmpType = GetRandomBlockType(i);
            BlockType blockType = transform.GetChild(i).GetComponent<BlockType>();  
            if (blockType == null) continue;
            blockType.Init(tmpType);

            if (blockType.transform.position.x != (-3.5f + i))
            {
                //Debug.Log($"{transform.GetChild(i).name}의 위치가 잘못되었습니다. 수정합니다.");
                blockType.transform.position = new Vector3(-3.5f + i, blockType.transform.position.y, blockType.transform.position.z);
            }
            
            if (i > 8)
            {
                Debug.Log($"{transform.GetChild(i).name}이 발견되었습니다. 삭제합니다.");
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        //Wing Potion 일때
        for (int i = 0; i < transform.childCount-1; i++)
        {
            BlockType blockType = transform.GetChild(i).GetComponent<BlockType>();
            if (blockType == null) continue;
            if (blockType.BlockTypeValue == BlockTypeEnum.WingPotion)
            {
                //플레이어 체력 비교
                if (InGameManager.Instance.Player.MaxHp <= InGameManager.Instance.Player.Hp)
                {
                    blockType.Init(BlockTypeEnum.Yellow);
                }
                else if (Random.value < _potionProbability)
                {
                    blockType.Init(BlockTypeEnum.WingPotion);
                    _potionProbability = 0f;
                }
                else
                {
                    var increaseValue = .2f;
                    if (GameManager.Instance.CurrentAnimal == Animal.Horse) increaseValue = .3f;
                    _potionProbability += increaseValue;
                }
            }
        }

        Span<int> iceIdx = stackalloc int[transform.childCount - 1];
        // 얼음 블록이면 양 옆 블록도 얼음 블록으로 변경
        int iceCount = 0;
        for (int i = 0; i < transform.childCount - 1; i++)
        {
            BlockType blockType = transform.GetChild(i).GetComponent<BlockType>();
            if (blockType == null) continue;
            if (blockType.BlockTypeValue == BlockTypeEnum.Ice)
            {
                iceIdx[iceCount++] = i;
            }
        }
        for (int i = 0; i < iceCount; i++)
        {
            ChangeSideBlockType(BlockTypeEnum.Ice, 1, iceIdx[i]);
        }
        
        CheckUnBreakable();
    }

    public void InitChildBlockType(BlockGroupInfo _blockGroupInfo)
    {
        excludedBlockTypes = _blockGroupInfo.ExcludedBlockTypes;
        isRandom = _blockGroupInfo.IsRandom;
        // 필요한 컴포넌트들 초기화
        if (blockSpawner == null || laserSpawner == null || enemySpawner == null) 
        {
            blockSpawner = InGameManager.Instance.BlockSpawner;
            laserSpawner = InGameManager.Instance.LaserSpawner;
            enemySpawner = InGameManager.Instance.EnemySpawner;
        }

        // blockTypeDistributions 초기화
        if (blockTypeDistributions == null || blockTypeDistributions.Count == 0)
        {
            blockTypeDistributions = blockSpawner.ActiveDistributions;
        }
        for (int i = 0; i < transform.childCount - 1; i++)
        {
            BlockType blockType = transform.GetChild(i).GetComponent<BlockType>();
            if (blockType == null) continue;
            
            if (blockType.transform.position.x != (-3.5f + i))
            {
                //Debug.Log($"{transform.GetChild(i).name}의 위치가 잘못되었습니다. 수정합니다.");
                blockType.transform.position = new Vector3(-3.5f + i, blockType.transform.position.y, blockType.transform.position.z);
            }
            
            if (isRandom)
            {
                BlockTypeEnum tmpType = GetRandomBlockType(i);
                blockType.Init(tmpType);
            }
            else
            {
                blockType.Init((BlockTypeEnum)_blockGroupInfo.BlockGroupTypeArray[i]);
            }
        }
        HandleMeterText();
    }

    public void HandleMeterText()
    {
        if ((spawnHeight - 1) % 35 == 0 && spawnHeight != 1)
        {
            var meterText = transform.Find("MeterText");
            meterText.gameObject.SetActive(true);
            var modifiedMeter = (int)(spawnHeight - 1) * 10 / 35;
            if (InGameManager.Instance.SpaceShipExecuted) modifiedMeter += 350;
            meterText.GetComponent<TextMeshPro>().text = $"-{modifiedMeter}m";
        }
        else
        {
            transform.Find("MeterText").gameObject.SetActive(false);
        }
    }

    public void HandleAllBlockBreakEvent()
    {
        if (CheckAllChildBroken())
        {
            //Get score
            int upScore = (int)Mathf.Round(InGameManager.Instance.UpgradeCategories[UpgradeCategory.ScoreBreakAllBlock]);
            InGameManager.AddScore(upScore, transform.position);
            //SuperJump
            InGameManager.Instance.PlayerController.SetTrigger(true);
            InGameManager.Instance.PlayerController.StartCoroutine(InGameManager.Instance.PlayerController.ExecuteItemRocket(.5f));
        }
    }

    public void CheckUnBreakable()
    {
        //모든 블럭이 UnBreakable이면 랜덤한 블럭 하나를 Breakable로 변경
        var _isAllUnBreakable = true;
        for (int i = 0; i < transform.childCount - 1; i++)
        {
            BlockType blockType = transform.GetChild(i).GetComponent<BlockType>();
            if (blockType == null) continue;
            if (blockType.GetComponent<BreakableBlock>().IsBroken) continue;
            if (blockType.Breakable || blockType.BlockTypeValue == BlockTypeEnum.Yellow)
            {
                _isAllUnBreakable = false;
                break;
            }
        }
        
        if (_isAllUnBreakable)
        {
            int randomIdx = Random.Range(0, transform.childCount - 1);
            BlockType blockType = transform.GetChild(randomIdx).GetComponent<BlockType>();
            BlockTypeEnum[] tmpTypes = {BlockTypeEnum.Brown, BlockTypeEnum.Yellow};
            BlockTypeEnum tmpType = tmpTypes[Random.Range(0, tmpTypes.Length)];
            blockType.Init(tmpType);
        }
    }

    public void HandleCheckAllIceBlock()
    {
        if (CheckAllChildBroken())
        {
            //if all childs are ice
            bool isAllIce = true;
            for (int i = 0; i < transform.childCount - 1; i++)
            {
                BlockType blockType = transform.GetChild(i).GetComponent<BlockType>();
                if (blockType.BlockTypeValue != BlockTypeEnum.Ice)
                {
                    isAllIce = false;
                    break;
                }
            }

            if (isAllIce)
            {
                InGameManager.Instance.InGameShowStringText("IceBreaker!", transform.position + Vector3.down * 2f, new Color(.5f, 0.7f, 1f), 5, isBorder: true);
            }
        }
    }

    public void HandleCantJumpUpSituation()
    {
        if (CheckAllChildBroken())
        {
            InGameManager.Instance.DailyMissionVariables.IncrementMission(DailyMissionType.BreakAllBlock);
            StartCoroutine(CantJumpUpSituationCO());
        }
    }
    
    private IEnumerator CantJumpUpSituationCO()
    {
        yield return wait;
        for (int i = 0; i < transform.childCount - 1; i++)
        {
            BlockType blockType = transform.GetChild(i).GetComponent<BlockType>();
            if (blockType == null) continue;
            BreakableBlock breakableBlock = blockType.GetComponent<BreakableBlock>();
            breakableBlock.RendererOn();

            blockType.Init(BlockTypeEnum.OnOffInvisible);
        }
        yield return sec3;
        for (int i = 0; i < transform.childCount - 1; i++)
        {
            BlockType blockType = transform.GetChild(i).GetComponent<BlockType>();
            if (blockType == null) continue;
            blockType.Init(BlockTypeEnum.Yellow);
            
            GameObject changeBlockParticle = Instantiate(Resources.Load<GameObject>("Prefabs/Particles/VFX_Broken_Wall"));
            changeBlockParticle.transform.position = blockType.transform.position;
            
            SoundManager.Instance.PlaySound("Fantasy click sound 10", .3f);
            SoundManager.Instance.PlaySound("Pop sound 10", .3f);
            
            Destroy(changeBlockParticle, 2f);
        }
    }
    
    private bool CheckAllChildBroken()
    {
        int brokenCount = 0;
        for (int i = 0; i < transform.childCount - 1; i++)
        {
            if (transform.GetChild(i).TryGetComponent(out BreakableBlock breakableBlock))
            {
                if (breakableBlock.IsBroken)
                {
                    brokenCount++;
                }
            }
        }
        if (brokenCount >= transform.childCount - 2)
        {
            return true;
        }
        return false;
    }
    
    public void OnDespawn()
    {
        // 오브젝트가 비활성화될 때 정리 작업 수행
    }
    
    public void SwitchTouchEvent(BreakableBlock child)
    {
        remainSwitch--;
        if (GameManager.Instance.CurrentAnimal == Animal.Dolphin)
        {
            remainSwitch = 0;
        }
        //find child index
        int childIdx = -1;
        for (int i = 0; i < transform.childCount - 1; i++)
        {
            if (transform.GetChild(i).gameObject == child.gameObject)
            {
                childIdx = i;
                break;
            }
        }
        if (childIdx == -1) return;
        if (lasers == null || lasers.Count <= 0) return;
        if (childIdx == 1)
        {
            //Destroy left laser
            Destroy(lasers[0]);
        }
        else if (childIdx == 6)
        {
            //Destroy right laser
            Destroy(lasers[1]);
        }

        if (remainSwitch == 0)
        {
            AllSwitchTouchEvent();
        }
    }
    
    public void AllSwitchTouchEvent()
    {
        if (lasers != null)
        {
            foreach (var laser in lasers)
            {
                Destroy(laser);
            }
        }
        
        for (int i = 0; i < transform.childCount - 1; i++)
        {
            if (i != 1 && i != 6)
            {
                BlockType blockType = transform.GetChild(i).GetComponent<BlockType>();
                if (blockType == null) continue;
                GameObject changeBlockParticle = Instantiate(Resources.Load<GameObject>("Prefabs/Particles/VFX_Broken_Wall"));
                changeBlockParticle.transform.position = transform.GetChild(i).transform.position;
                Destroy(changeBlockParticle, 2f);
                transform.GetChild(i).GetComponent<BreakableBlock>().SwitchRendererOn();
                blockType.Init(BlockTypeEnum.Yellow);
            }
            else
            {
                BreakableBlock breakableBlock = transform.GetChild(i).GetComponent<BreakableBlock>();
                breakableBlock.ModifyColliderSize(1f);
            }
        }
        SoundManager.Instance.PlaySound("Ui Success 19", .07f);
    }
    
    private void AdjustBlockProbabilitiesByTheme(List<BlockTypeDistribution> distributions)
    {
        Theme currentTheme = InGameManager.Instance.BackgroundController.CurrentTheme;

        foreach (var distribution in distributions)
        {
            switch (currentTheme)
            {
                case Theme.Iceberg:
                    if (distribution.blockType == BlockTypeEnum.Ice)
                        distribution.probability = 15f;
                    if (distribution.blockType == BlockTypeEnum.Cactus)
                        distribution.probability = 0f;
                    break;
                case Theme.Desert:
                    if (distribution.blockType == BlockTypeEnum.Cactus)
                        distribution.probability = 2f;
                    if (distribution.blockType == BlockTypeEnum.Ice)
                        distribution.probability = 0f;
                    break;
                default:
                    if (distribution.blockType == BlockTypeEnum.Ice)
                        distribution.probability = 0f;
                    if (distribution.blockType == BlockTypeEnum.Cactus)
                        distribution.probability = 0f;
                    break;
            }
        }
    }
    
    private BlockTypeEnum GetRandomBlockType(int index)
    {
        float elapsedTime = InGameManager.Instance.ElapsedTime;
        //float randomValue = Random.value;
        float randomValue = (float)random.NextDouble(); // Random.value 대신 사용
        float sumProbability = 0f;
        float cumulativeProbability = 0f;

        // Yellow 블록의 확률 조정
        float adjustedYellowProbability = Mathf.Lerp(80f, 25f, Mathf.Clamp01(elapsedTime / 120f));

        // Empty 블록의 확률 조정
        float adjustedEmptyProbability = Mathf.Lerp(0f, 20f, Mathf.Clamp01((elapsedTime - 500f) / 500f));

        // 유효한 블록 타입만 필터링
        var validDistributions = blockTypeDistributions.Where(dist => 
            elapsedTime >= dist.timeRange.x * 60f && 
            elapsedTime <= dist.timeRange.y * 60f &&
            !((index == 0 || index == 1 || index == 6 || index == 7) && dist.blockType == BlockTypeEnum.ChangeDirection) &&
            !((index == 1 || index == 2 || index == 5 || index == 6) && dist.blockType == BlockTypeEnum.Empty) &&
            !excludedBlockTypes.Contains(dist.blockType)
        ).ToList();

        // 테마에 따라 블록 확률 조정
        AdjustBlockProbabilitiesByTheme(validDistributions);

        // 확률 합계 계산 및 조정
        foreach (var distribution in validDistributions)
        {
            if (distribution.blockType == BlockTypeEnum.Yellow)
                distribution.probability = adjustedYellowProbability;
            else if (distribution.blockType == BlockTypeEnum.Empty)
                distribution.probability = adjustedEmptyProbability;

            sumProbability += distribution.probability;
        }

        // 블록 타입 선택
        foreach (var distribution in validDistributions)
        {
            cumulativeProbability += distribution.probability / sumProbability;
            if (randomValue <= cumulativeProbability)
            {
                return distribution.blockType;
            }
        }

        // 만약 선택되지 않았다면 기본값 반환
        return BlockTypeEnum.Brown;
    }
    private void ChangeSideBlockType(BlockTypeEnum blockType, int sideNum, int curIdx)
    {
        for (int i = 1; i <= sideNum; i++)
        {
            int leftTargetIdx = curIdx - i;
            int rightTargetIdx = curIdx + i;

            if (leftTargetIdx >= 0 && leftTargetIdx < transform.childCount - 1)
            {
                BlockType leftTargetBlockType = transform.GetChild(leftTargetIdx).GetComponent<BlockType>();
                if (leftTargetBlockType == null) continue;
                if (leftTargetBlockType.BlockTypeValue != BlockTypeEnum.Ice)
                {
                    leftTargetBlockType.Init(blockType);
                }
            }
            if (rightTargetIdx >= 0 && rightTargetIdx < transform.childCount - 1)
            {
                BlockType rightTargetBlockType = transform.GetChild(rightTargetIdx).GetComponent<BlockType>();
                if (rightTargetBlockType == null) continue;
                if (rightTargetBlockType.BlockTypeValue != BlockTypeEnum.Ice)
                {
                    rightTargetBlockType.Init(blockType);
                }
            }
        }
    }
    
    public int[] GetBlockGroupTypeArray()
    {
        int[] blockGroupTypeArray = new int[Statics.BLOCK_PER_GROUP];
        for (int i = 0; i < Statics.BLOCK_PER_GROUP; i++)
        {
            BlockType blockType = transform.GetChild(i).GetComponent<BlockType>();
            if (blockType == null) continue;
            blockGroupTypeArray[i] = (int)blockType.BlockTypeValue;
        }
        return blockGroupTypeArray;
    }
    
    public void InitLasers(List<GameObject> _lasers)
    {
        lasers = _lasers;
    }
}

public class BlockGroupInfo
{
    private bool isRandom;
    public bool IsRandom => isRandom;
    private List<BlockTypeEnum> excludedBlockTypes;
    public List<BlockTypeEnum> ExcludedBlockTypes => excludedBlockTypes;
    private int[] blockGroupTypeArray;
    public int[] BlockGroupTypeArray => blockGroupTypeArray;
    
    private List<GameObject> lasers;
    public List<GameObject> Lasers => lasers;
    public BlockGroupInfo(bool isRandom, List<BlockTypeEnum> excludedBlockTypes, int[] blockGroupTypeArray)
    {
        this.isRandom = isRandom;
        this.excludedBlockTypes = excludedBlockTypes;
        this.blockGroupTypeArray = blockGroupTypeArray;
    }
}

public class OtherObject
{
    private GameObject prefab;
    public GameObject Prefab => prefab;
    private Vector2 position;
    public Vector2 Position => position;
    
    public OtherObject(GameObject prefab, Vector2 position)
    {
        this.prefab = prefab;
        this.position = position;
    }
}

public class BlockGroupPrefabInfo
{
    private List<BlockGroupInfo> blockGroupInfos;
    public List<BlockGroupInfo> BlockGroupInfos => blockGroupInfos;
    private List<OtherObject> otherObjectInfos;
    public List<OtherObject> OtherObjectInfos => otherObjectInfos;
    
    public BlockGroupPrefabInfo(List<BlockGroupInfo> blockGroupInfos, List<OtherObject> otherObjectInfos)
    {
        this.blockGroupInfos = blockGroupInfos;
        this.otherObjectInfos = otherObjectInfos;
    }
}