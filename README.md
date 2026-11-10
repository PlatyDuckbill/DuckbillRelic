# DuckbillRelic

FFXIV(KR·글로벌)의 레릭 무기·도구 진행을 직업별로 보여주는 Dalamud 플러그인입니다.

- 직업×단계 표로 계열별 진행을 한눈에 확인 (레릭 무기 6계열, 하늘강철·모웬·우주 도구)
- 장비함·소지품의 레릭과 직업별 업적으로 완료 단계를 자동 판정하고, 확인할 수 없는 단계는 직접 표시
- 서버에 아무것도 요청하지 않고, 자동 조작을 하지 않습니다

현재 1차 구현(진행 표) 개발 중이며 아직 배포하지 않았습니다. 첫 배포 후 공용 설치 목록 [DuckbillRepo](https://github.com/PlatyDuckbill/DuckbillRepo)에서 설치할 수 있습니다. 진행 상황은 [docs/STATUS.md](docs/STATUS.md), 문서 목록은 [docs/README.md](docs/README.md)를 보세요.

## 개발 빌드

.NET SDK 10과 KR XIVLauncher의 Dalamud(15.x)가 필요합니다.

```powershell
pwsh -File scripts/build.ps1            # 기본: %APPDATA%\XIVLauncherKR\addon\Hooks\dev 를 참조
```

출력된 `DuckbillRelic.dll` 경로를 Dalamud 설정 → 실험적 기능 → Dev Plugin Locations에 등록한 뒤 `/dbrl` 또는 `/duckbillrelic`로 엽니다.

레릭 단계·직업별 아이템·업적 대응 자료(`data/relic-catalog.v1.json`)는 게임 클라이언트 시트에서 [tools/relic-catalog](tools/relic-catalog/)로 생성합니다.
