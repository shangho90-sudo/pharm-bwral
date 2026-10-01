# 팽브롤 전용 게임 서버

브라우저는 입력만 전송하고 전투·피격·점수·충돌은 서버에서 계산합니다. 방장은 맵과 시작을 정하는 권한만 갖습니다. 서버는 Unity 클라이언트와 같은 ArenaSimulation, ArenaMap, ArenaNavigation 코드를 사용합니다.

## 로컬 실행

.NET SDK 10과 Node.js 22 이상이 필요합니다. 저장소 루트에서 실행하세요.

```powershell
dotnet restore Server/PharmaBrawl.Server.csproj --configfile Server/NuGet.Config
dotnet run --project Server/PharmaBrawl.Server.csproj --no-restore
```

웹게임의 START → 서버 주소에 `http://localhost:8787` 입력 → 닉네임 설정 → 방 만들기 또는 새로고침 → 캐릭터 선택 → 방장 시작. 미참가 슬롯은 서로 다른 AI 캐릭터로 채웁니다. 1:1부터 4:4까지 방을 만들 수 있습니다. 온라인 기능은 WebGL 빌드에서 동작합니다.

다른 터미널에서 `node Server/integration-test.mjs`로 8명 참가, 중복 선택 방지, 방장 권한, 입력 이동, 상태 일치, 방장 연결 종료 후 전투 지속을 검증합니다.

## 새 서버 배포 준비

`Server/Dockerfile`과 루트의 `render.yaml`을 포함했습니다. Render Web Service에서 저장소의 `codex/four-arenas-music` 브랜치를 선택하고 Dockerfile 경로를 `Server/Dockerfile`, Docker context를 저장소 루트로 지정합니다. `GAME_ORIGINS=https://shangho90-sudo.github.io`를 설정하고 `/health`를 상태 확인 경로로 사용합니다.

Blueprint의 `starter`는 상시 실행을 위한 유료 플랜입니다. 현재 외부 서버 계정 연결·서비스 생성·결제·배포는 실행하지 않았습니다. 계정에서 선택한 비용과 지역을 확인한 뒤 배포하세요. 게임 서버는 1개 인스턴스로 실행해야 합니다. 방 상태가 메모리에 있어 서버 재시작/배포 때 진행 중인 방은 사라집니다.

배포 후 얻은 HTTPS 주소를 게임의 서버 주소 칸에 넣습니다. 모든 참가자가 같은 주소를 사용해야 같은 방 목록을 볼 수 있습니다. 기본 주소를 전체 사용자에게 지정하려면 `Assets/StreamingAssets/server-config.json`에 `{ "endpoint": "https://실제-서버-주소" }`를 저장하고 WebGL을 다시 빌드합니다. 공개 URL에는 HTTPS/WSS를 사용합니다.

로그인·비밀번호 없이 게스트 참가 토큰을 발급하며 토큰은 WebSocket 첫 메시지로 전송합니다. 30Hz 전투 계산, 15Hz 상태 전송, 오래된 입력 중단, 느린 수신자의 출력 큐 제한, 연결 복구와 방장 승계가 포함되어 있습니다. 호스트 PC 성능이 전투 계산 속도를 결정하지 않습니다. 각 기기의 화면 프레임과 네트워크 지연은 기기·통신 환경의 영향을 받습니다.

구현 참고: [ASP.NET WebSockets](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/websockets?view=aspnetcore-10.0), [Render WebSockets](https://render.com/docs/websocket).
