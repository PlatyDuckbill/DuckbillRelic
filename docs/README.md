# 문서 지도

| 문서 | 내용 |
|---|---|
| [STATUS](STATUS.md) | 현재 사실, 남은 결정, 다음 작업 하나 |
| [product/FEATURES](product/FEATURES.md) | 기능 범위와 완료 판정 규칙 |
| [product/SCREENS](product/SCREENS.md) | 화면 구성과 목업 |
| [research/GAME-DATA](research/GAME-DATA.md) | KR·글로벌 게임 데이터 조사 결과 (업적·아이템·단계 이름, 카탈로그 생성 규칙) |
| [research/CLIENT-STATE](research/CLIENT-STATE.md) | 게임 실행 중 읽을 수 있는 상태와 조건 (ClientStructs·Dalamud 근거) |
| [research/SOURCE-SHEET](research/SOURCE-SHEET.md) | 출발점이 된 커뮤니티 시트 구조와 사용 조건 |
| [mockups/](mockups/) | 화면 목업 HTML (최신: mockup-v4) |

코드: `src/DuckbillRelic.Core`(게임에 의존하지 않는 카탈로그·판정), `src/DuckbillRelic.Plugin`(Dalamud 어댑터·화면).
도구: [tools/lumina-dump](../tools/lumina-dump/)(시트 조사), [tools/relic-catalog](../tools/relic-catalog/)(카탈로그 생성). 생성 자료는 [data/](../data/).
