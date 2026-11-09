# 실행 중 읽을 수 있는 상태

조사일: 2026-11-10 · 근거: [FFXIVClientStructs](https://github.com/aers/FFXIVClientStructs) main 브랜치 소스(고정 revision 미기록 — 구현 착수 시 실행 중 Dalamud 배포판의 ClientStructs로 다시 확인). 실기 확인은 아직 없다.

| 대상 | 구조 | 조건·한계 |
|---|---|---|
| 업적 완료 | `Client::Game::UI::Achievement` — `State`/`IsLoaded()`, `IsComplete(id)`, 완료 비트맵 | 비트맵은 업적 목록을 요청·수신한 뒤에만 유효(`State == Loaded`). 요청은 사용자가 업적 화면을 열 때 게임이 보낸다. 플러그인은 `RequestCompletedAchievements`·`RequestAchievementProgress`를 호출하지 않는다 |
| 아이템 보유 | 인벤토리(장착·병기고·소지품·크리스탈·화폐) | 항상 읽힘. 초코보 가방·집사·투영대는 해당 화면을 연 뒤에만 정확하므로 관찰 시각과 함께 저장 |
| 아이템 플래그 | `InventoryItem.Flags`에 `Relic = 4` | 레릭 아이템은 마테리아 칸 등을 다른 값으로 쓸 가능성. 넥서스 빛 등 저장 위치는 확인 안 됨 |
| 퀘스트 | 퀘스트 완료·진행 시퀀스 | 항상 읽힘. 고대의 무기 1단계처럼 직업마다 퀘스트가 다른 경우는 직업 구분 가능, 같은 퀘스트를 반복하는 단계는 구분 불가 |
| 황도 12문서 | `RelicNote` — `RelicId`, `RelicNoteId`, 몬스터 10칸 진행, 던전·돌발·리브 완료 비트 | 소스 주석상 돌발 index 3과 리브 index 0이 같은 비트(7)를 써서 겹침 의심 → 실기 확인 필요 |
| 콘텐츠 값 | `PlayerState.GetContentValue(key)` — 에우레카 원소 레벨(2~4), 보즈야 저항 계급(5), 초승달 지식 레벨(6·7) | 진행 보조 표시 후보 |
| 우주 탐사 | `WKSManager` — `ResearchModule`(정의 없음), 직업별 점수 11칸 | 연구 데이터 구조는 공개 소스에 없음 |

## 조사 필요

- 넥서스 빛·룩스 빛 등 무기 안에 쌓이는 수치의 저장 위치.
- 황도 12문서 비트 배치.
- 팬텀 숨은그림자의 지식 크리스탈 진행, 그림자 단계 에테르 게이지(무작위 임무) 저장 위치.
- 우주 도구 연구 데이터(`WKSResearchModule`).

확인 전까지 이 값들은 수동 입력칸으로 둔다.
