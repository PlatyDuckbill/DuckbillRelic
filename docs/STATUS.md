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
- 범위: 현재 출시된 모든 레릭 무기 6계열·도구 5계열과 남은 재료 합산. 우주 도구는 큰 단계만. [FEATURES](product/FEATURES.md).
- 화면 결정(2026-11-10, 권장안 채택): 칸에는 결과 5가지만 + "근거 표시" 토글, 자동 판정 정정 버튼, 진행은 "완성 n/m", 집사는 연 시점 관찰만, 나이트는 검 기준, 장인 도구 최종 단계 보유 시 아래 단계 추론 유지(안내 한 줄), 직접 입력은 재료 합산에 쓰이는 값만. [SCREENS](product/SCREENS.md).
- 목업 3차본: [mockups/mockup-v3.html](mockups/mockup-v3.html). 2차본 공유 사본: https://claude.ai/artifact/1bddKyBX15dFaEfaHkxENb (비공개).
- 원격 저장소: https://github.com/PlatyDuckbill/DuckbillRelic (`main`, 2026-11-10 첫 푸시).
- 공용 설치 목록 [DuckbillRepo](https://github.com/PlatyDuckbill/DuckbillRepo)에 등록했다. 아직 `repo/release.json`이 없어 첫 배포 대기로 건너뛴다. 첫 배포 때 Tweak·Fisher와 같은 절차(버전 태그·정식 Release·`DuckbillRelic-<4자리 버전>.zip`과 `.zip.sha256`·태그 고정 아이콘 `assets/*.png`, 그다음 `main`의 `repo/release.json` 갱신)를 따르면 자동으로 목록에 추가된다.

## 남은 확인·결정

- 목업 3차본에 대한 사용자 확인.
- 원본 시트 작성자에게 설명문·팁 사용 허락을 받을지 결정. 허락 없이는 수치만 별도 검증해 쓴다.
- 실기 확인 대기: 업적 목록을 불러온 상태가 지역 이동·캐릭터 전환 뒤에도 유지되는지, KR Dalamud의 ClientStructs 버전과 `IUnlockState` 유무, 레릭 `Flags`·투영대 보관 가능 여부, 황도 12문서 비트. 목록은 [CLIENT-STATE](research/CLIENT-STATE.md#확인-대기-실기).
- 자료 정리 남음: 에우레카·장인·찬란한 도구 전 직업 아이템 ID, 모든 계열 교환 단가(SpecialShop), 제타·조디악 재제작 재료, 레지스탕스 재료 KR 이름, 하늘강철·모웬 2단계 이후 재료.

## 다음 작업 하나

목업 3차본을 확인받아 화면 구성을 확정한다.
