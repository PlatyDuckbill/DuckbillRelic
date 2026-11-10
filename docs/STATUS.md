# 현재 상태

갱신: 2026-11-10 · 단계: 기획 (코드 없음) · 기준 클라이언트: KR 2026.10.06 · 글로벌 2026.10.16 (같은 패치)

## 현재 사실

- 이름·InternalName: `DuckbillRelic`.
- 실현 가능성 조사 완료: 레릭 보유(아이템 ID), 직업별 업적, 퀘스트 기록, 재료 보유 수로 대부분의 진행을 자동 판정할 수 있다. 근거는 [GAME-DATA](research/GAME-DATA.md), [CLIENT-STATE](research/CLIENT-STATE.md).
- 자료 재검증(2026-11-10): 게임 시트·ClientStructs·외부 자료로 기획 자료를 다시 대조했다. 큰 틀(계열 11개, 직업 수, 업적 481건, KR·글로벌 같은 ID)은 맞았고 다음을 고쳤다.
  - 아니마 직업별 업적은 3단계(새로운 아니마)부터.
  - 우주 도구는 직업당 20종, 연구 데이터 7종. 화면은 업적 아이템 기준 4줄.
  - 업적 조건 해석(Type 2·24), 업적 목록을 받기 전의 "미완료"는 모름.
  - 보유 관찰은 본 적 있는 최고 단계만 누적 저장, 판정은 단계 아이템 ID 목록으로만.
  - 재료: 팬텀 이전 단계 무기·숨은그림자 비용·KR 재료 이름, 레지스탕스 첫 무기 무료, 교환 단가는 게임 교환 목록에서.
- 범위: 레릭 무기 6계열과 도구 3계열(하늘강철·모웬·우주)의 진행, 남은 재료 합산. 장인·찬란한 도구는 제외, 우주 도구는 큰 단계만(2026-11-10 결정). [FEATURES](product/FEATURES.md).
- 화면 결정(2026-11-10): 목업 4 "A 최소" 방향. 칸에는 결과 5가지만, 근거는 상세 한 줄, 판정 정정은 [⋯] 메뉴, 진행은 "완성 n/m", 단계 행 이름은 번호 + 게임 접미어(없는 단계는 사용자가 정한 이름), 용어는 게임 UI 표기(장비함·환상의 옷장·추억의 보관함), 집사는 연 시점 관찰만, 나이트는 검 기준, 직접 입력은 재료 합산에 쓰이는 값만. [SCREENS](product/SCREENS.md).
- 목업 4: [mockups/mockup-v4.html](mockups/mockup-v4.html) (공유 사본: https://claude.ai/artifact/1bddKyBX15dFaEfaHkxENb, 비공개. 같은 링크에 2·3차본이 이전 버전으로 남음).
- 재검증 보고서 공유 사본: https://claude.ai/artifact/XgbsjCe4Y9xFCFrBrLuCEW (비공개). 원본 보고서·조사 노트는 저장소 밖 로컬 `reports/`·`research_notes/`에 있고, 확정 내용은 research·product 문서에 반영했다.
- 원격 저장소: https://github.com/PlatyDuckbill/DuckbillRelic (`main`, 2026-11-10 첫 푸시).
- 공용 설치 목록 [DuckbillRepo](https://github.com/PlatyDuckbill/DuckbillRepo)에 등록했다. 아직 `repo/release.json`이 없어 첫 배포 대기로 건너뛴다. 첫 배포 때 Tweak·Fisher와 같은 절차(버전 태그·정식 Release·`DuckbillRelic-<4자리 버전>.zip`과 `.zip.sha256`·태그 고정 아이콘 `assets/*.png`, 그다음 `main`의 `repo/release.json` 갱신)를 따르면 자동으로 목록에 추가된다.

## 남은 확인·결정

- 선행 퀘스트(해금) 표시는 보류했다(2026-11-10). 게임 데이터의 선행 퀘스트 연결로 자동 추출할 수 있음을 확인했고, 나중에 추가를 검토한다.
- 원본 시트 작성자에게 설명문·팁 사용 허락을 받을지 결정. 허락 없이는 수치만 별도 검증해 쓴다.
- 실기 확인 대기: 업적 목록을 불러온 상태가 지역 이동·캐릭터 전환 뒤에도 유지되는지, KR Dalamud의 ClientStructs 버전과 `IUnlockState` 유무, 레릭 `Flags`·환상의 옷장 보관 가능 여부, 황도 12문서 비트. 목록은 [CLIENT-STATE](research/CLIENT-STATE.md#확인-대기-실기).
- 자료 정리 남음: 에우레카 전 직업 아이템 ID, 모든 계열 교환 단가(SpecialShop), 제타·조디악 재제작 재료, 레지스탕스 재료 KR 이름, 하늘강철·모웬 2단계 이후 재료.

## 다음 작업 하나

1차 구현 범위를 확정하고 프로젝트 골격부터 시작한다.
