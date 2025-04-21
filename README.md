# 애니멀 점핑!
`Unity` · `Mobile` · `Casual` · `Optimization` · `Gacha`

## 🎮 프로젝트 개요
<p align="left">
  <img src="https://github.com/user-attachments/assets/c2c46ec6-770c-46db-8af2-b604e30742c5" width="400"/>
</p>

- `애니멀 점핑!`은 동물 캐릭터가 위로 점프하며 장애물을 피하는 원터치 캐주얼 액션 게임입니다.
- Unity 기반으로 개발되었으며, 가볍고 직관적인 조작감 속에서도 전략적인 판단력을 요구하는 게임성을 추구했습니다.

### 📲 다운로드

<a href="https://play.google.com/store/apps/details?id=com.Badarang.AnimalJumping&hl=ko" target="_blank">
  <img src="https://upload.wikimedia.org/wikipedia/commons/7/78/Google_Play_Store_badge_EN.svg" width="180"/>
</a>
&nbsp;
<a href="https://apps.apple.com/kr/app/%EC%95%A0%EB%8B%88%EB%A9%80-%EC%A0%90%ED%95%91/id6590631455" target="_blank">
  <img src="https://developer.apple.com/assets/elements/badges/download-on-the-app-store.svg" width="160"/>
</a>

---

## 🎯 기획 의도
![Image](https://github.com/user-attachments/assets/5499b8a0-d951-4051-a9af-dabfb6edaf56)
![Image](https://github.com/user-attachments/assets/9eaca73c-c6c9-42db-82bd-f26fc5f4c062)

- 본 게임은 `화면 상단 두 칸만 보이는 제한된 시야`를 통해, 플레이어가 순간적인 판단으로 다음 경로를 결정하게끔 유도합니다.
- 폭탄, 레이저 등 다양한 위협 요소가 조합되어 `순간 판단력과 반사신경`을 시험합니다.  
- 이러한 구조는 반복 플레이에서도 `긴장감과 몰입감`을 유지시킵니다.

---

## 𝒇 주요 기능

### 🧱 Block 생성 최적화

#### 문제 상황
- 초기 구현은 랜덤 + 풀링 방식이었습니다.
- 이후 기획에 따라 특정 패턴을 가진 프리팹(BlockGroupPrefab) 을 도입했지만,
- 해당 프리팹은 기존 풀링 시스템과 충돌하며 GC Alloc에 의한 성능 스파이크가 발생했습니다.
#### 해결 방향
- 모든 프리팹을 풀링하는 것은 관리 복잡도와 메모리 이슈로 적절하지 않다고 판단했습니다.
- 대신, 프리팹을 구성 단위로 분리하고 클래스 단위로 캐싱하여 재활용성을 확보했습니다.
```csharp
[Serializable] public class BlockGroupPrefabInfo
{
    public List<BlockGroupInfo> BlockGroupInfos;
    public List<OtherObject> OtherObjectInfos;
}

[Serializable] public class BlockGroupInfo
{
    public bool IsRandom;
    public List<BlockTypeEnum> ExcludedBlockTypes;
    public int[] BlockGroupTypeArray;
    public List<GameObject> Lasers;
}

[Serializable] public class OtherObject
{
    public GameObject Prefab;
    public Vector2 Position;
}
```

이 구조를 통해,

- 레이저 / 아이템 / 블록 그룹을 개별적으로 분리하고
- 미리 캐싱된 풀 객체(BlockGroup)에 데이터를 바인딩해 조립식으로 생성함으로써
- GC를 줄이고(202KB → 68KB), 동적 생성의 오버헤드를 최소화했습니다.

---

### 🗺️ 맵 생성 개선
기존의 단순 랜덤 생성 대신, 높이 기반 + 확률 기반 프리팹 스폰 시스템을 도입했습니다.
```csharp
public GameObject GetSpawnableBlockGroupPrefabWithHeight(float curHeight)
{
    reusableSpawnableEntries.Clear();

    foreach (var folder in blockGroupFolders)
    foreach (var entry in folder.entries)
    {
        if (IsHeightInRange(curHeight, entry) && !recentPrefabs.Contains(entry.prefab))
            reusableSpawnableEntries.Add(entry);
    }

    // 확률 기반 선택
    var totalWeight = reusableSpawnableEntries.Sum(e => (int)e.probability);
    var randomWeight = Random.Range(0, totalWeight);
    int accumulatedWeight = 0;

    foreach (var entry in reusableSpawnableEntries)
    {
        accumulatedWeight += (int)entry.probability;
        if (randomWeight < accumulatedWeight)
        {
            UpdateRecentPrefabs(entry.prefab);
            return entry.prefab;
        }
    }

    return recentPrefabs.Count > 0 ? recentPrefabs.Peek() : null;
}
```
- 기획된 구조 + 랜덤성이 균형을 이루도록 설계
- 최근 사용된 프리팹은 제외하여 다양한 패턴 유도
- ScriptableObject 기반 구조로 협업 시에도 디자인 직관성 확보

---

### 💎 가챠 시스템 트랜잭션
- 가챠는 단순한 연출보다 재화를 소모하는 순간 결과가 보장되어야 하는 Atomic 트랜잭션 구조로 설계되었습니다.
- 재화 차감 → 결과 생성 → 저장 → 연출 흐름이 끊기지 않고 안정적으로 이어지도록 구성했습니다.

#### 🎲 Gacha 시스템 구조

| 단계 | 역할 |
|------|------|
| `TryStartGachaTransaction()` | 재화 차감 및 결과 생성 / 실패 시 중단 |
| `InitGachaVisuals()` | 결과 데이터를 기반으로 연출용 구슬 생성 |
| `GachaPonCO()` | 사운드 + DOTween 연출 → 결과 화면 출력 |
| `ResetGachaGame()` | 초기화 및 상태 복원 |

#### 🔒 트랜잭션 설계 포인트

```csharp
if (!GameManager.Instance.CanSpendCurrency(numOfGachaBalls))
    return false;

GameManager.Instance.SpendCurrency(numOfGachaBalls);
```
- 재화가 부족하면 즉시 중단
- SpendCurrency() 이후 결과 생성 및 서버 저장까지 한 번에 수행
- 트랜잭션 실패 없이 항상 Atomic하게 보상이 보장됨
- SaveDataToServer()를 통해 서버 데이터 일관성 유지

---

## 📚 기술 학습 및 공유

현재 구조는 `DOTS(Entity 기반 구조)`로 전환 가능하도록 준비하고 있으며,  
- `BlockGroup`만 ECS로 관리하여 Spawn 최적화  
- `Job System`을 도입해 병렬 처리 적용 예정  
- 궁극적으로 `GC-Free한 퍼포먼스 기반 아키텍처`를 실현하는 것을 목표로 하고 있습니다.

관련하여 학습한 내용을 정리하고 있으며,  
스터디 파티원들과 아래 노션 페이지를 통해 공유하고 있습니다:

<a href="https://badarang.notion.site/Unity-DOTS-1d94124737e3802fbc9fe48d730a6280?pvs=74" target="_blank" style="text-decoration: none; color: inherit;">
  <table>
    <tr>
      <td width="150">
        <img src="https://github.com/user-attachments/assets/ae43c57b-c16c-41b3-a13c-d9f0b9933f29" width="140">
      </td>
      <td valign="middle">
        <strong>🔗 Unity DOTS 학습 정리 (Notion)</strong><br>
        - Unity DOTS의 핵심 개념을 정리한 문서입니다.<br>
        - ECS, 캐시 메모리, Job System, Query 구조 등을 설명합니다.
      </td>
    </tr>
  </table>
</a>
