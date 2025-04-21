using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class GachaGame : MonoBehaviour
{
    [SerializeField] private GameObject gachaPanel;
    [SerializeField] private GameObject gachaParticle;
    [SerializeField] private GameObject gachaPon;
    [SerializeField] private GameObject border;
    [SerializeField] private GameObject gachaBallContainer;
    [SerializeField] private GameObject hand;
    [SerializeField] private GachaHand gachaHand;
    [SerializeField] private GameObject borderDown;
    [SerializeField] private GameObject gachaBallObj;

    private readonly int numOfGachaBalls = 25;

    private bool canSkip;
    private Coroutine gachaAnimationCoroutine;
    private GameObject[] gachaBalls;
    private GachaResult[] gachaResults;
    private bool probabilityCorrectNeeded = true;

    [Button]
    public void StartGachaGame()
    {
        if (!TryStartGachaTransaction(out var results))
        {
            Debug.LogWarning("Not enough currency to start gacha.");
            return;
        }

        gachaResults = results;
        InitGachaVisuals(gachaResults);

        gachaPanel.SetActive(true);
        var anim = gachaPanel.GetComponent<UIAnimationBase>();
        anim.FadeIn(0.85f);
        anim.FadeInChildren(0.85f);

        if (gachaAnimationCoroutine != null) StopCoroutine(gachaAnimationCoroutine);
        gachaAnimationCoroutine = StartCoroutine(GachaPonCO());
    }

    private bool TryStartGachaTransaction(out GachaResult[] results)
    {
        results = null;
        var allUpgradeable = GameManager.Instance.GetAllUpgradeableAnimals();

        if (!GameManager.Instance.CanSpendCurrency(numOfGachaBalls))
            return false;

        GameManager.Instance.SpendCurrency(numOfGachaBalls);

        var selected = GameManager.Instance.GetRandomUpgradeableAnimals(allUpgradeable, numOfGachaBalls);
        results = GameManager.Instance.GetGachaResults(selected);

        foreach (var result in results)
        {
            GameManager.Instance.SaveDataToServer(result.Animal, result.Count);
        }

        GoogleMobileAdsManager.Instance.GachaResults = results;
        return true;
    }

    private void InitGachaVisuals(GachaResult[] results)
    {
        gachaBalls = new GameObject[results.Length];
        for (int i = 0; i < results.Length; i++)
        {
            var ball = Instantiate(gachaBallObj, gachaBallContainer.transform);
            ball.transform.localPosition = new Vector3(Random.Range(-1f, 1f), Random.Range(1f, 2f), 0);
            ball.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-180, 180));
            ball.transform.localScale = Vector3.one * 0.5f;

            if (ball.TryGetComponent(out GachaBall gachaBall))
            {
                gachaBall.Init(results[i].Animal, results[i].Grade);
                ball.SetActive(false);
            }

            gachaBalls[i] = ball;
        }
    }

    public IEnumerator GachaPonCO()
    {
        SoundManager.Instance.PlaySound("Get Magic Item", .5f);
        yield return new WaitForSeconds(.7f);

        gachaPon.SetActive(true);
        gachaPon.GetComponent<UIAnimationBase>().SlowJumpAppear();
        SoundManager.Instance.PlaySound("Cash register 2", .5f);
        yield return new WaitForSeconds(1.2f);

        canSkip = true;

        SoundManager.Instance.PlaySound("Item purchase 28", .6f);
        SoundManager.Instance.PlaySound("Drum roll 7", .3f);
        gachaParticle.SetActive(true);
        border.SetActive(true);

        foreach (var gachaBall in gachaBalls) gachaBall.SetActive(true);

        yield return new WaitForSeconds(2f);
        SoundManager.Instance.PlaySound("Lottery Balls Spin", .5f);

        for (var i = 0; i < 6; i++)
        {
            AddWindForce();
            var time = (i + 1) * .1f;
            yield return new WaitForSeconds(time);
        }

        SoundManager.Instance.PlaySound("Vending Machine Handle Twice");
        var sequence = DOTween.Sequence();
        sequence.Append(hand.transform.DORotate(new Vector3(0, 0, -50), .5f));
        sequence.Append(hand.transform.DORotate(new Vector3(0, 0, -10), .2f));
        sequence.Append(hand.transform.DORotate(new Vector3(0, 0, -100), .5f));
        sequence.Append(hand.transform.DORotate(new Vector3(0, 0, -70), .2f));
        sequence.Append(hand.transform.DORotate(new Vector3(0, 0, -150), .5f));
        sequence.Append(hand.transform.DORotate(new Vector3(0, 0, -120), .2f));
        sequence.Append(hand.transform.DORotate(new Vector3(0, 0, -200), .5f));
        sequence.Append(hand.transform.DORotate(new Vector3(0, 0, -180), .2f));
        sequence.Append(hand.transform.DORotate(new Vector3(0, 0, -250), .5f));
        sequence.Append(hand.transform.DORotate(new Vector3(0, 0, -235), .2f));
        sequence.Play();

        yield return new WaitForSeconds(3f);
        gachaPon.GetComponent<UIAnimationBase>().ShrinkOutQuart();
        yield return new WaitForSeconds(.2f);

        if (!gachaPanel.GetComponent<GachaPanelController>().ResultPanel.activeSelf)
            gachaPanel.GetComponent<GachaPanelController>().ShowResult();
    }

    public void SkipGachaAnimation()
    {
        if (gachaPanel.GetComponent<GachaPanelController>().ResultPanel.activeSelf) return;
        if (canSkip && gachaAnimationCoroutine != null)
        {
            StopCoroutine(gachaAnimationCoroutine);
            gachaPon.SetActive(false);
            canSkip = false;
            gachaPanel.GetComponent<GachaPanelController>().ShowResult();
        }
    }

    [Button]
    public void AddWindForce()
    {
        foreach (var gachaBall in gachaBalls)
        {
            if (gachaBall.transform.localPosition.y < .65f)
            {
                gachaBall.GetComponent<Rigidbody2D>()
                    .AddForce(new Vector2(Random.Range(-1f, 1f), Random.Range(0f, 1f)) * 500);
            }
        }
    }

    public void ResetGachaGame()
    {
        canSkip = false;
        probabilityCorrectNeeded = true;
        gachaPanel.SetActive(false);
        gachaPon.SetActive(false);
        gachaParticle.SetActive(false);
        border.SetActive(false);
        borderDown.SetActive(true);

        if (gachaBalls != null)
        {
            foreach (var gachaBall in gachaBalls)
            {
                if (gachaBall != null)
                    Destroy(gachaBall);
            }
        }
    }

    public GachaResult[] GetGachaResults() => gachaResults;
}

public class GachaResult
{
    public GachaResult(Animal animal, Grade grade, int count)
    {
        Animal = animal;
        Grade = grade;
        Count = count;
    }

    public Animal Animal { get; }
    public Grade Grade { get; }
    public int Count { get; }
}
