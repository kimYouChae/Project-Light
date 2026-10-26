# 코드 작성 방식

이 문서는 두 부분으로 나뉜다.
1. **새 프로젝트 규칙**: 새 프로젝트에서 반드시 따를 규칙이다.
2. **OHO 관찰 기록**: OHO 코드를 분석한 참고 자료다.

두 부분이 다르면 **새 프로젝트 규칙이 우선**한다. 관찰 기록의 "불일치" 항목은 규칙에서 하나로 정했으므로, 관찰 기록을 근거로 다른 방식을 고르지 않는다.

---

## 1. 새 프로젝트 규칙

### 네이밍
- 클래스, 메서드, public 프로퍼티: PascalCase
- private 필드, [SerializeField] 필드, 지역 변수, 매개변수: camelCase (접두사 없음)
- 상수: UPPER_SNAKE_CASE
- 폴더와 타입의 PopUp 표기는 "PopUp"으로 통일
- OHO의 Singleton<T>를 가져올 때 Singleton_Awake는 OnSingletonAwake로 이름을 바꾼다

### 파일
- 파일 하나에 클래스 하나, 파일명과 클래스명은 같게

### 접근 제한자
- 접근 제한자는 항상 명시 (private 생략 금지)

### 이벤트
- 이벤트를 등록하는 API는 반드시 해제 API와 쌍으로 만든다
- 등록한 쪽은 OnDisable 또는 OnDestroy에서 해제한다

### async
- async void는 Unity 이벤트 메서드(Start 등)에서만 쓰고, 반드시 try/catch로 감싸 예외를 로그로 남긴다
- 그 외는 async Task를 쓴다. _ = 로 기다리지 않고 호출할 때도 예외가 로그로 남게 한다

### 인스펙터
- Dictionary에 [SerializeField]를 쓰지 않는다. 인스펙터 설정이 필요하면 직렬화 가능한 키·값 쌍 리스트로 만들고 런타임에 Dictionary로 변환한다

### 주석
- 처리 단계와 필드 용도는 한글 // 주석
- 외부에서 쓰는 public API는 XML summary 주석
- 디버그 로그는 주석 처리해서 남기지 말고, 임시로 둘 경우 // ##TEMP 표시

### OHO에서 따라 하지 않을 것
- 해제 API 없이 이벤트만 등록하는 구조 (`Localization/LocalizationManager.cs`의 `RegisterChangeLanguage`)
- catch 없는 async void (`Sound/SoundManager.cs`의 `Start`)
- Dictionary에 [SerializeField] (`Spawner&Bullet/SpawnerAnimator.cs`의 `spawnerAniStateToAction`)
- 오타가 있는 이름 (`DropParemeter`, `AchiveObject.cs`)

---

## 2. OHO 관찰 기록 (참고용)

### 분석 범위
- `D:\Project-G\Project  G\Assets\1.Script`의 직접 작성된 것으로 보이는 게임 코드 중 아래에 명시한 파일의 비네트워크 부분을 관찰했다. Photon PUN·RPC·소유권·동기화 및 기타 통신 로직은 근거에서 제외했다.
- 아래 상대 경로의 기준은 `D:\Project-G\Project  G\Assets\1.Script\`이다.

### 네이밍
- 클래스는 PascalCase. 예: `SoundManager`, `ButtonClickImage` (`Sound/SoundManager.cs`, `UI/UIComponent/ButtonClickImage.cs`)
- 변수·매개변수는 camelCase 예가 있다: `startVolume`, `targetVolume`, `fadeTime` (`Sound/BGMManager.cs`)
- 불일치 — 필드 대소문자: `soundTrs` (`Sound/SoundManager.cs`)와 `MasterSlider`, `SFXSlider` (`UI/PopUP/SettingPopUP.cs`)가 공존
- 불일치 — private 필드명: `languageContainer`와 `ChangeLanguageAction`이 같은 파일에 공존 (`Localization/LocalizationManager.cs`)
- 불일치 — 메서드명: `PlayBGM` (`Sound/BGMManager.cs`), `langAdd`·`langGet` (`Localization/LocalizationManager.cs`), `Singleton_Awake` (`Utility/Singleton.cs`)
- 불일치 — 상수명: `DEFAULT_FADE_TIME` (`Sound/BGMManager.cs`), `baseVolume` (`Sound/SoundVolume.cs`), `DropParemeter` (`Map/MarketMap/CartMerchant.cs`)

### 폴더·파일 구조
- 기능별 폴더와 기능 내부 세부 폴더를 사용한다. 예: `Sound/BGMManager.cs`, `UI/PopUP/SettingPopUP.cs`, `Data/PlayerStatusData/PlayerStatus.cs`
- 불일치 — 팝업 표기: `SettingPopUP`, `TextPopUp`, `UI/PopUp_LeaderBoard/LeaderBoardPopUp.cs`가 공존
- 불일치 — 파일명과 클래스명: `AchiveObject.cs`에 `AchieveObject` 클래스, `PlayerStatus.cs`에 `PlayerStatusData`와 `PlayerStatus` 두 클래스

### 클래스 구성
- 공용 매니저는 `Singleton<T>`를 상속하고 초기화 메서드에서 초기화한다. 공통 베이스가 인스턴스 탐색·생성·중복 제거와 초기화 호출을 담당한다. 영속화는 `SetUpDontDestroy()`를 별도 호출한다 (`Utility/Singleton.cs`, `Resource/ResourceManager.cs`)
- 사운드는 믹서·볼륨의 `SoundManager`와 BGM 재생·페이드의 `BGMManager`로 역할을 나눈다. UI 호버는 별도 컴포넌트, 팝업 공통 동작은 베이스와 override로 분리한다 (`UI/UIComponent/ButtonClickImage.cs`, `UI/PopUP/UIPopUP.cs`, `UI/PopUP/TextPopUp.cs`)
- 데이터는 일반 클래스·생성자·읽기 프로퍼티와 static 저장소로 구성한 사례가 있다 (`Data/PlayerStatusData/PlayerStatus.cs`)

### 이벤트·콜백
- UI는 `AddListener`에 람다를 등록한다 (`UI/PopUP/SettingPopUP.cs`, `UI/UIComponent/ButtonClickImage.cs`)
- 커스텀 알림은 private `Action<T>`에 `+=`하고 `?.Invoke`한다 (`Localization/LocalizationManager.cs`)
- 비동기 초기화 콜백은 `Func<Task>`에 누적하고 `GetInvocationList()`를 순회하며 순서대로 await한다 (`Sound/SoundManager.cs`의 `RegisterAction`·`Start`)

### 인스펙터 노출
- 참조·설정·런타임 상태에 `[SerializeField]`, 영역 구분에 `[Header]`·`[Space]`를 붙인다 (`UI/UIComponent/ButtonClickImage.cs`, `Sound/BGMManager.cs`)
- 불일치 — 접근 제한자 표기: 명시적 private, 생략된 private, protected가 공존 (`Sound/SoundManager.cs`, `UI/PopUP/SettingPopUP.cs`, `UI/PopUP/UIPopUP.cs`)

### 코루틴·async
- 시간 경과·프레임 대기는 `IEnumerator`, `yield return null`, `WaitForSecondsRealtime`을 사용한다. BGM은 Coroutine 핸들을 저장하고 기존 실행을 중지하며 완료 시 null로 만든다 (`Sound/BGMManager.cs`). 공용 실행 래퍼도 있다 (`Utility/CoroutineHandler.cs`의 `Run`·`Stop`)
- 에셋 로드는 `async Task<T>`·`Task<IList<T>>`와 `await handler.Task`를 사용한다 (`Addressable/AddressableManager.cs`)
- 불일치 — 비동기 예외 처리: catch 없는 `async void Start` (`Sound/SoundManager.cs`), `_ = SettingBGM()` 호출과 try/catch가 있는 `InitAudioClip` (`Sound/BGMManager.cs`)

### 주석
- 한글 `//`로 처리 단계와 필드 용도를 설명하고, XML `summary`·`param` 주석과 `#region`으로 일부 API·구간을 설명한다. 주석 처리한 Debug.Log도 남아 있다 (`Spawner&Bullet/SpawnerAnimator.cs`, `UI/PopUP/UIPopUP.cs`)