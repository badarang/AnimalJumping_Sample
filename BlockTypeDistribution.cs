using Sirenix.OdinInspector;
using UnityEngine;

[System.Serializable] 
public class BlockTypeDistribution
{
    [Header("Block Section")]
    [LabelText("Block Type")] 
    public BlockTypeEnum blockType;

    [PropertySpace(SpaceBefore = 10)]
    [HideLabel]
    [ProgressBar(0, 100, ColorGetter = "ProbabilityColor", Height = 20)]
    public float probability;

    [LabelText("Time Range")] 
    [MinMaxSlider(0, 100, true)]
    public Vector2 timeRange;

    private Color ProbabilityColor => Color.HSVToRGB(probability / 100f, 1f, 1f);
    public BlockTypeDistribution()
    {
        probability = 1f; // 초기값 설정
        timeRange = new Vector2(0, 100);
    }
}