using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public abstract class ItemBase : MonoBehaviour
{
    private ItemType _itemType;
    public ItemType ItemType
    {
        get => _itemType;
        set => _itemType = value;
    }

    private bool nonce = true;
    [SerializeField] private int playerLayer;

    public float moveDistance = .5f; // 움직일 거리
    public float moveDuration = 1f; // 움직일 시간
    public Ease moveEaseType = Ease.Linear; // 이징 타입 (선택사항)
    public float flipDuration = 1f; // 플립 시간
    public Ease flipEaseType = Ease.InOutCubic; // 이징 타입 (선택사항)
    public float rotationAmount = 180f; // 회전 각도
    private bool _isRotate = true;
    public bool IsRotate
    {
        get => _isRotate;
        set => _isRotate = value;
    }

    private Vector3 initialPosition;
    private bool movingUp = true;

    public virtual void Init()
    {
        // Cache the player layer
        playerLayer = LayerMask.NameToLayer("Player");
        // 겹치지 않도록 위치 조정
        EnsureNoOverlap();
        nonce = true;
    }

    private void EnsureNoOverlap()
    {
        const float overlapRadius = 0.5f; // 체크할 반경
        const float maxCheckDistance = 4.0f; // 최대 체크 거리
        const float stepDistance = 0.5f; // 한번에 움직일 거리

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, overlapRadius);
        foreach (var collider in colliders)
        {
            if (collider.gameObject != gameObject && collider.TryGetComponent(out ItemBase itemBase))
            {
                Vector2 direction = (transform.position.x < 0f) ? Vector2.right : Vector2.left;
                float checkDistance = stepDistance;

                while (checkDistance < maxCheckDistance)
                {
                    Vector3 newPosition = transform.position + (Vector3)(direction * checkDistance);
                    colliders = Physics2D.OverlapCircleAll(newPosition, overlapRadius);

                    bool isOverlapping = false;
                    foreach (var col in colliders)
                    {
                        if (col.gameObject != gameObject && col.TryGetComponent(out ItemBase _))
                        {
                            isOverlapping = true;
                            break;
                        }
                    }

                    if (!isOverlapping)
                    {
                        transform.position = newPosition;
                        return;
                    }
                    
                    checkDistance += stepDistance;
                }

                // 모든 시도가 실패했을 경우 원래 위치에 남아있게 됩니다.
                Debug.LogWarning("Failed to find non-overlapping position for ItemBase.");
            }
        }
    }
    
    void Start()
    {
        initialPosition = transform.position;
        MoveUpDown();
        FlipUpDown();
        
        // SpriteRenderer spriteRenderer = transform.GetChild(2).GetComponent<SpriteRenderer>();
        // spriteRenderer.sprite = Resources.Load<Sprite>($"Sprites/Items/Item_{_itemType.ToString()}");
    }

    void MoveUpDown()
    {
        // 위로 움직이기
        if (movingUp)
        {
            transform.DOMoveY(initialPosition.y + moveDistance, moveDuration)
                .SetEase(moveEaseType)
                .SetLoops(-1, LoopType.Yoyo); // 위아래 반복
        }
        // 아래로 움직이기
        else
        {
            transform.DOMoveY(initialPosition.y, moveDuration)
                .SetEase(moveEaseType)
                .SetLoops(-1, LoopType.Yoyo); // 위아래 반복
        }
    }

    void FlipUpDown()
    {
        if (!_isRotate) return;
        // 회전 애니메이션
        Sequence flipSequence = DOTween.Sequence();
        // 처음 회전
        flipSequence.Append(transform.DORotate(new Vector3(0f, rotationAmount, 0f), flipDuration)
            .SetEase(flipEaseType));
        // 1초 대기
        flipSequence.AppendInterval(1f);
        // 다음 회전
        flipSequence.Append(transform.DORotate(new Vector3(0f, -rotationAmount, 0f), flipDuration)
            .SetEase(flipEaseType));
        // 반복 설정
        flipSequence.SetLoops(-1, LoopType.Yoyo); // 회전 반복
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (nonce && other.gameObject.layer == playerLayer)
        {
            OnItemPickup();
            nonce = false;
        }
    }

    public virtual void OnItemPickup()
    {
        PlayerPrefsManager.Instance.MediumVibrate();
        Destroy(gameObject);
    }
}

public enum ItemType
{
    None,
    Rocket,
    WingPotion,
}
