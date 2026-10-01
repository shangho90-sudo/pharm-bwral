# 팽브롤 / PHARMA BRAWL

Unity **6000.6.1f1**로 만든 3D 탑다운 약사 아레나 슈팅 게임입니다. 캐릭터 10명과 AI 3 vs 3 전투, 첨부 이미지에서 재구성한 4개의 선택 가능한 맵을 포함합니다. 온라인 멀티플레이는 아직 구현하지 않았습니다.

## 다른 컴퓨터에서 이어서 개발

1. Unity Hub에서 **6000.6.1f1**과 Windows Build Support, WebGL Build Support를 설치합니다.
2. `git clone https://github.com/shangho90-sudo/pharm-bwral.git`으로 저장소를 받습니다. GitHub의 **Code → Download ZIP**도 사용할 수 있습니다.
3. Unity Hub → Add → 저장소 폴더를 선택합니다. `Assets`, `Packages`, `ProjectSettings`가 있는 폴더가 Unity 프로젝트입니다.
4. 첫 import가 끝나면 `Assets/Scenes/Pharmacy.unity`를 열고 Play를 누릅니다.
5. START → 캐릭터와 맵 선택 → START 3 vs 3 순서로 게임을 실행합니다.

추가 Tripo 생성이나 로그인 없이 포함된 모델로 바로 실행할 수 있습니다. 캐릭터와 무기 모델, 텍스처, `.meta`, Humanoid Avatar와 프리팹을 함께 저장합니다. 컴퓨터를 끄면 로컬 Codex/Unity 작업은 중단됩니다. 다른 컴퓨터에서 이 README와 `DEVELOPMENT.md`를 기준으로 작업을 이어갈 수 있습니다.

## 조작 및 규칙

WASD 이동 · 마우스 조준 · 좌클릭 공격 · 우클릭/E 특수 스킬 · Space 궁극기 · Esc 일시정지.

**F2**는 이동 판정 표시를 켜고 끕니다. 녹색 점은 캐릭터 반경을 고려한 이동 가능 위치, 빨간 점은 이동 불가 위치입니다. 화면 우측 상단에서 음악을 켜거나 끌 수 있습니다.

## 네 가지 아레나

기존 고정 약국 맵을 제거했습니다. 네 맵은 `ArenaMap`의 같은 구조물 데이터로 3D 외형과 이동/탄환 판정을 만듭니다. 첨부 이미지를 평면 배경으로 사용하지 않고, 실제 3D 전투 구역으로 재구성했습니다. 외형과 배치는 게임에 맞게 조정한 것으로 참조 이미지의 픽셀 단위 복제는 아닙니다.

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

3 vs 3, AI 5명 자동 참가. 3분 또는 먼저 20킬에 도달한 팀이 승리합니다. 3.5초 동안 공격/피격이 없으면 회복하며, 사망 후 4초 뒤 부활합니다. 10명 모두 서로 다른 일반 공격, 스킬, 궁극기를 가집니다. 현재 게임 모드는 팀 데스매치입니다.

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
