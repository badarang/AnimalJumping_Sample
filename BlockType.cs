using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BlockType : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer decorativeSpriteRenderer;
    [SerializeField] private BlockTypeEnum blockType;
    [SerializeField] private BoxCollider2D _canJumpUpCollider;
    [SerializeField] private SpriteRenderer _decorateRenderer;
    [SerializeField] private GameObject _shineParticle;
    [SerializeField] private GameObject _bombParticle;
    [SerializeField] private BoxCollider2D boxCollider2D;
    private BlockColorEnum blockColor;
    private float _debrisSize = .2f;
    public float DebrisSize => _debrisSize;
    
    public BlockTypeEnum BlockTypeValue
    {
        get => blockType;
        set => blockType = value;
    }

    public BlockColorEnum BlockColorValue
    {
        get => blockColor;
        set => blockColor = value;
    }

    private bool _canJumpUp = false;
    private bool _breakable;

    public bool Breakable
    {
        get => _breakable;
        set => _breakable = value;
    }

    public bool CanJumpUp
    {
        get => _canJumpUp;
        set
        {
            _canJumpUp = value;
            if (_canJumpUpCollider != null)
            {
                _canJumpUpCollider.enabled = value;
            }
        }
    }

   
    // private void OnValidate()
    // {
    //     if (spriteRenderer != null)
    //     {
    //         UpdateBlockSprite(true);
    //     }
    // }

    private void UpdateBlockSprite(bool isValidate = false)
    {
        spriteRenderer.sprite = GameManager.Instance.BlockSpriteCache[blockType];
        if (spriteRenderer.sprite == null)
        {
            Debug.LogWarning($"BlockType: {blockType} is not found in the cache");
            return;
        }
        if (!isValidate)
        {
            string spritePath = $"Sprites/Blocks/Block_{blockType}";
            if ((blockType == BlockTypeEnum.Spike || blockType == BlockTypeEnum.RotatingSpike) &&
                InGameManager.Instance != null && 
                InGameManager.Instance.BackgroundController.CurrentTheme == Theme.Industrial)
            {
                spritePath += "_Industrial";
            }
            spriteRenderer.sprite = Resources.Load<Sprite>(spritePath);
        }
        decorativeSpriteRenderer.enabled = (blockType == BlockTypeEnum.Spike || blockType == BlockTypeEnum.RotatingSpike);
    }
    
    void Start()
    {
        InitSettings();
    }

    private void InitSettings()
    {
        if (boxCollider2D == null)
        {
            boxCollider2D = GetComponent<BoxCollider2D>();
        }
        boxCollider2D.offset = new Vector2(0, 0);
        boxCollider2D.size = new Vector2(1f, 1);
    }


    public void Init(BlockTypeEnum _blockType)
    {
        blockType = _blockType;
        InitSettings();
        GetComponent<BlockShaderController>().Init();
        UpdateBlockSprite();
        
        TurnCollider();
        TurnCanJumpUp(false);

        // Reset common properties
        boxCollider2D.isTrigger = false;
        blockColor = BlockColorEnum.None;
        _debrisSize = 0.5f;
        _breakable = false;
        _decorateRenderer.enabled = false;
        _shineParticle.SetActive(false);
        _bombParticle.SetActive(false);

        switch (blockType)
        {
            case BlockTypeEnum.Empty:
                TurnCollider(false);
                _debrisSize = 0f;
                break;
            case BlockTypeEnum.Brown:
                blockColor = BlockColorEnum.Brown;
                _debrisSize = .25f;
                _breakable = true;
                break;
            case BlockTypeEnum.Yellow:
                blockColor = BlockColorEnum.Yellow;
                TurnCanJumpUp(true);
                _debrisSize = .5f;
                boxCollider2D.offset = new Vector2(0, .25f);
                boxCollider2D.size = new Vector2(1f, .5f);
                break;
            case BlockTypeEnum.OnOffInvisible:
            case BlockTypeEnum.FlickerOff:
                boxCollider2D.isTrigger = true;
                break;
            case BlockTypeEnum.Black:
            case BlockTypeEnum.BlackWall:
                blockColor = BlockColorEnum.Black;
                _debrisSize = .5f;
                break;
            case BlockTypeEnum.Star:
                _breakable = true;
                _debrisSize = .6f;
                _shineParticle.SetActive(true);
                break;
            case BlockTypeEnum.PortraitBomb:
                spriteRenderer.sortingOrder = 2;
                _debrisSize = .6f;
                _bombParticle.SetActive(true);
                break;
            case BlockTypeEnum.LandscapeBomb:
                spriteRenderer.sortingOrder = 1;
                _debrisSize = .6f;
                _bombParticle.SetActive(true);
                break;
            case BlockTypeEnum.ChangeDirection:
                _breakable = true;
                _debrisSize = .5f;
                break;
            case BlockTypeEnum.Ice:
            case BlockTypeEnum.Cloud:
                _debrisSize = .4f;
                _breakable = true;
                break;
            case BlockTypeEnum.Spike:
            case BlockTypeEnum.RotatingSpike:
                blockColor = BlockColorEnum.Spike;
                _decorateRenderer.enabled = true;
                break;
            case BlockTypeEnum.Rainbow:
                blockColor = BlockColorEnum.Rainbow;
                _shineParticle.SetActive(true);
                break;
            case BlockTypeEnum.Switch:
                blockColor = BlockColorEnum.Switch;
                _debrisSize = .5f;
                boxCollider2D.size = new Vector2(1.2f, 1);
                break;
            case BlockTypeEnum.Spring:
                blockColor = BlockColorEnum.Spring;
                _debrisSize = .7f;
                _breakable = true;
                break;
            case BlockTypeEnum.WingPotion:
                blockColor = BlockColorEnum.WingPotion;
                _breakable = true;
                _shineParticle.SetActive(true);
                break;
            case BlockTypeEnum.Leaf:
            case BlockTypeEnum.Bamboo:
                blockColor = BlockColorEnum.WingPotion;
                _breakable = true;
                _shineParticle.SetActive(true);
                break;
            case BlockTypeEnum.Cactus:
                blockColor = BlockColorEnum.None;
                _debrisSize = .5f;
                _breakable = true;
                break;
            case BlockTypeEnum.Sand:
                blockColor = BlockColorEnum.None;
                _debrisSize = .2f;
                _breakable = true;
                break;
            case BlockTypeEnum.OnOff:
            case BlockTypeEnum.OnOffVisible:
            case BlockTypeEnum.FlickerOn:
            case BlockTypeEnum.Moving:
            case BlockTypeEnum.Conveyor:
                blockColor = BlockColorEnum.None;
                _debrisSize = .5f;
                _breakable = false;
                break;
        }

        ApplyMaterial();

        if (TryGetComponent(out BreakableBlock breakableBlock))
        {
            breakableBlock.Init();
        }
    }

    public void ApplyMaterial()
    {
        // Apply material
        spriteRenderer.material = blockType switch
        {
            BlockTypeEnum.Empty or
                BlockTypeEnum.Brown or
                BlockTypeEnum.Yellow or
                BlockTypeEnum.Black or
                BlockTypeEnum.BlackWall or
                BlockTypeEnum.PortraitBomb or
                BlockTypeEnum.LandscapeBomb or
                BlockTypeEnum.ChangeDirection or
                BlockTypeEnum.Spike or
                BlockTypeEnum.RotatingSpike or
                BlockTypeEnum.Cactus or
                BlockTypeEnum.Sand or
                BlockTypeEnum.Spring or 
                BlockTypeEnum.OnOff or
                BlockTypeEnum.OnOffInvisible or
                BlockTypeEnum.OnOffVisible or
                BlockTypeEnum.Cloud or
                BlockTypeEnum.FlickerOff or
                BlockTypeEnum.FlickerOn or
                BlockTypeEnum.Moving => BlockSpawner.MaterialCache["Base"],
            BlockTypeEnum.Star or
                BlockTypeEnum.WingPotion or
                BlockTypeEnum.Leaf or
                BlockTypeEnum.Bamboo => BlockSpawner.MaterialCache["Star"],
            BlockTypeEnum.Ice => BlockSpawner.MaterialCache["Ice"],
            BlockTypeEnum.Rainbow => BlockSpawner.MaterialCache["Rainbow"],
            BlockTypeEnum.Switch => BlockSpawner.MaterialCache["Switch"],
            BlockTypeEnum.Conveyor => BlockSpawner.MaterialCache["Conveyor"],
            _ => spriteRenderer.material,
        };
    }

    private void TurnCanJumpUp(bool isEnabled = true)
    {
        boxCollider2D.usedByEffector = isEnabled;
        if (TryGetComponent(out PlatformEffector2D platformEffector2D))
        {
            platformEffector2D.enabled = isEnabled;
        }

        CanJumpUp = isEnabled;
    }

    private void TurnCollider(bool isEnabled = true)
    {
        boxCollider2D.enabled = isEnabled;
    }
}

public enum BlockTypeEnum
{
    None, //아무것도 없는 상태
    Brown,
    Yellow,
    Black,
    Star, 
    PortraitBomb, //5
    LandscapeBomb,
    ChangeDirection,
    Rainbow,
    Ice, 
    Switch, //10
    Spring,
    Spike,
    WingPotion,
    RotatingSpike,
    Leaf, //15
    Bamboo,
    Empty,
    Cactus,
    Sand, //19
    BlackWall,
    OnOff,
    OnOffInvisible,
    OnOffVisible, //23
    Cloud,
    FlickerOn,
    FlickerOff, //26
    Moving,
    Conveyor
}

public enum BlockColorEnum
{
    Brown,
    Yellow,
    Black,
    Star,
    Red, // PortraitBomb
    Blue, // LandscapeBomb
    Green, // ChangeDirection
    SkyBlue, // Ice
    Ice, 
    None, // For other types without specific color
    Spike,
    Rainbow,
    Switch,
    Spring,
    WingPotion,
}