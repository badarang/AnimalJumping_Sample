using System.Collections;
using DG.Tweening;
using TarodevController;
using UnityEngine;

public class WaveController : MonoBehaviour
{
    private float _damageTime = 1.5f;
    private float _damageTimer = 1.5f;
    private Coroutine checkPlayerStayCoroutine;
    private bool isWaveTweenActive;
    private readonly float maxSpeed = 5f;
    private readonly WaitForSeconds waitSec = new(5f);
    private int waveAnimationCount;

    private Tween waveTween;
    public bool IsTouchingPlayer { get; private set; }

    public float AccelSpeed { get; private set; } = .05f;

    public float CurrentSpeed { set; get; } = 1f;

    private void Update()
    {
        if (GameManager.Instance.CurrentGameState != GameState.InGame)
        {
            //all coroutines will be stopped
            StopAllCoroutines();
            return;
        }

        if (!isWaveTweenActive) transform.position += Vector3.up * CurrentSpeed * Time.deltaTime;

        if (IsTouchingPlayer)
        {
            _damageTimer -= Time.deltaTime;
            if (_damageTimer <= 0 && !InGameManager.Instance.PlayerController.IsRocketLaunchReady &&
                !InGameManager.Instance.PlayerController.IsUsingItem)
            {
                _damageTimer = _damageTime;
                InGameManager.TakeDamage(1);
                if (InGameManager.Instance.Player.Hp <= 0) return;
                SoundManager.Instance.PlaySound("Water Bubbles 1");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.GetComponent<PlayerController>().IsGodMode) return;
            IsTouchingPlayer = true;
            other.GetComponent<PlayerController>().IsInWater = true;
            if (GameManager.Instance.CurrentAnimal == Animal.Fish ||
                GameManager.Instance.CurrentAnimal == Animal.Flamingo) InGameManager.Instance.ScoreMultiplier = 2f;
            _damageTimer = _damageTime;
            //if (checkPlayerStayCoroutine != null) StopCoroutine(checkPlayerStayCoroutine);
            //checkPlayerStayCoroutine = StartCoroutine(CheckPlayerStay(other));
        }
    }

    // private IEnumerator CheckPlayerStay(Collider2D other)
    // {
    //     yield return new WaitForSeconds(1f);
    //     if (isTouchingPlayer && !other.GetComponent<PlayerController>().IsRocketLaunchReady)
    //     {
    //         //InGameManager.Instance.GameOver(DeadType.Wave);
    //         InGameManager.TakeDamage(1);
    //         
    //     }
    // }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            if (other.gameObject.transform.position.y > transform.position.y)
            {
                if (other.gameObject.transform.position.y > transform.position.y + 2f)
                    other.GetComponent<PlayerController>().IsInWater = false;
                IsTouchingPlayer = false;
                if (GameManager.Instance.CurrentAnimal == Animal.Fish ||
                    GameManager.Instance.CurrentAnimal == Animal.Flamingo) InGameManager.Instance.ScoreMultiplier = 1f;
            }
    }

    public void SetDamageTimer(float damageTime)
    {
        _damageTime = damageTime;
        _damageTimer = damageTime;
    }

    public void Init()
    {
        StartCoroutine(AccelWave());
        StartCoroutine(WaveAnimation());
    }

    private IEnumerator AccelWave()
    {
        while (CurrentSpeed < maxSpeed)
        {
            CurrentSpeed += AccelSpeed;
            yield return waitSec;
        }
    }

    public float GetDistanceFromPlayer()
    {
        return InGameManager.Instance.Player.transform.position.y - transform.position.y;
    }

    public IEnumerator WaveAnimation(float moveAmount = 15f)
    {
        yield return new WaitForSeconds(10f);
        while (true)
        {
            isWaveTweenActive = true;
            var duration = 3f;
            var yDiff = GetDistanceFromPlayer();

            if (waveAnimationCount > 0 && waveAnimationCount % 10 == 0 && yDiff > 30f)
            {
                StartCoroutine(PreWarningFunction());
                yield return new WaitForSeconds(10f);
                InGameManager.Instance.StartCoroutine(InGameManager.Instance.WarningAnimation(.75f));
                AccelSpeed = .1f;
                transform.position = new Vector3(transform.position.x,
                    InGameManager.Instance.Player.transform.position.y - 30f, transform.position.z);
                var targetPos = InGameManager.Instance.Player.transform.position.y - moveAmount;
                duration = 6f;
                waveTween = transform.DOMoveY(targetPos, duration).SetEase(Ease.OutBounce);
            }
            else
            {
                var targetPos = transform.position.y + 5f;
                waveTween = transform.DOMoveY(targetPos, duration).SetEase(Ease.OutBounce);
            }

            yield return waveTween.WaitForCompletion();
            isWaveTweenActive = false;
            waveAnimationCount++;
            yield return new WaitForSeconds(10f);
        }
    }

    private IEnumerator PreWarningFunction()
    {
        var preWarningTime = 10f;
        var elapsedTime = 0f;

        UIManager.Instance.SwitchRainEffect(true);
        while (elapsedTime < preWarningTime)
        {
            yield return new WaitForSeconds(1f);
            elapsedTime += 1f;
        }
    }

    private IEnumerator TutorialWaveAnimation()
    {
        isWaveTweenActive = true;
        var duration = 2f;
        var targetPos = transform.position.y + 3f;
        waveTween = transform.DOMoveY(targetPos, duration).SetEase(Ease.OutBounce);
        yield return waveTween.WaitForCompletion();
        isWaveTweenActive = false;
    }

    public void TutorialAccelWave()
    {
        StartCoroutine(TutorialWaveAnimation());
        InGameManager.Instance.StartCoroutine(InGameManager.Instance.WarningAnimation(.75f));
    }
}