using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BlockShaderController : MonoBehaviour
{
    private static Dictionary<int, Material> materialPool = new Dictionary<int, Material>();
    private MaterialPropertyBlock propBlock;
    
    [SerializeField] private BlockType _blockTypeScript;
    [SerializeField] private BreakableBlock breakableBlock;
    [SerializeField] private GameObject rendererObject;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator _animator;
    [SerializeField] private GameObject _bombParticle;
    private BlockGroup parentGroup;
    private const float disableDistance = 15f;

    private bool isShining = false;
    private bool hasShineProperty = false;

    private WaitForSeconds wait1MiliSec = new WaitForSeconds(.1f);
    private WaitForSeconds wait2MiliSec = new WaitForSeconds(.2f);
    private WaitForSeconds rotateSec = new WaitForSeconds(1f);
    private WaitForSeconds pointFiveSec = new WaitForSeconds(.5f);
    private WaitForSeconds pointEightSec = new WaitForSeconds(.8f);
    private float lastMaterialUpdateTime = 0;
    private float materialUpdateInterval = 0.05f;
    private Coroutine rainbowEffectCO;
    private Coroutine bombMotionCoroutine;
    
    private BlockTypeEnum blockType;
    private float shineDelay = 0;
    private bool isRainbowShift = false;

    // Dotween variables
    private Vector3 originalScale = Vector3.one;
    private Vector3 targetScale = Vector3.one * 1.3f;
    private Coroutine flickerCoroutine;
    private bool isFlickerOn;
    
    //Rainbow
    private float _hsvShift = 40;
    
    private float updateTimer = 0f;
    private const float UPDATE_INTERVAL = 0.1f;

    private void OnDisable()
    {
        StopAllCoroutines();
        isShining = false;
        ResetRotation();
    }

    private void Awake()
    {
        parentGroup = GetComponentInParent<BlockGroup>();
        propBlock = new MaterialPropertyBlock();
    }

    public void Init()
    {
        StopAllCoroutines();
        rendererObject.transform.DOKill();
        blockType = _blockTypeScript.BlockTypeValue;
        _animator.runtimeAnimatorController = null;
        
        hasShineProperty = (blockType == BlockTypeEnum.Ice);
        rendererObject.transform.localScale = Vector3.one;
        rendererObject.transform.rotation = Quaternion.identity;
        spriteRenderer.transform.localScale = Vector3.one;

        shineDelay = 0;
        isRainbowShift = false;
        ResetRotation();

        SetAnimatorController();
        SetInitialPropertyBlock();

        if (blockType == BlockTypeEnum.RotatingSpike)
        {
            StartCoroutine(RotateBlock());
        }
        else if (blockType == BlockTypeEnum.Conveyor)
        {
            StartCoroutine(ConveyorSound());
        }
        if (blockType == BlockTypeEnum.FlickerOn || blockType == BlockTypeEnum.FlickerOff)
        {
            isFlickerOn = blockType == BlockTypeEnum.FlickerOn;
            StartFlicker();
        }
    }
    
    void Start()
    {
        InitializeMaterial();
    }
    
    private void InitializeMaterial()
    {
        Material originalMaterial = spriteRenderer.sharedMaterial;
        int materialId = originalMaterial.GetInstanceID();

        if (!materialPool.TryGetValue(materialId, out Material material))
        {
            material = new Material(originalMaterial);
            material.enableInstancing = true;  // Enable GPU Instancing
            materialPool[materialId] = material;
        }

        spriteRenderer.sharedMaterial = material;
        SetInitialPropertyBlock();
    }

    private void SetInitialPropertyBlock()
    {
        GetPropertyBlockNullCheck();

        switch (blockType)
        {
            case BlockTypeEnum.Rainbow:
                propBlock.SetFloat("_HsvShift", 40f);
                break;
            case BlockTypeEnum.Ice:
                propBlock.SetFloat("_ShineLocation", -0.5f);
                break;
            case BlockTypeEnum.LandscapeBomb:
                if (bombMotionCoroutine != null)
                {
                    StopCoroutine(bombMotionCoroutine);
                }
                bombMotionCoroutine = StartCoroutine(RepeatBombMotion());
                break;
        }

        spriteRenderer.SetPropertyBlock(propBlock);
    }

    private void SetAnimatorController()
    {
        switch (blockType)
        {
            case BlockTypeEnum.Spring:
                _animator.runtimeAnimatorController = GameManager.Instance.BlockAnimatorCache[BlockTypeEnum.Spring];
                break;
            case BlockTypeEnum.Cactus:
                _animator.runtimeAnimatorController = GameManager.Instance.BlockAnimatorCache[BlockTypeEnum.Cactus];
                break;
            case BlockTypeEnum.OnOffInvisible:
                _animator.runtimeAnimatorController = GameManager.Instance.BlockAnimatorCache[BlockTypeEnum.OnOffInvisible];
                break;
            case BlockTypeEnum.Conveyor:
                _animator.runtimeAnimatorController = GameManager.Instance.BlockAnimatorCache[BlockTypeEnum.Conveyor];
                SetConveyorDirection();
                break;
        }
    }

    private void SetConveyorDirection()
    {
        if (transform.position.x == -2.5f)
        {
            _animator.SetTrigger("Left");
        }
        else if (transform.position.x == 2.5f)
        {
            _animator.SetTrigger("Right");
        }
    }

    public void ChangeAnimation(string animationName)
    {
        _animator.Play(animationName);
    }
    
    private void GetPropertyBlockNullCheck()
    {
        if (propBlock == null)
        {
            propBlock = new MaterialPropertyBlock();
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.GetPropertyBlock(propBlock);
        }
    }

    void Update()
    {
        if (InGameManager.Instance == null || InGameManager.Instance.Player == null)
        {
            return;
        }

        updateTimer += Time.deltaTime;
        if (updateTimer >= UPDATE_INTERVAL)
        {
            updateTimer = 0f;
            UpdateBlockProperties();
        }
    }
    
    void UpdateBlockProperties()
    {
        GetPropertyBlockNullCheck();
        bool propertyChanged = false;

        switch (blockType)
        {
            case BlockTypeEnum.Ice:
                propertyChanged = UpdateIceShine();
                break;
            case BlockTypeEnum.Rainbow:
                propertyChanged = UpdateRainbowShift();
                break;
            case BlockTypeEnum.LandscapeBomb:
                if (breakableBlock.IsBomb) HandleBombActivation();
                break;
        }

        if (propertyChanged)
        {
            spriteRenderer.SetPropertyBlock(propBlock);
        }
    }
    
    void HandleBombActivation()
    {
        if (_bombParticle.gameObject.activeSelf)
        {
            rendererObject.transform.DOKill();
            rendererObject.transform.localScale = targetScale;
            rendererObject.transform.DOScale(originalScale, 0.1f)
                .SetEase(Ease.InOutSine)
                .OnKill(() => rendererObject.transform.localScale = originalScale)
                .OnComplete(() =>
                {
                    _bombParticle.gameObject.SetActive(false);
                    enabled = false;
                });
        }
    }

    private bool UpdateIceShine()
    {
        if (!hasShineProperty) return false;
        
        if (isShining)
        {
            float shine = propBlock.GetFloat("_ShineLocation");
            shine += Time.deltaTime * 10f;
            if (shine >= 1.5f)
            {
                shine = -0.5f;
                isShining = false;
                shineDelay = .75f;
            }
            propBlock.SetFloat("_ShineLocation", shine);
            return true;
        }
        else
        {
            shineDelay -= Time.deltaTime * 10f;
            if (shineDelay <= 0)
            {
                isShining = true;
            }
            return false;
        }
    }

    private bool UpdateRainbowShift()
    {
        if (isRainbowShift)
        {
            float shift = propBlock.GetFloat("_HsvShift");
            shift += Time.deltaTime * 1000;
            propBlock.SetFloat("_HsvShift", shift);
            return true;
        }
        return false;
    }

    private IEnumerator RotateBlock()
    {
        while (!breakableBlock.IsBroken)
        {
            PlaySoundDistance("SMG Reload", .7f);
            PlaySoundDistance("Switch sounds 7", .7f);
            Sequence seq = DOTween.Sequence();
            seq.Append(rendererObject.transform.DORotate(new Vector3(0, 0, -90), 0.15f).SetRelative(true).SetEase(Ease.OutExpo));
            seq.InsertCallback(0.07f, breakableBlock.RotateDangerousSide);
            seq.Play();
            yield return rotateSec;
        }
    }

    private IEnumerator ConveyorSound()
    {
        while (!breakableBlock.IsBroken)
        {
            PlaySoundDistance("Staff Hitting (Flesh) 2", .2f);
            yield return pointFiveSec;
        }
    }
    
    public void ResetRotation()
    {
        StopCoroutine(RotateBlock());
        rendererObject.transform.rotation = Quaternion.identity;
        transform.localRotation = Quaternion.identity;
    }
    
    public void StartFlicker()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
        }
        flickerCoroutine = StartCoroutine(Flicker());
    }

    public void StopFlicker()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }
    }
    
    public void PlaySoundDistance(string soundName, float multiplier = 1f)
    {
        var distance = Vector3.Distance(InGameManager.Instance.Player.transform.position, transform.position);
        if (distance < 10f)
        {
            float volume = (1 - distance / 10f) * multiplier;
            SoundManager.Instance.PlaySound(soundName, volume);
        }
    }

    private IEnumerator Flicker()
    {
        rendererObject.transform.DOKill();
        rendererObject.transform.localScale = originalScale;

        while (!breakableBlock.IsBroken)
        {
            yield return rendererObject.transform.DOScale(targetScale, 0.1f)
                .SetEase(Ease.OutElastic)
                .WaitForCompletion();
            
            breakableBlock.HandleFlicker();
            isFlickerOn = !isFlickerOn;
            spriteRenderer.sprite = isFlickerOn ? GameManager.Instance.BlockSpriteCache[BlockTypeEnum.FlickerOn] : GameManager.Instance.BlockSpriteCache[BlockTypeEnum.FlickerOff];
            breakableBlock.ChangeTrigger(!isFlickerOn);
            
            PlaySoundDistance("Mining (Hitting Stone With Pickaxe) 4", .2f);
            PlaySoundDistance("Switch sounds 16", .2f);

            yield return rendererObject.transform.DOScale(originalScale, 0.1f)
                .SetEase(Ease.InOutQuad)
                .WaitForCompletion();

            yield return pointEightSec;
        }

        rendererObject.transform.localScale = originalScale;
    }

    private void ChangeBlockType(BlockTypeEnum newType)
    {
        blockType = newType;
        GetComponent<BlockType>().Init(newType);
        SetInitialPropertyBlock();
    }

    private IEnumerator RepeatBombMotion()
    {
        rendererObject.transform.DOKill();
        rendererObject.transform.localScale = originalScale;

        while (!breakableBlock.IsBomb)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Append(rendererObject.transform.DOScale(targetScale, 0.5f).SetEase(Ease.InOutSine))
                .Append(rendererObject.transform.DOScale(originalScale, 0.5f).SetEase(Ease.InOutSine))
                .AppendInterval(0.002f);  // wait2MiliSec

            yield return sequence.WaitForCompletion();
        }

        rendererObject.transform.localScale = originalScale;
    }
    
    public IEnumerator RainbowEmitMotion()
    {
        isRainbowShift = true;
        var i = 0;
        float duration = 0.05f;
        SoundManager.Instance.PlaySound("Time Bomb Short", .7f);
        while (i < 10)
        {
            yield return rendererObject.transform.DOScale(targetScale, duration).SetEase(Ease.InOutSine).WaitForCompletion();
            yield return rendererObject.transform.DOScale(originalScale, duration).SetEase(Ease.InOutSine).WaitForCompletion();
        
            duration -= 0.005f;
            if (blockType != BlockTypeEnum.Rainbow)
            {
                break;
            }
            yield return null;
            i++;
        }
    
        if (blockType == BlockTypeEnum.Rainbow)
        {
            breakableBlock.BreakSelf(true);
        }
    }

    public void TurnOffSwitch()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites/Blocks/Block_Switch");
        spriteRenderer.sprite = sprites[1];

        Material baseMaterial = Resources.Load<Material>("Materials/Block_Base");
        int materialId = baseMaterial.GetInstanceID();

        if (!materialPool.TryGetValue(materialId, out Material material))
        {
            material = new Material(baseMaterial);
            material.enableInstancing = true;  // Enable GPU Instancing
            materialPool[materialId] = material;
        }

        spriteRenderer.sharedMaterial = material;
        GetPropertyBlockNullCheck();
        spriteRenderer.SetPropertyBlock(propBlock);
    }
    
    public void OnOff(bool isOn)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites/Blocks/Block_OnOff");
        spriteRenderer.sprite = isOn ? sprites[1] : sprites[0];
        
        Material baseMaterial;
        if (isOn)
        {
            BlockSpawner.MaterialCache.TryGetValue("Switch", out baseMaterial);
        }
        else
        {
            BlockSpawner.MaterialCache.TryGetValue("Base", out baseMaterial);
        }

        if (baseMaterial != null)
        {
            int materialId = baseMaterial.GetInstanceID();
            if (!materialPool.TryGetValue(materialId, out Material material))
            {
                material = new Material(baseMaterial);
                material.enableInstancing = true;  // Enable GPU Instancing
                materialPool[materialId] = material;
            }
            spriteRenderer.sharedMaterial = material;
        }
        
        GetPropertyBlockNullCheck();
        spriteRenderer.SetPropertyBlock(propBlock);
    }

    public void ChangeColor(float hsvShift)
    {
        GetPropertyBlockNullCheck();
        propBlock.SetFloat("_HsvShift", hsvShift);
        spriteRenderer.SetPropertyBlock(propBlock);
    }

    public void ChangeSprite(Sprite _sprite)
    {
        spriteRenderer.sprite = _sprite;
    }
    
    public void PlaySpringAnimation()
    {
        if (blockType == BlockTypeEnum.Spring)
        {
            _animator.Play("SpringBlock_Jump");
        }
    }
}