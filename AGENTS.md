# 프로젝트 지침

## 작업 전에 반드시 읽을 문서
작업을 시작하기 전에 아래 문서를 모두 읽고, 그 내용을 따라.
- docs/GDD.md : 기획서
- docs/CODE_STYLE.md : 코드 작성 방식
- docs/POOLING.md : 오브젝트 풀링 구조
- docs/WORKLOG_RULES.md : 작업 기록 규칙

## 핵심 규칙 (위 문서를 못 읽었더라도 반드시 지킬 것)
- Unity {유니티_버전}, URP 2D Renderer, 2D 종스크롤 탄막 게임이다.
- 싱글 플레이 게임이다. 네트워크 코드를 쓰지 마.
- 탄과 이펙트처럼 대량으로 생성되는 오브젝트는 반드시 풀링을 써.
- 판정선과 헤일로 크기는 코드의 반경 값으로 판정해. Light 2D는 그 값에 맞춰 그리기만 해.
- 기획서에 없는 기능·동작·규칙은 임의로 추가하거나 변경하지 마. 의견이나 개선 제안이 있으면 반드시 사용자에게 먼저 질문하고 명시적인 승인을 받은 뒤에만 반영해. 애매하면 작업 전에 질문해.
- 기획서의 "조정할 수치" 항목은 코드에 박지 말고 인스펙터에서 바꿀 수 있게 해.
- 내가 지시하지 않은 기존 파일이나 씬 오브젝트는 수정하지 마. 필요하면 먼저 물어봐.
- 임시로 넣은 값(테스트용 수치, 디버그 코드)은 코드에 // ##TEMP 주석으로 표시해.
- git commit, push, reset 등 저장소를 바꾸는 명령은 쓰지 마. 커밋은 내가 GitHub Desktop으로 한다. git status, git diff 같은 읽기 명령만 써.
- 작업이 끝나면 docs/WORKLOG_RULES.md 규칙대로 docs/worklog.md에 기록해.
- 코드 작업은 계획 제시 → 사용자 계획 승인 → 변경 예정 코드를 채팅에 제시 → 사용자 코드 검토·승인 → 프로젝트 파일 적용 → 사용자 검증 순서를 따른다. 계획 승인만으로 파일에 코드를 적용하지 않는다.
- 검증은 유저가 직접한다. 에이전트는 검증·테스트를 실행하지 않고, 어떤 테스트를 하면 되는지 목록만 나열해준다.
- 기능은 내가 지정한 단계 하나씩만 구현해. 다음 단계 기능이나 미래 확장용 구조를 미리 만들지 마.
- 단계가 끝나면 새로 만든 파일과 메서드마다 "무엇을 하고 누가 부르는지" 한 줄씩 정리해서 알려줘.

## 작업 번호 및 다른 대화 인계
- 작업 목록은 기능별로 번호를 붙인다. 큰 기능은 1, 2, 3으로, 하위 기능은 1-1, 1-2, 2-1처럼 표기한다.
- 다른 대화에서 작업을 이어갈 때는 이 문서와 필수 문서, docs/worklog.md의 최신 기록 및 현재 프로젝트 상태를 먼저 읽는다.
- 사용자가 작업 목록만 요청하면 번호 목록만 제시하고 코드를 수정하지 않는다.

### 현재 인계 상태 (2026-11-06 기준)
- 보상·스킬 재구현은 **1. 카드 데이터 / 2. 보상 선택·획득 / 3. 스탯 카드 7종 / 4. 독 카드 2종(4-6 합산 구조 포함)**까지 코드·데이터를 적용했다. 기존 기본 공격·빛 조각 풀링/회복/보유량은 유지했다. 상세 변경 및 사용자 확인 항목은 docs/worklog.md를 참고한다.
- **현재 우선 작업은 보스 구현**이다. 사용자가 스킬 구현을 독 카드 4단계까지 적용한 상태에서 잠시 두고, 짤보스2와 마지막 보스를 만들어 1스테이지 한 사이클을 먼저 진행하도록 방향을 변경했다.
- 보스 작업 번호 전체는 **docs/BOSS_IMPLEMENTATION_PLAN.md**에 저장했다. 다른 대화에서 보스 작업을 이어갈 때 이 문서도 반드시 읽는다. 기존 스킬 번호와 구분해 "보스 작업 1-1"처럼 부른다.
- 보스 작업은 **1-1~1-5 규칙 확정 / 기존 기능을 재사용한 2단계 전환 기반 / 3-1~3-9 코드·프리팹 적용**까지 진행했다. 3-3 이후 목록은 수정된 GDD에 맞춰 **3-4~3-20**으로 다시 번호를 매겼다. 2-1은 별도 공통 SO를 만들지 않고 기존 Inspector 설정을 유지하기로 결정했다.
- **다음 단위는 보스 작업 3-10의 남은 부분인 감지 종료 후 벽 1초 후퇴·풀 반환 연결**이다. 통로 공격 탄의 피격·퍼펙트는 기존 TestBullet 처리를 사용하며 사용자도 판정이 들어간다고 확인했다. 중복 구현하지 않는다. 3-10 후퇴 코드는 아직 제시·승인·적용되지 않았다.
- 최신 추가 변경은 **벽과 통로 탄의 좌우 거리를 코드 반경 계산과 분리한 wallDistance=2 / laneDistance=1.2**이다. 코드·프리팹 저장 완료이며 **이 최신 거리 변경과 직전 발사 간격 분리의 사용자 실행 검증은 아직 확인되지 않았다.**
- docs/BOSS_IMPLEMENTATION_PLAN.md는 작업 번호를 참조한다. 상단의 2026-11-05 "보스 작업 시작 전" 인계 설명은 과거 상태이므로, 진행 상황은 이 문서의 현재 인계와 최신 worklog를 우선한다. 목록 작성이나 일부 연결이 이후 전체 기능의 일괄 승인은 아니다.
- 스킬 작업은 보류 상태다. 재개 시 다음은 **스킬 5-1. 서리 누적·발동 기준·감속량·지속 시간 규칙 확정**이며, 이후 스킬6. 불씨 / 7. 뱀 보스 각성 / 8. 독3개 조합 순서다. 스킬9. 조합 진행도는 제안 채택 여부 확인 후 진행한다. 보스 작업 번호와 혼동하거나 다음 기능을 미리 구현하지 않는다.

#### 현재 데이터·실행 구조
- 카드13종은 Assets/Prototype/Rewards의 RewardCardData 에셋이다. 씬의 RewardCardCatalog에 한 번만 등록하며, Singleton<RewardCardCatalog>를 상속해 ID→데이터 Dictionary로 조회한다. Singleton은 씬 배치 방식으로 자동 생성/탐색 및 DontDestroyOnLoad를 사용하지 않는다.
- RewardCardSelector는 카탈로그 목록에서 서로 다른3장 추출, 다음 보상에는 재등장 가능. 카드 에셋을 Selector/PlayerStats에 다시 드래그 등록하지 않는다.
- 확정 시 Controller가 빛 조각 소비 후 History에 카드 ID·실제 투입량을 기록하고 changed Action을 실행한다. History의 동일 카드 재획득 기록은 별도로 누적한다.
- PlayerStats는 History 변경 알림에 RecalculateStats 하나를 등록한다. 단일 순회에서 공통 검사/수치 계산 후 카드 데이터의 StatEffectType으로 switch 합산한다. 카드 이름/ID별 하드코딩 분기를 추가하지 않는다.
- PlayerAttackEvents는 Player에 부착된 적중 알림 전달자다. 보스/공격 구분을 영구 보관하지 않고 탄 명중 시 전달받아 등록된 Action에 넘긴다. 탄은 직접 피해를 먼저 적용한 뒤 적중 알림을 보낸다.
- PlayerAutoAttack이 발사 당시 Normal/Enhanced/Frenzy 구분을 PlayerBullet에 저장한다. 강화 연발은 탄마다 개별 적중 알림 발생, 풀 반환 시 참조/공격 구분 초기화. 독/서리/불씨는 발사 타입이 아니라 추가 효과다. 현재 폭주 전용 스킬은 없으며 기획서의 속성별 폭주 전용 연출은 이후 범위다.
- PlayerPoisonOnHit는 Player에 부착한다. 최초 활성화와 History 변경 시 HitEffect=ApplyPoison인 획득 카드만 조회해 일반/강화/폭주별 독 부여량을 미리 계산한다. 적중 Action의 OnAttackHit에서는 저장값을 고르고 맞은 보스의 AddStacks만 호출한다. 매 적중마다 History 순회/카드 조회/수치 재계산을 하지 않는다.
- PoisonLight(독 묻은 빛)는 일반/강화/폭주에 기본1중첩, VenomStrike(맹독 강타)는 강화/폭주에 기본2중첩+투입조각2개당1중첩이다. 두 카드 각1장/투입0이면 일반1·강화3·폭주3. 두 에셋 모두 HitEffect=ApplyPoison이며 다른 카드의 적중 효과는 아직 None이다.
- BossPoisonStatus는 각 보스 프리팹의 BossHealth와 같은 오브젝트에 부착한다. 보스별 currentStacks/만료/다음피해 시각만 보관하며 공통 규칙은 Assets/Prototype/Combat/PoisonSettings.asset SO에서 읽는다. SO에 실행 상태를 저장하거나 실행 중 공유 수치를 덮어쓰지 않는다.

#### 보스 생성·Flow·Action 구조
- Unity 버전은 ProjectSettings/ProjectVersion.txt 기준 **6000.3.27f1**이다.
- StageBattleController는 Inspector에 연결한 bossPrefab을 Instantiate하고 루트의 BossHealth/IBossPattern을 GetComponent하여 Flow에 반환한다. 생성객체·CurrentBoss·IsFighting 및 전투 시작/종료 Action을 관리한다. **IBossPattern의 초기화·등장·시작·중단 메서드를 호출하는 주체는 Flow**다. 관리자가 패턴 인터페이스를 보관·직접 실행하는 구조로 오해하지 않는다.
- MiniBossBattleFlow는 이름은 유지하지만 **IBossPattern 필드로 보관하며 TailBossPattern 캐스팅을 사용하지 않는다.** 생성→Initialize→BeginEntrance→IsEntranceComplete 대기→TryStartBattle(시작 Action)→StartPattern 순서다. 종료 시 StopPattern→탄 흡수·회수 대기→보스 퇴장→COMPLETE 종료→TryCompleteBattle(생성 보스 제거)→보상 요청을 사용한다.
- IBossPattern은 IsConfigured/IsEntranceComplete/IsRetreatComplete 및 Initialize(플레이어·필드·탄관리자·풀부모3개)/BeginEntrance/StartPattern/StopPattern을 정의한다. TailBossPattern과 HeadBossPattern이 구현한다.
- PlayerAutoAttack/BossHealthView/LightShardDropper는 관리자 시작 Action으로 현재 보스를 전달받는다. 자동공격은 종료 Action으로 대상·예약발사를 정리한다. 드롭은 이전 보스 PlayerAttackHit 해제 후 새 보스에 등록한다. 처치 타격의 Defeated 뒤 PlayerAttackHit가 발생하므로 체력소진 순간에 드롭 구독을 즉시 끊지 않는다. UI의 퇴장 표시 판단은 기존 Flow를 사용한다.
- Assets/Resources/Bosses/TailBoss.prefab은 체력·독·TailBossPattern과 BossBulletPattern/TailAttackPattern/TailLanePattern을 같은 루트에 가진다. 기존 분리 패턴 전용 씬 오브젝트는 제거했다. BossBulletPattern 파일/클래스 이름은 아직 변경하지 않았다.
- Assets/Resources/Bosses/HeadBoss.prefab은 체력·독·HeadBossPattern·HeadBossEntrance·HeadBossFanPattern·HeadBossWallPattern·HeadBossFreezeWarning·HeadBossFreezePattern 및 외형/FirePoint/Tongue_TEMP를 가진다. 씬 참조는 프리팹에 저장하지 않고 생성 후 Initialize로 전달한다.
- **현재 Play 시작 보스는 테스트 편의를 위해 HeadBoss**다. 디스크 Stage01Prototype의 관리자 프리팹 참조도 HeadBoss GUID와 일치함을 읽었다. BossSpawnPoint와 생성 부모를 사용하며 편집 상태에 배치된 전투 보스는 없다. 짤보스1→첫 보상→Head 자동 진행 연결은 아직 없다.
- ResourceManager는 이전 프로젝트의 ResourceManager.cs/ResourcePath.cs를 읽어 구조만 확인했다. **도입은 보류**, 현재 Inspector 프리팹 직접 참조다. 보스 공통 설정 SO·등장 이동 SO/DOTween/추가 이동 인터페이스도 필요 시 추후 검토하며 선구현하지 않는다.

#### 현재 머리 보스 패턴·수치
- HeadBossPattern은 현재 구현 범위 확인을 위해 **등장1초→팬5초→대기1초→좌우 영역 경고2초→벽 접근1초→혀 예고0.5초→감지2초→Finished 대기**를 한 번 실행한다. 반복·돌진·물림은 없다. 이 연결은 사용자 요청으로 수행했으며 3-19 전체 구현 완료가 아니다.
- HeadBossEntrance는 자동 Start 대신 HeadBossPattern.BeginEntrance가 명시적으로 시작한다. 임시 시작(0,6.5,0)→종료(0,3.5,0),1초. Head 최대체력100/피격반경0.6. 수치는 Inspector에서 조절한다.
- HeadBossFanPattern은 아래쪽 고정140도/전체15방향/연속3방향을 비워 실제12발. 틈 중심은 왼쪽20도부터 오른쪽20도와 묶음마다 교대한다. 플레이어 추적 조준은 제거했다. 속도2/반경0.12/구간5초/간격1초/초기풀64, 시작즉시 및1·2·3·4초 발사 후5초 종료.
- HeadBossWallPattern은 **경고 시작 시 플레이어X**를 고정한다. Wall Distance=2는 중심에서 좌우 **벽 중심**까지의 직접 거리다. coreMargin/PlayerLight 의존은 제거했다. 경고 안전통로 반폭은 max(0,wallDistance-visualRadius)로 벽 안쪽에 맞춘다. 표시반경0.12/세로간격0.3/접근1초/초기풀64.
- 벽 구성 탄은 Assets/Prototype/HeadBossWallVisual.prefab의 **Transform+SpriteRenderer만** 풀링한다. TestBullet/Collider/피격/퍼펙트/BulletRegistry 등록이 없다. 좌우 접근 전 경고는 HeadBossWallAreaWarning.prefab(비활성 사각형·Unlit·반투명 붉은색)을2개 풀링하며 안전통로를 비워2초 표시한다. 경고·벽 기준은 표시 시작 후 추적하지 않는다.
- HeadBossFreezeWarning은 Unlit Tongue_TEMP를0.5초 표시·정리하고 완료 상태를 제공한다. 전용 AudioSource는 있지만 **hissClip과 AudioSource.clip은 사용자 지시로 빈칸**이다. 소리 없이 진행하는 것이 정상이며, 나중에 클립이 연결되면 재생한다. 시간정지 시 예고/소리도 정지한다.
- HeadBossFreezePattern은 **감지 시작 순간 플레이어 위치**를 safePosition으로 저장한다. Lane Distance=1.2는 그 중심에서 좌우 **통로 탄 중심**까지의 직접 거리이며 반경으로 계산하지 않는다. 벽과 탄은 기준을 잡는 시점이 달라 경고 중 이동하면 중심이 달라질 수 있다. 탄 생성 높이는 기존 필드 상단이다.
- 통로 탄은 **각 발사마다 좌우 같은 확률로 한쪽1발**, 같은쪽 연속 가능. Lane Fire Interval=0.5초, 감지2초 동안0/0.5/1/1.5초 총4발. 이동 감지 조준탄은 별도 Aimed Fire Interval=0.2초, 입에서 플레이어 방향1발. 안전위치와 실제 XY차가0.05초과하면 감지하고 처음/재이탈 순간 즉시발사, 벗어나 있는 동안 반복, 돌아오면 중단한다. 속도2/반경0.12/초기풀64. 두 종류 모두 기존 TestBullet 피해·퍼펙트·풀링 사용.
- 감지 종료 후 새 공격은 중단하되 발사된 탄은 계속 이동한다. **벽 후퇴는 아직 없어 감지 종료 후 벽은 남고, 보스 종료 시 ClearWalls로 즉시 반환**한다. 3-10에서 정상 후퇴를 추가할 예정이다.
- 수정된 GDD의 넓은부채꼴→연사벽·얼어붙기→눈빛조준 돌진·물기 규칙과 확장 작업3-4~3-20을 따른다. 물림·흔들기3회·폭주시간정지·착지무적은 아직 미구현이며 세부 확정 단계부터 진행한다. 큰 덤불은 마지막 뱀 보스전 전용이므로 짤보스2에 선구현하지 않는다.

#### 1스테이지 진행 확정 사항·아직 적용하지 않은 것
- 같은 씬에서 짤보스1→보상확정→짤보스2→보상확정→마지막 보스 순서. 등장 완료 후 전투 시작. 마지막 보스 처치→연출·남은 조각흡수완료→클리어표시→진행정지로 확정했다. **각성 카드 선택은 다음 스테이지 제작 때 추가**하며 이번에는 다음스테이지/재시작도 제외한다.
- 전투 종료 체력 완전회복·폭주 종료/게이지0·획득카드/스탯/조각보유 유지·보상투입량만소비를 확정했지만 **완전회복과 폭주 초기화는 아직 미구현**이다. 독·기존 탄/위험영역 정리는 구현돼 있다.
- 빛 조각은 **드롭 즉시 플레이어를 추적하고 도착 시 회복·보유량 반영, 남은 조각 흡수까지 끝나면 보상창 표시**로 변경 승인했지만 **자동흡수와 보상대기는 아직 미구현**이다. 현재 기존 직접줍기/생성위치 방식을 유지한다. 카드·스탯·보유량의 다음 보스 연결도 아직 전체 사이클 검증 전이다.

#### 확정 규칙·미정 항목·테스트 설정
- 독 임시 시작값: 중첩당피해1/주기1초/지속3초/기본최대10중첩. 추가 부여 시 전체 만료 갱신, 기존 피해 주기 유지. 만료와 겹친 마지막 주기 피해 후 전체 제거. 사망/비활성 시 독 정리, 시간 정지 중 독도 정지. 독 피해는 TryTakeDamage를 사용해 추가 빛 조각 드롭을 발생시키지 않는다.
- 독 묻은 빛의 빛 조각 강화량은 미정이며 현재 추가수치0이다. 맹독 강타는 독 부여량 증가 카드이며 독10중첩 폭발은 별도7-5 각성 카드로 미구현이다.
- 넓은 헤일로는 최대반경/기본최대체력에 동일 증가율 적용, 최대체력 소수점 올림, 획득 시 현재체력은 유지한다. 보스 종료 완전회복은 보스1-4에서 규칙 확정했으나 아직 구현하지 않았다. 코어는 고정이다.
- 짤보스1 종료는 탄흡수→퇴장→COMPLETE 표시 종료→보상창 순서로 연결했다. Play 시작 자동 표시는 제거했다. 임시 F8 재열기는 RewardSelectionPreview_TEMP에만 있으며 같은 RewardSelectionFlow를 호출한다. 보상창 표시 중 timeScale0 및 필드 처리 중지, 확정/비활성 시 이전 시간 배율 복원.
- 테스트용 시작 빛 조각10개, 체력20% 미만 드롭30%/이상10%, 플레이어 주변 생성거리1~2(기존 필드 여백 Clamp 때문에 경계에서는 더 가까울 수 있음). 임시값은 해당 코드의 ##TEMP와 작업 기록을 참고한다.
- 서리·불씨·각성·조합의 실제 효과는 아직 구현하지 않았다. 카드 데이터만 존재한다. 서리/불씨 규칙은 각각5-1/6-1에서 먼저 확정한다.
- 에이전트는 Play/명시적 프로젝트 컴파일 확인/테스트를 실행하지 않았다. 컴포넌트/프리팹 편집에 필요한 AssetDatabase.Refresh와 script eval만 사용했다. 사용자는 기존 꼬리 흐름·프리팹런타임생성·보스사망시독0·Head등장/타게팅/HP바/팬·좌우틈교대·혀표시·좌우경고/안전지대·랜덤단발/기존판정을 확인했다. 이동감지/후퇴/전체종료·보상 및 최신 거리·간격 조정까지 모두 검증됐다고 확대하지 않는다.
- 최신 거리값 저장 시 열린 씬 dirty=false/playing=false였고 프리팹은 저장 완료했다. 과거 PlayerPoisonOnHit 추가/패턴이전/씬참조 설정 시 미저장 기록이 있으므로 재개 시 **현재 열린 씬/디스크/컴포넌트와 git status를 다시 읽고**, 미저장 변경을 버리거나 임의 Reload하지 않는다. 현재 여러 보스 관련 미추적 파일과 씬/코드/GDD 수정이 있으므로 모두 보존한다.
- 이 인계 상태는 위 날짜 기준이다. 이후 작업 기록과 사용자 지시가 있으면 그 내용을 우선한다.
