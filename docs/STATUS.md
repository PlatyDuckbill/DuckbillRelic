# 현재 상태

갱신: 2026-11-10 · 단계: 기획 (코드 없음) · 기준 클라이언트: KR 2026.10.06 · 글로벌 2026.10.16 (같은 패치)

## 현재 사실

- 이름·InternalName: `DuckbillRelic`.
- 실현 가능성 조사 완료: 레릭 보유(아이템 ID), 직업별 업적, 퀘스트 기록, 재료 보유 수로 대부분의 진행을 자동 판정할 수 있다. 근거는 [GAME-DATA](research/GAME-DATA.md), [CLIENT-STATE](research/CLIENT-STATE.md).
- 범위: 현재 출시된 모든 레릭 무기 6계열·도구 5계열과 남은 재료 합산. [FEATURES](product/FEATURES.md).
- 첫 화면 구조 합의: 직업×단계 표 + 상세 패널, 탭은 무기 / 도구 / 남은 재료. [SCREENS](product/SCREENS.md).
- 목업 2차본: [mockups/mockup-v2.html](mockups/mockup-v2.html) (공유 사본: https://claude.ai/artifact/1bddKyBX15dFaEfaHkxENb, 비공개).
- 원격 저장소: https://github.com/PlatyDuckbill/DuckbillRelic (`main`, 2026-11-10 첫 푸시).
- 공용 설치 목록 [DuckbillRepo](https://github.com/PlatyDuckbill/DuckbillRepo)에 등록했다. 아직 `repo/release.json`이 없어 첫 배포 대기로 건너뛴다. 첫 배포 때 Tweak·Fisher와 같은 절차(버전 태그·정식 Release·`DuckbillRelic-<4자리 버전>.zip`과 `.zip.sha256`·태그 고정 아이콘 `assets/*.png`, 그다음 `main`의 `repo/release.json` 갱신)를 따르면 자동으로 목록에 추가된다.

## 남은 확인·결정

- 목업 2차본에 대한 사용자 피드백.
- 원본 시트 작성자에게 설명문·팁 사용 허락을 받을지 결정. 허락 없이는 수치만 별도 검증해 쓴다.
- 실기 조사 필요: 넥서스 빛 등 무기 내부 수치, 황도 12문서 진행(RelicNote) 비트 배치, 팬텀 지식 크리스탈, 우주 도구 연구 데이터.
- 단계별 재료 정리: 제타, 하늘강철·모웬 2단계 이후, 팬텀 숨은그림자 등은 아직 비어 있다.

## 다음 작업 하나

목업 피드백을 반영해 화면 구성을 확정한다.
