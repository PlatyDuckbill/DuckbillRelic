# lumina-dump

개발 중 조사용 콘솔 도구입니다. 로컬 KR·글로벌 클라이언트의 Excel 시트를 Lumina로 읽어 같은 RowId끼리 나란히 출력합니다. 플러그인에는 포함하지 않습니다.

- 참조: Dalamud dev 배포판의 `Lumina.dll`·`Lumina.Excel.dll` (`%APPDATA%\XIVLauncher\addon\Hooks\dev`)
- 클라이언트 경로는 `Program.cs` 위쪽 상수에 있습니다.

```
dotnet build -c Release -o out
out/lumina-dump.exe achcat              # 업적 분류 목록
out/lumina-dump.exe ach 62,64,81        # 분류의 업적 (Type/Key/Data 포함)
out/lumina-dump.exe item "Penumbrae"    # 글로벌 이름 정규식으로 아이템 검색 + KR 이름
out/lumina-dump.exe types "Relic|WKS"   # Lumina 시트 타입 이름 검색
out/lumina-dump.exe sheet RelicNote ""  # 시트 행 덤프
```
