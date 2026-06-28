# No Rubber Bushing Wear

[English README](README.md)

`Car Mechanic Simulator 2021`용 작은 BepInEx 플러그인입니다. 두 가지 품질 개선 기능을 제공합니다.

- `Rubber Bushing`과 `Small Rubber Bushing`이 고장 부품으로 잡히지 않습니다.
- 수리 화면에서 차량 부품 위에 마우스를 올리고 `B`를 누르면 교체용 부품 1개를 즉시 구매합니다.

목표는 반복적인 진단 스트레스를 줄이면서, 일반 수리, 손상, 인벤토리, 저장 데이터 동작은 그대로 유지하는 것입니다.

## 기능

### Rubber Bushing 고장 방지

- 아래 부품만 대상으로 합니다.
  - `Rubber Bushing`
  - `Small Rubber Bushing`
- 다른 모든 부품은 vanilla 동작을 유지합니다.
- Rubber Bushing 계열 부품이 diagnostics에서 faulty로 표시되지 않습니다.
- 저장 파일을 수정하지 않습니다.
- 게임 파일을 수정하지 않습니다.
- 설정 파일이 필요 없습니다.

확인된 localization key:

```text
tuleja_1     = Rubber Bushing
tulejaMala_1 = Small Rubber Bushing
```

### QuickShop

- 수리 또는 조립 화면에서 차량 부품 위에 마우스를 올립니다.
- `B` 키를 누릅니다.
- 모드가 교체용 부품 1개를 구매해 인벤토리에 추가합니다.

QuickShop은 vanilla 부품 ID, 가격, 돈, 인벤토리 아이템 데이터를 사용합니다. 돈 체크를 우회하지 않습니다.

## 동작 방식

Steam판 CMS 2021은 IL2CPP Unity 게임입니다. 이 모드는 BepInEx IL2CPP + Harmony 플러그인으로 동작합니다.

Rubber Bushing 기능은 생성된 interop metadata의 `PartData`, `BodyPartData` condition 접근자를 패치합니다. 현재 부품이 Rubber Bushing 계열로 식별되면 condition read는 `100`으로 반환하고, condition write는 해당 부품에 대해서만 건너뜁니다.

QuickShop 기능은 아래 UI 메서드를 패치합니다.

- `PartScript.SetMouseOver(bool)`
- `PartScript.Update`
- `PartScript.OnDisable`
- `PartScript.OnDestroy`

패치는 수리 화면에서 마우스를 올린 `PartScript`를 기억하고, `B` 입력을 감지한 뒤 vanilla 데이터에서 부품 ID와 가격을 읽어 새 `Item`을 vanilla 인벤토리에 추가하고 부품 가격만큼 돈을 차감합니다.

## 요구 사항

- Windows/Steam판 `Car Mechanic Simulator 2021`
- 빌드용 .NET SDK 9 이상
- BepInEx Unity IL2CPP x64

중요: CMS 2021은 `GameAssembly.dll`을 사용하는 IL2CPP 게임입니다. Mono용 BepInEx만으로는 충분하지 않습니다. 게임 루트에는 아래 파일이 있어야 합니다.

```text
BepInEx\core\BepInEx.Unity.IL2CPP.dll
BepInEx\core\Il2CppInterop.Runtime.dll
```

테스트한 CMS 2021 환경에서는 IL2CPP chainloader가 Unity logging 설정 중 실패할 경우 `BepInEx\config\BepInEx.cfg`에서 `UnityLogListening = false`로 설정해야 했습니다.

## 빌드

프로젝트 기본 경로에 게임이 설치되어 있다면:

```powershell
dotnet build src\NoRubberBushingWear.csproj -c Release
```

설치 경로가 다르다면:

```powershell
dotnet build src\NoRubberBushingWear.csproj -c Release -p:GameRoot="C:\Program Files (x86)\Steam\steamapps\common\Car Mechanic Simulator 2021"
```

빌드 결과:

```text
src\bin\Release\NoRubberBushingWear.dll
```

## 설치

1. CMS 2021 게임 루트에 BepInEx Unity IL2CPP x64를 설치합니다.
2. 게임을 한 번 실행해서 BepInEx가 IL2CPP interop assemblies를 생성하게 합니다.
3. 빌드된 DLL을 아래 위치에 복사합니다.

```text
Car Mechanic Simulator 2021\BepInEx\plugins\NoRubberBushingWear.dll
```

4. 게임을 실행한 뒤 아래 로그를 확인합니다.

```text
BepInEx\LogOutput.log
```

아래 메시지가 보이면 로드된 것입니다.

```text
No Rubber Bushing Wear 1.1.0
QuickShop enabled
```

## 검증 체크리스트

- 새 저장이 정상적으로 로드되고 플레이됩니다.
- 기존 저장이 정상적으로 로드되고 플레이됩니다.
- Rubber Bushing이 diagnostics에서 faulty로 표시되지 않습니다.
- Small Rubber Bushing이 diagnostics에서 faulty로 표시되지 않습니다.
- story mission과 repair order가 정상적으로 완료됩니다.
- 다른 suspension 부품은 vanilla처럼 고장날 수 있습니다.
- 수리 화면에서 차량 부품 위에 마우스를 올리고 `B`를 누르면 교체용 부품 1개가 구매됩니다.
- 플레이어 돈이 vanilla 부품 가격만큼 감소합니다.
- 구매한 부품이 인벤토리에 표시됩니다.
- BepInEx 로그에 이 플러그인에서 발생한 새 error/warning이 없습니다.

## 제한 사항

- 이 모드는 저장 파일 편집기가 아니라 런타임 Harmony 패치입니다.
- QuickShop은 수리 화면의 차량 부품 hover를 대상으로 합니다. 부품 상점 UI 내부 단축키는 추가하지 않습니다.
- 관련 없는 시스템은 의도적으로 변경하지 않습니다.
- 게임 업데이트로 내부 메서드명이나 구조가 바뀌면 재검증이 필요할 수 있습니다.
- 설정 파일은 의도적으로 제공하지 않습니다.

## 개발 메모

조사 기록은 [docs/research.md](docs/research.md)에 있습니다.

현재 로컬에서 확인한 환경:

- CMS 2021 Steam 설치본
- BepInEx Unity.IL2CPP win-x64 `6.0.0-be.752`
- `UnityLogListening = false`
- .NET SDK `9.0.315`
