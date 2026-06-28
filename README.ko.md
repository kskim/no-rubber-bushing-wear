# No Rubber Bushing Wear

[English README](README.md)

`Car Mechanic Simulator 2021`에서 `Rubber Bushing`과 `Small Rubber Bushing` 부품이 고장 부품으로 잡히지 않도록 하는 작은 모드입니다.

목표는 단순합니다. Rubber Bushing 때문에 반복적으로 진단이 막히는 불편만 제거하고, 다른 게임 시스템은 그대로 유지합니다.

## 기능

- 아래 부품만 대상으로 합니다.
  - `Rubber Bushing`
  - `Small Rubber Bushing`
- 다른 모든 부품은 vanilla 동작을 유지합니다.
- Rubber Bushing 계열 부품이 diagnostics에서 faulty로 표시되지 않게 합니다.
- 저장 파일을 수정하지 않습니다.
- 게임 파일을 수정하지 않습니다.
- 설정 파일이 필요 없습니다.

## 동작 방식

Steam판 CMS 2021은 IL2CPP Unity 빌드입니다. 이 모드는 BepInEx IL2CPP + Harmony 플러그인입니다.

런타임에서 생성된 interop `Assembly-CSharp` metadata를 기준으로 condition, wear, damage, diagnostic/fault 관련 메서드 후보를 찾고 Harmony patch를 적용합니다. 단, 현재 부품이 Rubber Bushing 계열로 식별될 때만 결과를 보정합니다.

확인된 localization key:

```text
tuleja_1     = Rubber Bushing
tulejaMala_1 = Small Rubber Bushing
```

Rubber Bushing 계열 부품으로 식별되면:

- condition read 결과를 `100`으로 보정합니다.
- diagnostic/fault 결과를 `false`로 보정합니다.
- damage/wear 계열 메서드는 해당 부품에 대해서만 실행을 건너뜁니다.

## 요구 사항

- Windows/Steam판 `Car Mechanic Simulator 2021`
- .NET SDK 9 이상
- Windows x64용 BepInEx IL2CPP

주의: CMS 2021은 `GameAssembly.dll`을 사용하는 IL2CPP 게임입니다. Mono용 BepInEx만으로는 부족합니다. 게임 루트에 아래 파일이 있어야 합니다.

```text
BepInEx\core\BepInEx.Unity.IL2CPP.dll
BepInEx\core\Il2CppInterop.Runtime.dll
```

## 빌드

프로젝트 기본 경로에 게임이 설치되어 있다면:

```powershell
dotnet build -c Release
```

다른 경로라면:

```powershell
dotnet build -c Release -p:GameRoot="C:\Program Files (x86)\Steam\steamapps\common\Car Mechanic Simulator 2021"
```

빌드 결과:

```text
src\bin\Release\NoRubberBushingWear.dll
```

## 설치

1. CMS 2021 게임 루트에 BepInEx IL2CPP x64를 설치합니다.
2. 게임을 한 번 실행해서 BepInEx가 IL2CPP interop assemblies를 생성하게 합니다.
3. 빌드된 DLL을 아래 위치에 복사합니다.

```text
Car Mechanic Simulator 2021\BepInEx\plugins\NoRubberBushingWear.dll
```

4. 게임을 실행한 뒤 아래 로그를 확인합니다.

```text
BepInEx\LogOutput.log
```

아래 메시지가 보이면 로드 성공입니다.

```text
No Rubber Bushing Wear loaded
```

## 검증 체크리스트

- 새 저장이 정상적으로 로드되고 플레이됩니다.
- 기존 저장이 정상적으로 로드되고 플레이됩니다.
- Rubber Bushing이 diagnostics에서 faulty로 표시되지 않습니다.
- Small Rubber Bushing이 diagnostics에서 faulty로 표시되지 않습니다.
- story mission과 repair order가 정상적으로 완료됩니다.
- 다른 suspension 부품은 vanilla처럼 고장날 수 있습니다.
- BepInEx 로그에 이 플러그인에서 발생한 새 error/warning이 없습니다.

## 제한 사항

- 이 모드는 저장 파일 편집기가 아니라 런타임 Harmony patch입니다.
- 관련 없는 시스템은 의도적으로 변경하지 않습니다.
- 게임 업데이트로 내부 메서드명이나 구조가 바뀌면 재검증이 필요할 수 있습니다.
- 설정 파일은 의도적으로 제공하지 않습니다.

## 개발 메모

조사 기록은 [docs/research.md](docs/research.md)에 있습니다.

현재 로컬에서 확인한 환경:

- CMS 2021 Steam 설치본
- BepInEx Unity.IL2CPP win-x64 `6.0.0-be.784`
- .NET SDK `9.0.315`
