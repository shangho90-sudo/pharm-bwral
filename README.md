# 팽브롤 / PHARMA BRAWL

Unity **6000.6.1f1**로 만든 3D 탑다운 약사 아레나 전투 프로토타입입니다. Phase 1 전투와 AI 연습 게임을 구현했습니다. 온라인 멀티플레이는 아직 구현하지 않았습니다.

## 다른 컴퓨터에서 이어서 개발

1. Unity Hub에서 **6000.6.1f1**과 Windows Build Support, WebGL Build Support를 설치합니다.
2. `git clone https://github.com/shangho90-sudo/pharm-bwral.git`으로 저장소를 받습니다. GitHub의 **Code → Download ZIP**도 사용할 수 있습니다.
3. Unity Hub → Add → 저장소 폴더를 선택합니다. `Assets`, `Packages`, `ProjectSettings`가 있는 폴더가 Unity 프로젝트입니다.
4. 첫 import가 끝나면 `Assets/Scenes/Pharmacy.unity`를 열고 Play를 누릅니다.
5. START → 캐릭터 선택 → START 3 vs 3 순서로 게임을 실행합니다.

추가 Tripo 생성이나 로그인 없이 포함된 모델로 바로 실행할 수 있습니다. 캐릭터와 무기 모델, 텍스처, `.meta`, Humanoid Avatar와 프리팹을 함께 저장합니다. 컴퓨터를 끄면 로컬 Codex/Unity 작업은 중단됩니다. 다른 컴퓨터에서 이 README와 `DEVELOPMENT.md`를 기준으로 작업을 이어갈 수 있습니다.

## 조작 및 규칙

WASD 이동 · 마우스 조준 · 좌클릭 공격 · 우클릭/E 특수 스킬 · Space 궁극기 · Esc 일시정지.

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

Unity Build Profiles에서 Windows 또는 WebGL을 선택하거나 에디터 배치 모드의 `PrototypeBuilder.Build` / `PrototypeBuilder.BuildWeb`를 실행합니다. 두 메서드의 기본 출력은 프로젝트 상위 폴더의 `Windows` / `WebGL`입니다. `Build`는 캐릭터별 공격·스킬·궁극기, 회복, 사망/부활, 10개의 완전한 AI 경기를 검사합니다.

실행본 옵션: `--smoke-test`(헤드리스 AI 한 판), `--capture <절대 PNG 경로>`(전투), `--capture-models <절대 PNG 경로>`(10명 장착 갤러리), `--capture-lobby <경로>`(시작 화면), `--capture-result <경로>`(결과).

폰트: Noto Sans CJK KR, SIL Open Font License. 라이선스는 `Assets/Resources/Fonts/OFL.txt`에 포함되어 있습니다. 시작 화면과 캐릭터/무기 참조는 사용자가 제공한 이미지입니다.
