using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class GachaGame : MonoBehaviour
{
    [SerializeField]
    private GameObject gachaPanel;

    [SerializeField]
    private GameObject gachaParticle;

    [SerializeField]
    private GameObject gachaPon;

    [SerializeField]
    private GameObject border;

    [SerializeField]
    private GameObject gachaBallContainer;

    [SerializeField]
    private GameObject hand;

    [SerializeField]
    private GachaHand gachaHand; //Result에 달려있음

    [SerializeField]
    private GameObject borderDown;

    [SerializeField]
    private GameObject gachaBallObj;

    private readonly int numOfGachaBalls = 25;

    private bool canSkip;
    private Coroutine gachaAnimationCoroutine;
    private GameObject[] gachaBalls;
    private GachaResult[] gachaResults;
    private bool probabilityCorrectNeeded = true;

    [Button] public void StartGachaGame()
    {
        var allUpgradeableAnimals = GameManager.Instance.GetAllUpgradeableAnimals();
        var selectedAnimals = GameManager.Instance.GetRandomUpgradeableAnimals(allUpgradeableAnimals, numOfGachaBalls);
        gachaResults = GameManager.Instance.GetGachaResults(selectedAnimals);
        GoogleMobileAdsManager.Instance.GachaResults = gachaResults;

        foreach (var gachaResult in gachaResults)
            //Debug.Log(gachaResult.Animal + " " + gachaResult.Grade + " " + gachaResult.Count);
            GameManager.Instance.SaveDataToServer(gachaResult.Animal, gachaResult.Count);

        gachaBalls = new GameObject[selectedAnimals.Count];
        for (var i = 0; i < selectedAnimals.Count; i++)
        {
            gachaBalls[i] = Instantiate(gachaBallObj, gachaBallContainer.transform, gachaBallContainer.transform);
            //Randomize the position of the gacha balls
            gachaBalls[i].transform.localPosition = new Vector3(Random.Range(-1f, 1f), Random.Range(1f, 2f), 0);
            gachaBalls[i].transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-180, 180));
            gachaBalls[i].transform.localScale = new Vector3(.5f, .5f, .5f);

            if (gachaBalls[i].TryGetComponent(out GachaBall gachaBall))
            {
                gachaBall.Init(selectedAnimals[i], GameManager.Instance.GetAnimalGrade(selectedAnimals[i]));
                gachaBalls[i].SetActive(false);
            }
        }

        //CreateGachaBalls();
        gachaPanel.SetActive(true);
        gachaPanel.GetComponent<UIAnimationBase>().FadeIn(.85f);
        gachaPanel.GetComponent<UIAnimationBase>().FadeInChildren(.85f);
        if (gachaAnimationCoroutine != null) StopCoroutine(gachaAnimationCoroutine);
        gachaAnimationCoroutine = StartCoroutine(GachaPonCO());
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
        //All gacha balls appear
        foreach (var gachaBall in gachaBalls) gachaBall.SetActive(true);

        yield return new WaitForSeconds(2f);

        SoundManager.Instance.PlaySound("Lottery Balls Spin", .5f);

        for (var i = 0; i < 6; i++)
        {
            AddWindForce();
            var time = (i + 1) * .1f;
            yield return new WaitForSeconds(time);
        }

        //Rotate hand
        SoundManager.Instance.PlaySound("Vending Machine Handle Twice");
        var sequence = DOTween.Sequence();
        sequence.Append(hand.transform.DORotate(new Vector3(0, 0, -50), .5f));
        sequence.Append(hand.transform.DORotate(new Vector3(0, 0, -10), .2f));
        sequence.Append(hand.transform.DORotate(new Vector3(0, 0, -100), .5f));
        sequence.Append(hand.transform.DORotate(new Vector3(0, 0, -70), .2f));
        sequence.Append(hand.transform.DORotate(new Vector3(0, 0, -150), .5f));
        sequence.Append(hand.transform.DORotate(new Vector3(0, 0, -120), .2f));
        //borderDown.SetActive(false);
        sequence.Append(hand.transform.DORotate(new Vector3(0, 0, -200), .5f));
        sequence.Append(hand.transform.DORotate(new Vector3(0, 0, -180), .2f));
        sequence.Append(hand.transform.DORotate(new Vector3(0, 0, -250), .5f));
        sequence.Append(hand.transform.DORotate(new Vector3(0, 0, -235), .2f));
        sequence.Play();

        yield return new WaitForSeconds(3f);
        //gachaHand.SwitchTrigger(false);
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

    public GachaResult[] GetGachaResults()
    {
        return gachaResults;
    }

    [Button] public void AddWindForce()
    {
        foreach (var gachaBall in gachaBalls)
            if (gachaBall.transform.localPosition.y < .65f)
                gachaBall.GetComponent<Rigidbody2D>()
                    .AddForce(new Vector2(Random.Range(-1f, 1f), Random.Range(0f, 1f)) * 500);
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
        foreach (var gachaBall in gachaBalls) Destroy(gachaBall);
    }
}

public class GachaResult
{
    public GachaResult(Animal animal, Grade grade, int count)
    {
        Animal = animal;
        Grade = grade;
        Count = count;
    }

    public Animal Animal { get; set; }
    public Grade Grade { get; set; }
    public int Count { get; set; }
}