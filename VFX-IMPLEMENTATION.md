# 스킬·궁극기 연출 개선

Unity 6000.6.1f1. 전투 판정은 기존 ArenaSimulation.cs 및 공개 서버가 담당하며 이번 변경에는 피해 계산 코드 변경이 없습니다.

- 팽재현: 텍스처 기반 총구 섬광, 반동, 캡슐 궤적, 실제 0.9초 지연에 맞춘 충전과 낙하, 명중 섬광·캡슐 파편·충격파·잔류 연기.
- 구자현: 서버 이벤트 순서의 실제 타격 대상 사이에 번개 코어·광채·스파크.
- 방지욱: 다음 관통탄 장전 표시 및 35 거리, 반폭 0.8의 실제 판정에 맞는 레이저 중심부·외곽·경계.
- 박철호: 실제 보호 상태에만 나타나는 입체 림 보호막, 돌진 경로와 캐릭터 잔상.
- 정석호·사공민: 각 캡슐을 분리한 발사 궤적, 준비 상태와 발사 반동.
- 차동호: 실제 포격 예고와 낙하 캡슐, 개별 폭발 및 지속 구역의 피해 주기 표시.
- 김두영: 서버의 실제 출발·도착 지점과 캐릭터 메시 잔상.
- 조강희: 독립된 Tripo 지원/정예 로봇, 소환 원·출현·몸체 조준·반동 및 발사. 관절 리깅이 아닌 기계 몸체 애니메이션입니다.
- 이선용: 생성·확산·유지·소멸하는 독 연기 파티클과 실제 독 구역의 둔화·피해 표시.

기존 Tripo 캡슐과 무기 메시를 재사용했습니다. 새 로봇 2개는 승인된 총 80크레딧으로 생성했습니다. 생성 ID와 사용 에셋은 REFERENCE-ASSETS.json에 있습니다. VFX의 2x2 투명 텍스처는 image_gen으로 직접 생성했습니다.

반복 연출은 제한된 풀을 사용합니다. 연기는 구역당 최대 24개, 번개 스파크는 연결당 최대 18개입니다. 일시정지 메뉴의 화면 흔들림 ON/OFF 선택은 저장됩니다. 서버 판정이 유지되는 독/포격 구역은 서버 상태를 따라 표시하고, 경기 종료·방 이탈·일시정지에서 렌더 풀을 정리합니다.

## 검증

- Unity 컴파일 및 WebGL 빌드 성공.
- 팽재현: 직격 600+주변 300, 쿨타임 9초, 궁극기 1700, 지연 0.9초, 반경 3.5, 넉백 1.2, 충전 및 효과 종료 검증.
- 나머지 9명: 실제 공격/보호/소환/이동/지속 피해 조건과 효과 종료를 Unity 시뮬레이션에서 검증.
- 기존 4개 맵 × 10명, 총 40경기의 충돌·AI·부활·경기 종료 검증.
- 공개 서버의 별도 1:1 QA 방에서 9명 각각 실제 스킬과 피해로 충전한 궁극기 실행. 두 WebSocket 클라이언트에서 기술 전후 3개 동일 tick의 events, fighters, shots, zones, robots 일치 확인.
- Unity UI: 36개의 원형 방향, 두 개의 독립 pointerId, 이동·조준/발사 중 스킬 및 궁극기 입력 유지와 손가락별 해제 검증.
- 실제 휴대폰의 GPU 성능 및 실기기 멀티터치 검증은 실행하지 않았습니다.

주요 변경 파일: Assets/Scripts/PaengAbilityView.cs, LightningAbilityView.cs, GoldenAbilityView.cs, ReferenceAbilityView.cs, MedicalVfx.cs, CapsuleProjectile.cs, PharmacyDroneView.cs, PharmacistModelRig.cs, PharmaGame.cs, WeaponAudio.cs. 프리팹·텍스처·셰이더는 Assets/Resources/Robots, Assets/Resources/VFX, Assets/Shaders에 있습니다.

재현: Unity batchmode에서 ReferenceAbilityBuilder.FinalBuild를 실행하면 시뮬레이션·모바일 입력 검증, 동일 카메라 전후 캡처 및 WebGL 빌드를 실행합니다. 서버 동기화 검증은 프로젝트 루트에서 `node Server/reference-sync-test.mjs`를 실행합니다.

[개선 전](Docs/VFX/paeng-before.png) / [개선 후](Docs/VFX/paeng-after.png) / [Unity 렌더 영상 3초](Docs/VFX/paeng-ultimate.mp4)

모바일 360도 양쪽 패드, 기존 캐릭터, 네 개 맵, 공개 서버 주소와 카카오 공유 미리보기 설정을 유지합니다. 서버 런타임/프로토콜은 변경하지 않았습니다.
