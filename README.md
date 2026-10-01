# 팽브롤 / PHARMA BRAWL

Unity **6000.6.1f1**로 만든 3D 탑다운 약사 아레나 슈팅 게임입니다. 캐릭터 10명과 최대 4 vs 4 전투, 첨부 이미지에서 재구성한 4개의 선택 가능한 맵을 포함합니다. 게스트 방 생성/조회와 전용 서버 기반 온라인 멀티플레이를 포함합니다. 외부 서버는 아직 배포하지 않았으며 Server/README.md에 실행·배포 방법을 정리했습니다.

## 다른 컴퓨터에서 이어서 개발

1. Unity Hub에서 **6000.6.1f1**과 Windows Build Support, WebGL Build Support를 설치합니다.
2. `git clone --branch codex/four-arenas-music https://github.com/shangho90-sudo/pharm-bwral.git`으로 저장소를 받습니다. GitHub에서 **codex/four-arenas-music** 브랜치를 선택한 뒤 **Code → Download ZIP**도 사용할 수 있습니다.
3. Unity Hub → Add → 저장소 폴더를 선택합니다. `Assets`, `Packages`, `ProjectSettings`가 있는 폴더가 Unity 프로젝트입니다.
4. 첫 import가 끝나면 `Assets/Scenes/Pharmacy.unity`를 열고 Play를 누릅니다.
5. START → 닉네임과 방 설정 → 캐릭터와 맵 선택 → 게임 시작 순서로 게임을 실행합니다.

추가 Tripo 생성이나 로그인 없이 포함된 모델로 바로 실행할 수 있습니다. 캐릭터와 무기 모델, 텍스처, `.meta`, Humanoid Avatar와 프리팹을 함께 저장합니다. 컴퓨터를 끄면 로컬 Codex/Unity 작업은 중단됩니다. 다른 컴퓨터에서 이 README와 `DEVELOPMENT.md`를 기준으로 작업을 이어갈 수 있습니다.

## 조작 및 규칙

WASD 이동 · 마우스 조준 · 좌클릭 공격 · 우클릭/E 특수 스킬 · Space 궁극기 · Esc 일시정지.

**F2**는 이동 판정 표시를 켜고 끕니다. 녹색 점은 캐릭터 반경을 고려한 이동 가능 위치, 빨간 점은 이동 불가 위치입니다. 화면 우측 상단에서 음악을 켜거나 끌 수 있습니다.

## 네 가지 아레나

기존 고정 약국 맵을 제거했습니다. 네 맵은 `ArenaMap`의 같은 구조물 데이터로 3D 외형과 이동/탄환 판정을 만듭니다. 최신 전투 화면은 첨부 이미지의 배경과 3D 캐릭터·탄환을 조합합니다. 이동/탄환 충돌 영역은 배경의 벽·물·컨테이너 위치에 맞춰 별도로 설정했습니다.

| 맵 | 이동 가능 | 이동 불가 |
|---|---|---|
| 약국마을 | 포장길, 나무 다리, 수풀 | 캡슐 벽, 물, 보급 상자 |
| 네온 약품 연구소 | 연구소 바닥, 발광 약초 | 격벽, 실험 수조, 보급 상자 |
| 사막 약초 오아시스 | 모래, 약초, 강을 가로지르는 세 다리 | 강, 사암 유적 벽, 보급 상자 |
| 알파인 의약품 보급 기지 | 눈길, 중앙/측면 얼음, 수풀 | 컨테이너, 바위, 보급 상자 |

수풀은 통과 가능한 장식이며 은신 효과는 없습니다. 물은 이동을 막지만 탄환은 통과합니다. 일반 탄환은 벽/컨테이너/바위/상자에 막히고, 기존 관통 공격과 포격 능력은 유지합니다. 상자 파괴 후에는 해당 구역이 열립니다. 두 팀은 아래/위에서 시작하며 AI와 소환 로봇은 경로 탐색으로 다리와 통로를 이용합니다. 외곽 배경 건물과 나무는 전투 경계 밖에 있습니다.

## 배경 음악과 맵 소품

시작/캐릭터 선택/결과 화면용 1곡과 각 맵 전투용 4곡을 Runway로 생성했습니다. 보컬 없는 슈팅게임용 음악이며 `Assets/Resources/Music`에 MP3로 포함했습니다. 화면 전환과 반복 경계에 1.5초 크로스페이드를 적용하고 일시정지 때 음량을 낮춥니다. 음악 OFF 설정은 다음 실행에도 유지합니다.

Tripo P1으로 배경 건물 4개와 모듈 소품 4개를 생성했습니다. FBX/텍스처는 `Assets/Art/Maps`, 실행용 프리팹은 `Assets/Resources/MapProps`에 포함합니다. 원본 FBX의 축 변환을 유지하고 모듈 외형을 충돌 영역에 맞게 조정합니다. 실행이나 빌드에 추가 생성/로그인이 필요하지 않습니다. 작업별 모델·프롬프트·비용은 `ARENA-ASSETS.json`에 기록했습니다.

온라인은 1 vs 1부터 4 vs 4까지, AI 연습은 4 vs 4로 진행합니다. 미참가 슬롯은 AI가 채웁니다. 3분 또는 먼저 20킬에 도달한 팀이 승리합니다. 3.5초 동안 공격/피격이 없으면 회복하며, 사망 후 4초 뒤 부활합니다. 10명 모두 서로 다른 일반 공격, 스킬, 궁극기를 가집니다. 현재 게임 모드는 팀 데스매치입니다.

## 모델과 코드

- `Assets/Art/Characters/00` … `09`: Tripo FBX 원본, 텍스처, 보정된 `Pharmacist.prefab`, `UnityHumanoid.asset`, `UnityRigMesh.asset`.
- `Assets/Art/Weapons/00` … `09`: Tripo 의료 무기 FBX 및 텍스처.
- `Assets/Resources/Characters`: 공격 데이터와 캐릭터/무기 참조가 있는 ScriptableObject.
- `Assets/Scripts/ArenaSimulation.cs`: 고정 시간 간격의 전투, AI, 피격, 사망/부활, 점수 로직.
- `Assets/Scripts/PharmaGame.cs`: 맵, 카메라, UI, 풀링된 공격 표현, 사운드, 입력.
- `Assets/Scripts/PharmacistModelRig.cs`: 오른손 무기 소켓, 크기/축 보정, 두 팔 조준 자세와 간단한 다리 움직임.
- `Assets/Editor/CanonicalPharmacistRig.cs`: Tripo 스킨 본의 불완전한 매핑을 공통 Unity Humanoid 골격으로 보정합니다. 원본 FBX는 유지합니다.
- `Assets/Editor/PrototypeBuilder.cs`: 전투 자동 검증과 Windows/WebGL 빌드.

무기 배정은 사용자 이미지의 왼쪽 위부터 오른쪽, 다음 줄 순서로 캐릭터와 같은 번호끼리 연결했습니다. 쌍권총은 1개의 무기 세트 모델로 연결했습니다. 자세와 스킨 가중치는 프로토타입 수준이므로 일부 어깨/가운 변형, 손잡이 정밀 정렬은 후속 아트 작업 대상입니다. 별도 유료 애니메이션 클립은 생성하지 않았습니다.

## 빌드

네 맵 업데이트 빌드는 Unity 배치 모드에서 **`ArenaMapBuilder.Build`**를 실행합니다. 소품을 가져온 뒤 캐릭터 10명 × 맵 4개의 AI 경기, 이동 판정, 스폰/부활, 다리, 물 위 탄환, 파괴 상자, 음악과 소품 참조를 검사하고 Windows 실행본을 생성합니다. 출력 위치는 프로젝트 기준 `../../outputs/PharmaBrawl-Windows`입니다. 검사만 수행하려면 `ArenaMapBuilder.Validate`를 사용합니다.

Unity Build Profiles에서 Windows 또는 WebGL을 선택하거나 에디터 배치 모드의 `PrototypeBuilder.Build` / `PrototypeBuilder.BuildWeb`를 실행합니다. 두 메서드의 기본 출력은 프로젝트 상위 폴더의 `Windows` / `WebGL`입니다. `Build`는 캐릭터별 공격·스킬·궁극기, 회복, 사망/부활, 10개의 완전한 AI 경기를 검사합니다.

실행본 옵션: `--smoke-test`(헤드리스 AI 한 판), `--capture <절대 PNG 경로>`(전투), `--capture-models <절대 PNG 경로>`(10명 장착 갤러리), `--capture-lobby <경로>`(시작 화면), `--capture-result <경로>`(결과).

`--map 0..3`은 검증/캡처 때 사용할 맵을 지정합니다. `--capture-map <절대 PNG 경로>`는 선택한 맵만 1600×900으로 렌더링합니다. 일반 실행에서는 로비에서 맵을 선택합니다.

폰트: Noto Sans CJK KR, SIL Open Font License. 라이선스는 `Assets/Resources/Fonts/OFL.txt`에 포함되어 있습니다. 시작 화면과 캐릭터/무기 참조는 사용자가 제공한 이미지입니다.

## Browser version

Build with Unity 6000.6.1f1, WebGL module installed:

`Unity.exe -batchmode -quit -buildTarget WebGL -projectPath <project> -executeMethod ArenaMapBuilder.BuildWeb -logFile web-build.log`

Output: `../../outputs/PharmaBrawl-Web`. Gzip with JavaScript decompression fallback allows ordinary static hosting, including GitHub Pages. Serve the folder through HTTP; opening index.html directly cannot load the game. Desktop keyboard and mouse: WASD movement, left mouse attack, right mouse skill, Space ultimate. Click the game to enable browser audio.

The `gh-pages` branch contains the generated web player. Pages publishes that branch from its root. The editor project remains on `codex/four-arenas-music`.

Hero selection uses the supplied 1672×941 artwork as a UI atlas. ReferenceSelectionView maps ten hero cards, four map cards, music/back/start buttons to real interactions, and updates the selected portrait, HP, speed, range and skill descriptions from CharacterDefinition. The original default hero/map appearance is preserved; alternative selections receive live profiles and selection markers. SelectionScreen is imported without mipmaps, resizing or lossy texture compression to preserve Korean text.

## Reference gameplay update

Four cleaned reference background plates retain the supplied arena composition. Live 3D fighters, weapons, Tripo capsule projectiles, luminous team medallions and effects render over those plates. ArenaMap stores calibrated collision footprints: solid walls, containers and rocks block movement/shots; water blocks movement; three oasis bridges and village bridges are traversable; foliage and alpine ice are traversable. Playfield bounds include fighter radius, movement is subdivided to avoid tunnelling, and spawn positions search for terrain clearance.

ReferenceCombatView recreates the supplied HUD with live scores, clock, HP, charge, cooldowns, music and pause/ability controls. ReferenceResultView uses the supplied victory/defeat artwork and overlays actual match score and six-player statistics. Reference01 replaces the first hero with a newly generated and rigged Tripo model. The remaining nine heroes retain their existing distinct models and abilities. CapsuleProjectile uses the new textured Tripo mesh, colored emission and short trails.

Runway music has been replaced with five energetic instrumental arcade tracks (150–168 BPM prompts), one menu theme and four map themes. Generation task IDs and billed costs are in ARENA-ASSETS.json. The reference gameplay style is 2.5D; background scenery is baked artwork rather than independently rotating geometry.

Rebuild the updated web version with `ResultScreenBuilder.BuildWebAndCapture`. This imports new reference models, captures actual victory/defeat screens, validates 40 AI matches and terrain/boundary rules, then builds WebGL.


## 전투 효과음과 캐릭터 표현

Runway로 일반 공격 10종과 피격음 1종을 생성했습니다. AttackKind에 따라 캡슐·전기·액체 다트·음파·3연사·포격·쌍권총·로봇·확산·독성 발사음을 연결합니다. 캐릭터별 재생 간격과 거리 음량을 적용하며 3연사 발사음은 한 묶음당 한 번 재생합니다. 생성 내역과 11크레딧 비용은 COMBAT-AUDIO.json에 기록했습니다.

피격 시 기존 모델이 0.24초 동안 뒤로 젖혀졌다가 돌아옵니다. Tripo 피격 애니메이션 생성은 실패하여 환불되었고, 실제 적용 동작은 런타임에서 구현했습니다. 캐릭터의 텍스처 발광을 소량 추가하고 금속 광택을 줄였습니다. 흰색 블록 형태의 소환 로봇은 작은 청록색 의료 드론으로 교체했습니다.

## 로그인 없는 방과 모바일

START와 캐릭터 선택 사이에서 닉네임, 방 만들기, 방 조회를 사용할 수 있습니다. 같은 전용 서버 주소를 쓰는 사람끼리 로그인 없이 참가합니다. 방장의 캐릭터에 노란 별을 표시하며 먼저 선택된 캐릭터는 다른 사람에게 회색으로 표시되어 선택할 수 없습니다. 맵 선택과 게임 시작은 방장만 가능합니다.

전투는 전용 .NET 서버가 30Hz로 계산하고 브라우저는 15Hz 상태를 받아 표시합니다. 방장 기기의 성능이나 종료가 전투 계산 권한에 영향을 주지 않습니다. 연결 종료 후에는 다른 참가자에게 방장 권한을 넘깁니다. 모바일은 가로 화면에서 이동 키패드와 조준/발사 패드, 스킬/궁극기 버튼을 표시합니다.

새 서버 배포 준비까지 완료했으며 외부 호스팅 서비스 생성·결제·배포는 하지 않았습니다. 현재 공개 웹 URL은 AI 연습과 방 UI를 제공하고, 온라인 대전은 실제 전용 서버 주소 설정 후 사용할 수 있습니다. Docker와 Render 배포 설정, 기본 서버 주소 지정 방법은 Server/README.md를 확인하세요.
