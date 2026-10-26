# 오브젝트 풀링 구조

이 문서는 두 부분으로 나뉜다.
1. **새 프로젝트 풀 규칙**: 새 프로젝트에서 반드시 따를 규칙이다.
2. **ETD 분석 기록**: ETD(Element Tower Defense)의 풀링 코드를 분석한 참고 자료다. OHO에는 풀링 구조가 없어서 ETD에서 가져온다.

두 부분이 다르면 **새 프로젝트 풀 규칙이 우선**한다.

---

## 1. 새 프로젝트 풀 규칙

### 가져올 것
- `ObjectPool.cs` → 이름을 바꿔 `ComponentPool.cs`로 가져온 뒤 아래 규칙대로 보강한다
- `IObjectFactory.cs` → 수정 없이 그대로 가져온다
- `Extension.cs`의 `InitTransform` → 이 메서드 하나만 static 확장 클래스에 담아 가져온다. 같은 파일의 다른 유틸리티는 가져오지 않는다

### 이름
- 클래스 이름은 `ComponentPool<T>`로 바꾼다 (`UnityEngine.Pool.ObjectPool<T>`와 충돌 방지)
- summary 주석은 실제 타입 제약(Component)에 맞게 고친다

### API
- T 기반 대여·반환만 남긴다. GameObject 기반 오버로드는 쓰지 않는다 (GetComponent 비용과 null 위험)

### 팩토리
- 탄, 이펙트 등 풀링 대상마다 `IObjectFactory<T>`를 구현한 팩토리를 새로 만든다
- 팩토리는 로컬 생성만 한다 (네트워크 생성 없음)

### 사전 생성
- 초기 수량은 인스펙터에서 정한다. 탄은 최대 동시 수량만큼 미리 만든다
- 고갈되어 새로 만들 때는 경고 로그를 남긴다 (초기 수량이 부족하다는 신호)

### 대여
- 대여할 때 위치·방향·속도·수명·크기 등을 설정하는 단계를 먼저 실행하고, 그 다음에 활성화한다
- 풀의 부모 오브젝트 아래에 있으므로 위치는 월드 좌표로 설정한다

### 크기
- `InitTransform`은 생성 시 localScale을 1로 리셋한다. 그래서 풀링 대상 프리팹의 루트 스케일은 1로 둔다
- 투사체 크기 강화처럼 런타임에 바뀌는 크기는 대여 시 설정 단계에서 매번 적용한다

### 반환
- 이미 반환된 오브젝트를 다시 반환하면 무시하고 경고 로그를 남긴다
- null은 반환받지 않는다

### 정리
- 풀 전체를 비우는 정리 API를 둔다 (씬 전환 시 사용)

---

## 2. ETD 분석 기록 (참고용)

경로 기준: `D:\Element_Tower_Defense\Assets\02_Scripts\`

### 관련 파일
- `Pooling/ObjectPool.cs`: 풀 본체
- `Pooling/IObjectFactory.cs`: 생성 계약 인터페이스 (1–9줄)
- `Utilities/Extension.cs`: `InitTransform` 확장 메서드 정의 (35–44줄)

### ObjectPool<T>
- **구조:** `ObjectPool<T> where T : Component`는 MonoBehaviour를 상속하지 않는 일반 제네릭 클래스다. 비활성 객체를 `Queue<T>`에 보관하고, 팩토리·초기 수량·선택적 부모 Transform을 생성자에서 받는다. 주석은 MonoBehaviour라고 설명하지만 실제 타입 제약은 Component다 (불일치 — 주석과 타입 제약)
- **초기 생성:** 생성자가 `InitPool()`을 호출해 `initCount`개를 생성·큐에 넣는다. `InstanceT()`는 `factory.CreateInstance()` → `temp.transform.InitTransform(parent)` → `SetActive(false)` 순서다
- **대여:** `GetPool()`은 GameObject, `GetPoolAsT()`는 T를 반환한다. 큐가 비면 한 개 생성해 Enqueue한 뒤 Dequeue하고 활성화한다. 대여 시 별도 상태 초기화나 Transform 재설정은 없다
- **반환:** `SetObject(GameObject)`는 비활성화 후 `GetComponent<T>()`를 큐에 넣고, `SetObject(T)`는 비활성화 후 직접 넣는다. 반환 시 상태 초기화나 중복 반환 검사는 없다
- **확장:** 고갈 시 한 개씩 늘어나며 상한·축소·일괄 정리 API는 없다

### IObjectFactory<T>
- `T : Component` 제약을 둔 제네릭 인터페이스로, T를 반환하는 `CreateInstance()` 하나만 선언한다. 생성 구현은 이 파일에 없다 (`IObjectFactory.cs:5–9`)

### InitTransform
- static `Extension` 클래스의 Transform 확장 메서드다. parent가 null이 아닐 때만 `SetParent(parent)`를 호출하고, 그 후 항상 localPosition을 zero, localRotation을 identity, localScale을 one으로 설정한다. parent가 null이면 기존 부모를 해제하지 않는다 (`Extension.cs:7–8, 35–44`)

### 네트워크 의존 여부
- `ObjectPool.cs`, `IObjectFactory.cs`, `InitTransform` 모두 Photon·RPC 등 네트워크 타입·호출이나 관련 using이 없다
- 실제 생성은 팩토리 구현체에 위임되므로, 새 프로젝트에서 로컬 생성 팩토리를 새로 만들면 네트워크 의존은 없다

### 이전 시 제안 (규칙에 반영됨)
- 대여 시 활성화 전에 탄 상태를 설정하는 단계 추가. 현재는 곧바로 활성화하므로 OnEnable이 새 발사 정보를 먼저 받는 구조가 보장되지 않는다
- 중복 반환·null 검사 추가. 현재는 검증 없이 Enqueue한다
- 최대 동시 수량에 맞춘 초기 수량과 정리 API 추가. 현재는 고갈마다 생성하고 정리 API가 없다