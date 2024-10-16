using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BreakableBlock : MonoBehaviour
{
    [SerializeField] private UnityEvent _break;
    [SerializeField] private BlockType _blockType;
    [SerializeField] private BlockShaderController _blockShaderController;
    private float breakDelay = 0.1f; // 딜레이 시간 설정
    private Coroutine breakCoroutine;
    BlockTypeEnum blockType;
    public BlockTypeEnum BlockType => blockType;
    BlockColorEnum blockColor;
    private float _debrisSize;
    private bool _isBroken;
    private bool _isSwitched;
    private bool _isOn;
    public bool IsOn
    {
        get => _isOn;
        set => _isOn = value;
    }
    public BlockDirection DangerousSide { get; set; }
    private GameObject blockGroup;

    public bool IsBroken
    {
        get => _isBroken;
        set => _isBroken = value;
    }

    private bool _isBomb = false;
    public bool IsBomb => _isBomb;

    private int _hp = 1;
    private int[] _rainbowColorArray = { 60, 0, 290, 150 };

    public int Hp
    {
        get => _hp;
        set => _hp = value;
    }

    public void Init()
    {
        blockType = _blockType.BlockTypeValue;
        blockColor = _blockType.BlockColorValue;
        _debrisSize = _blockType.DebrisSize;
        _isBroken = false;
        DangerousSide = BlockDirection.None;
        _isBomb = false;
        _isSwitched = false;
        _isOn = false;

        if (blockType == BlockTypeEnum.Empty)
        {
            _isBroken = true;
        }
        
        if (blockType == BlockTypeEnum.Rainbow)
        {
            _hp = 2;
            _blockShaderController.ChangeColor(_rainbowColorArray[0]);
        }

        if (GameManager.Instance.CurrentAnimal == Animal.Lion || GameManager.Instance.CurrentAnimal == Animal.Badarang)
        {
            if (blockType == BlockTypeEnum.Black)
            {
                _hp = 2;
            }
        }

        if (blockType == BlockTypeEnum.RotatingSpike)
        {
            DangerousSide = BlockDirection.Down;
        }

        if (blockType == BlockTypeEnum.Moving)
        {
            float xLeft = Mathf.Max(-3.5f, transform.position.x - 2f);
            float xRight = Mathf.Min(3.5f, transform.position.x + 2f);
            Vector2 startPos = new Vector2(xLeft, transform.position.y);
            Vector2 targetPos = new Vector2(xRight, transform.position.y);
            MoveBlockBetweenTarget(startPos, targetPos, 3f);
        }

        blockGroup = transform.parent.gameObject;
    }

    public void RotateDangerousSide()
    {
        //0, 1, 2, 3 으로 바뀌고 3이면 0으로
        DangerousSide = (BlockDirection)(((int)DangerousSide + 1) % 4);
    }
    public void TakeDamage(int damage)
    {
        if (blockType == BlockTypeEnum.Cactus)
        {
            //animation 재생
            _blockShaderController.ChangeAnimation("Cactus_Hide");
        }

        if (_hp <= 0) return;
        _hp -= damage;


        if (blockType == BlockTypeEnum.Rainbow)
        {
            _blockShaderController.ChangeColor(_rainbowColorArray[_rainbowColorArray.Length - _hp - 1]);
            //hot pink
            Color comboColor = new Color32(255, 105, 180, 255);
            Color comboColor2 = new Color32(50, 255, 50, 255);
            if (_hp == 1) InGameManager.Instance.InGameShowStringText("한번 더!", transform.position + new Vector3(0f, -1f, 0f), comboColor2, 4, true);
            SoundManager.Instance.PlayRandomSound(1f, "Block 4", "Block 5", "Block 6");
            if (_hp <= 0)
            {
                InGameManager.Instance.InGameShowStringText("Good!", transform.position + new Vector3(0f, -2.5f, 1f), comboColor, 5, true);
                _isBomb = true;
                _blockShaderController.StartCoroutine(_blockShaderController.RainbowEmitMotion());
            }
        }
        else if (blockType == BlockTypeEnum.Black)
        {
            if (_hp <= 0)
            {
                BreakSelf(true);
            }
            else
            {
                _blockShaderController.ChangeSprite(Resources.Load<Sprite>("Sprites/Blocks/Block_Black_Broken"));
            }
        }
    }

    public void TurnOffSwitch()
    {
        if (blockType == BlockTypeEnum.Switch && _isSwitched == false)
        {
            _isSwitched = true;
            _blockShaderController.TurnOffSwitch();
            transform.parent.GetComponent<BlockGroup>().SwitchTouchEvent(this);
        }
    }

    public void TurnOffAllSwitch()
    {
        if (blockType == BlockTypeEnum.Switch && _isSwitched == false)
        {
            _isSwitched = true;
            transform.parent.GetComponent<BlockGroup>().AllSwitchTouchEvent();
        }
    }
    
    public void HandleOnOff(bool isOn = false)
    {
        if (blockType == BlockTypeEnum.OnOff)
        {
            _isOn = !_isOn;
            if (isOn) _isOn = true;
            _blockShaderController.OnOff(_isOn);
            if (_isOn)
            {
                SoundManager.Instance.PlaySound("Ui Success 19", .07f);
                InGameManager.Instance.PlayerController.ChangeTargetBlockType(BlockTypeEnum.OnOffInvisible, BlockTypeEnum.OnOffVisible, -1, 12f);
            }
            else
            {
                InGameManager.Instance.PlayerController.ChangeTargetBlockType(BlockTypeEnum.OnOffVisible, BlockTypeEnum.OnOffInvisible, -1, 12f);
            }
        }
    }

    public void BreakSelf(bool _isPlayer, bool isBreakInstantly = false)
    {
        if (breakCoroutine == null && gameObject.activeSelf)
        {
            breakCoroutine = StartCoroutine(BreakCoroutine(_isPlayer, isBreakInstantly));
            if (GameManager.Instance.CurrentGameState != GameState.InGame) return; //CutScene 때 오류 방지용
            if (InGameManager.Instance.UpgradeCategories[UpgradeCategory.ScoreBreakAllBlock] > 0)
            {
                blockGroup.GetComponent<BlockGroup>().HandleAllBlockBreakEvent();
            }
            blockGroup.GetComponent<BlockGroup>().HandleCantJumpUpSituation();
            blockGroup.GetComponent<BlockGroup>().HandleCheckAllIceBlock();
        }
    }
    
    public void BombSelf()
    {
        if (_isBomb) return;
        if (breakCoroutine == null)
        {
            switch (blockType)
            {
                case BlockTypeEnum.LandscapeBomb:
                    var parent = transform.parent;

                    bool gainEnergyFlag =
                        InGameManager.Instance.UpgradeCategories[UpgradeCategory.EnergyGainBombBlock] > 0;
                    if (gainEnergyFlag)
                    {
                        InGameManager.AddGuage(10 * (int)InGameManager.Instance.UpgradeCategories[UpgradeCategory.EnergyGainBombBlock]);
                    }

                    for (int i = 0; i < parent.childCount; i++)
                    {
                        var child = parent.GetChild(i);
                        if (child != transform) // 자신이 아닌 경우에만 실행
                        {
                            var breakableBlock = child.GetComponent<BreakableBlock>();
                            if (breakableBlock != null)
                            {
                                breakableBlock.BreakSelf(false);
                                InGameManager.AddScore(Statics.BREAK_BLOCK_SCORE, breakableBlock.transform.position);
                                // if (InGameManager.Instance.PlayerController.IsBlockBreakEnergy && !gainEnergyFlag)
                                // {
                                //     InGameManager.AddGuage(1);
                                // }
                            }
                        }
                    }

                    _isBomb = true;
                    InGameManager.Instance.DailyMissionVariables.IncrementMission(DailyMissionType.BreakBombBlock);
                    break;
                case BlockTypeEnum.PortraitBomb:
                    var portraitBreakPoints = new List<Vector2>
                    {
                        new Vector2(0, 1),
                        new Vector2(0, 2),
                        new Vector2(0, -1),
                        new Vector2(0, -2),
                        new Vector2(0, -3)
                    };
                    foreach (var point in portraitBreakPoints)
                    {
                        var center = (Vector2)transform.position + point * Statics.SPAWN_HEIGHT_OFFSET;
                        var colliders = Physics2D.OverlapCircleAll(center, 0.4f, LayerMask.GetMask("Ground"));
                        foreach (var collider in colliders)
                        {
                            //if (collider.gameObject != gameObject) // 자기 자신이 아닌 경우에만 실행
                            {
                                var breakableBlock = collider.GetComponent<BreakableBlock>();
                                if (breakableBlock != null)
                                {
                                    breakableBlock.BreakSelf(false);
                                }
                            }
                        }
                    }

                    _isBomb = true;
                    break;
            }

            SoundManager.Instance.PlaySound("Explosion 39", .25f);
        }
    }
    public void CheckEnemyUpSide()
    {
        // 콜라이더 영역 설정
        Vector2 center = (Vector2)transform.position + Vector2.up * 0.5f; // 현재 위치에서 위로 0.5만큼 이동한 위치를 중심으로 설정
        Vector2 size = new Vector2(1.5f, 1f); // 정사각형 영역의 크기 설정

        // 콜라이더 영역에 있는 모든 적을 감지
        Collider2D[] colliders = Physics2D.OverlapBoxAll(center, size, 0f, LayerMask.GetMask("Enemy"));

        // 감지된 모든 적에게 데미지 적용
        foreach (Collider2D collider in colliders)
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                if (enemy.EnemyType == EnemyType.Goomba && GameManager.Instance.CurrentAnimal != Animal.Bat)
                {
                    enemy.TakeDamage(1);
                }
            }
        }
    }

    public void DestroySelfAndSideBlocks(int deleteNum)
    {
        if (deleteNum > 0)
        {
            var colliders = Physics2D.OverlapCircleAll(transform.position, 0.6f, LayerMask.GetMask("Ground"));
            foreach (var collider in colliders)
            {
                var breakableBlock = collider.GetComponent<BreakableBlock>();
                if (breakableBlock != null)
                {
                    breakableBlock.BreakSelf(false);
                    breakableBlock.DestroySelfAndSideBlocks(deleteNum - 1);
                }
            }
        }

        BreakSelf(false);
    }
    public void CheckParentUnBreakable()
    {
        transform.parent.GetComponent<BlockGroup>().CheckUnBreakable();
    }
    public void HandleFlicker()
    {
        if (blockType == BlockTypeEnum.FlickerOff)
        {
            blockType = BlockTypeEnum.FlickerOn;
            return;
        }
       if (blockType == BlockTypeEnum.FlickerOn)
        {
            blockType = BlockTypeEnum.FlickerOff;
        }
    }

    private IEnumerator BreakCoroutine(bool _isPlayer, bool isBreakInstantly = false)
    {
        if (!isBreakInstantly) yield return new WaitForSeconds(breakDelay);

        UpdateDailyMissionVariables();
        HandleSpecialEffects();
        CreateDebris();

        if (_isPlayer)
        {
            HandlePlayerBreak(isBreakInstantly);
            PlayBreakSound();
        }
        
        _break?.Invoke();
        breakCoroutine = null;
    }

    private void UpdateDailyMissionVariables()
    {
        switch (blockType)
        {
            case BlockTypeEnum.Rainbow:
                InGameManager.Instance.DailyMissionVariables.IncrementMission(DailyMissionType.BreakRainbowBlock);
                break;
            case BlockTypeEnum.Star:
                InGameManager.Instance.DailyMissionVariables.IncrementMission(DailyMissionType.BreakStarBlock);
                break;
            case BlockTypeEnum.WingPotion:
                InGameManager.Instance.DailyMissionVariables.IncrementMission(DailyMissionType.BreakWingPotionBlock);
                break;
            case BlockTypeEnum.Ice:
                InGameManager.Instance.DailyMissionVariables.IncrementMission(DailyMissionType.BreakIceBlock);
                break;
            case BlockTypeEnum.ChangeDirection:
                InGameManager.Instance.DailyMissionVariables.IncrementMission(DailyMissionType.BreakChangeDirectionBlock);
                break;
        }
    }

    private void HandleSpecialEffects()
    {
        if (blockType == BlockTypeEnum.Leaf || blockType == BlockTypeEnum.Bamboo)
        {
            string particleName = blockType == BlockTypeEnum.Leaf ? "VFX_Leaf" : "VFX_Bamboo";
            CreateParticleEffect(particleName);
        }

        if (blockType == BlockTypeEnum.Rainbow)
        {
            InGameManager.AddScore(Statics.RAINBOW_BLOCK_SCORE, transform.position);
        }

        if ((blockType == BlockTypeEnum.Spike || blockType == BlockTypeEnum.RotatingSpike) &&
            InGameManager.Instance.BackgroundController.CurrentTheme == Theme.Industrial)
        {
            CreateParticleEffect("VFX_Poison");
        }
    }

    private void CreateParticleEffect(string particleName)
    {
        GameObject particleObject = Instantiate(Resources.Load<GameObject>($"Prefabs/Particles/{particleName}"));
        particleObject.transform.position = transform.position;
        Destroy(particleObject, 5f);
    }

    private void CreateDebris()
    {
        GameObject debris = Instantiate(Resources.Load<GameObject>("Prefabs/BlockDebris"), transform.position, Quaternion.identity);
        debris.TryGetComponent<BlockDebris>(out var blockDebris);
        blockDebris.Init(blockColor, blockType, _debrisSize);

        if (blockType == BlockTypeEnum.Cactus)
        {
            GameObject cactus = Instantiate(Resources.Load<GameObject>("Prefabs/Particles/Cactus"), transform.position, Quaternion.identity);
            cactus.GetComponent<Cactus>().Init();
            Destroy(cactus, 2f);
        }
    }

    private void HandlePlayerBreak(bool isBreakInstantly)
    {
        if (InGameManager.Instance.PlayerController.IsBlockBreakEnergy && !isBreakInstantly)
        {
            int addValue = (blockType == BlockTypeEnum.Star) ? 5 : (blockType == BlockTypeEnum.Leaf || blockType == BlockTypeEnum.Bamboo) ? 10 : 1;
            InGameManager.AddGuage(addValue);
            InGameManager.Instance.PlayerController.BreakBlockCount++;
            InGameManager.Instance.GetStageInfoAndChangeConditionUI(StageCondition.BreakBlockCount, InGameManager.Instance.PlayerController.BreakBlockCount);
        }

        AddScore();
    }

    private void AddScore()
    {
        int score = Statics.BREAK_BLOCK_SCORE;
        bool isSpecialScore = false;

        switch (blockType)
        {
            case BlockTypeEnum.Star:
                score *= 5;
                if (InGameManager.Instance.UpgradeCategories[UpgradeCategory.ScoreStarBlock] > 0)
                {
                    score = (int)Mathf.Round(score * (1 + InGameManager.Instance.UpgradeCategories[UpgradeCategory.ScoreStarBlock] * 0.01f));
                    isSpecialScore = true;
                }
                break;
            case BlockTypeEnum.Ice:
                if (GameManager.Instance.CurrentAnimal == Animal.HarpSeal)
                {
                    score *= 5;
                    isSpecialScore = true;
                }
                break;
            case BlockTypeEnum.Leaf:
            case BlockTypeEnum.Bamboo:
                score = 300;
                isSpecialScore = true;
                break;
        }

        InGameManager.AddScore(score, transform.position, isSpecialScore);
    }

    private void PlayBreakSound()
    {
        switch (blockType)
        {
            case BlockTypeEnum.Brown:
                SoundManager.Instance.PlayRandomSound(1f, "Arrow Impact wood 1", "Arrow Impact wood 2", "Arrow Impact wood 3");
                SoundManager.Instance.PlayRandomSound(1f, "Wooden item Breaks", "Wooden item Breaks 5", "Wooden item Breaks 6");
                break;
            case BlockTypeEnum.Yellow:
                SoundManager.Instance.PlaySound("Break_Yellow");
                break;
            case BlockTypeEnum.Ice:
                SoundManager.Instance.PlayRandomSound(.6f, "Glass Item Breaks", "Glass Item Breaks 2", "Glass Item Breaks 5");
                break;
            case BlockTypeEnum.Spike:
            case BlockTypeEnum.RotatingSpike:
                SoundManager.Instance.PlaySound("Brick");
                SoundManager.Instance.PlaySound("Block 8");
                break;
            case BlockTypeEnum.Star:
                SoundManager.Instance.PlaySound("Break_Yellow", .6f);
                SoundManager.Instance.PlaySound("Special Click Sound 10", .5f);
                SoundManager.Instance.PlaySound("Power up 7", .3f);
                break;
            case BlockTypeEnum.ChangeDirection:
                SoundManager.Instance.PlaySound("Break_ChangeDirection", .6f);
                break;
            case BlockTypeEnum.LandscapeBomb:
                SoundManager.Instance.PlaySound("Break_Bomb", .5f);
                break;
            case BlockTypeEnum.PortraitBomb:
                SoundManager.Instance.PlaySound("Break_Bomb", .5f);
                break;
            case BlockTypeEnum.Switch:
                SoundManager.Instance.PlayRandomSound(1f, "Block 1", "Block 8", "Block 9", "Break_Brown_3");
                break;
            case BlockTypeEnum.Spring:
                SoundManager.Instance.PlaySound("Game Punch (12)", .7f);
                SoundManager.Instance.PlaySound("Block 9");
                break;
            case BlockTypeEnum.Rainbow:
                SoundManager.Instance.PlayRandomSound(1f, "Explosion Tiny 4", "Explosion Tiny 5");
                SoundManager.Instance.PlaySound("Coins 2");
                SoundManager.Instance.PlaySound("Coins 10");
                GameObject vfxRainbow = InGameManager.Instance.CreatePrefab("Particles/VFX_Hit_Rainbow", transform.position);
                Destroy(vfxRainbow, 2f);
                break;
            case BlockTypeEnum.WingPotion:
                SoundManager.Instance.PlaySound("Power up 7", .3f);
                SoundManager.Instance.PlaySound("Break_Yellow", .6f);
                SoundManager.Instance.PlaySound("Glass 4", .3f);
                SoundManager.Instance.PlaySound("pop-6");
                break;
            case BlockTypeEnum.Bamboo:
                SoundManager.Instance.PlaySound("Wood hit Light 7", .5f);
                SoundManager.Instance.PlaySound("Wood Impact Hard surface 7", .7f);
                break;
            case BlockTypeEnum.Leaf:
                SoundManager.Instance.PlaySound("Wooden Item Breaks 3", .5f);
                SoundManager.Instance.PlayRandomSound(1f, "Grass 1", "Grass 6", "Grass 7");
                break;
            case BlockTypeEnum.Cactus:
                SoundManager.Instance.PlaySound("Wooden item Breaks 5");
                SoundManager.Instance.PlaySound("Arrow Impact flesh (human) 6");
                break;
        }
    }

    public void BreakImmediately()
    {
        _break?.Invoke();
    }

    public void ModifyColliderSize(float targetSize)
    {
        BoxCollider2D boxCollider2D = transform.GetComponent<BoxCollider2D>();
        boxCollider2D.size = new Vector2(targetSize, 1f);
    }
    
    public void ChangeTrigger(bool isTrigger)
    {
        BoxCollider2D boxCollider2D = transform.GetComponent<BoxCollider2D>();
        boxCollider2D.isTrigger = isTrigger;
    }

    public void RendererOff()
    {
        GetComponent<Collider2D>().enabled = false;
        if (GetComponentInChildren<SpriteRenderer>() != null)
        {
            GetComponentInChildren<SpriteRenderer>().gameObject.SetActive(false);
        }
        
        if (GetComponent<BlockType>().Breakable)
        {
            GetComponent<BlockType>().CanJumpUp = true;
        }
        
        _isBroken = true;
    }
    
    public void RendererOn()
    {
        GetComponent<Collider2D>().enabled = true;
        transform.GetChild(0).gameObject.SetActive(true);
        if (GetComponent<BlockType>().Breakable)
        {
            GetComponent<BlockType>().CanJumpUp = false;
        }
        _isBroken = false;
    }
    
    public void SwitchRendererOn()
    {
        GetComponent<Collider2D>().enabled = true;
        
        transform.GetChild(0).gameObject.SetActive(true);
        
        _isBroken = false;
    }
    
    public void MoveBlockBetweenTarget(Vector2 startPos, Vector2 targetPos, float duration)
    {
        StartCoroutine(MoveBlockBetweenTargetCoroutine(startPos, targetPos, duration));
    }
    
    private IEnumerator MoveBlockBetweenTargetCoroutine(Vector2 startPos, Vector2 targetPos, float duration)
    {
        float elapsedTime = 0f;
    
        while (true) // 무한 루프로 계속 왔다갔다 함
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.PingPong(elapsedTime / duration, 1f);
        
            transform.position = Vector2.Lerp(startPos, targetPos, t);
        
            yield return null;
        }
    }

    public void OnDisable()
    {
        SwitchRendererOn();
    }
}

public enum BlockDirection
{
    None = -1,
    Down,
    Left,
    Up,
    Right
}