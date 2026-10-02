# 전투 수치 조정

- 김두영 스킬 쿨타임: 7초 → 3초.
- 나머지 9명 스킬 쿨타임: 9초 → 6초. 스킬 자체의 지속시간은 유지합니다.
- 궁극기 충전 요구량: 기존 값의 70%. 박철호 3,300 → 2,310, 나머지 4,000 → 2,800.
- 조강희 지원 로봇 HP 800, 정예 로봇 HP 1,600. 체력바, 피격, 파괴 연출 추가. 아군 공격은 무시하고, 파괴 시 점수·킬·궁극기 충전을 지급하지 않습니다.
- 소환물은 투사체·관통탄·레이저·충격파·번개·폭발·지속 피해 구역으로 공격할 수 있습니다. 기존 소환 지속시간/이동속도/공격력은 유지합니다.
- 서버의 공통 ArenaSimulation 및 characters.json과 Unity ScriptableObject를 함께 변경했습니다. 로봇 HP/maxHp는 기존 스냅샷의 Robot 구조로 동기화합니다.

검증: BalanceValidator에서 10명 쿨타임/궁극기 문턱, 로봇 피해/파괴/아군 공격 제외/풀 재사용 초기화/관통탄 중복 적중 방지/점수 제외를 확인합니다. 기존 캐릭터 기술, 모바일 입력, 4개 맵 총 40경기 검증도 실행합니다. Server/reference-sync-test.mjs는 두 클라이언트의 동일 tick 상태 및 지원 로봇 피해·제거와 정예 로봇 HP를 검증합니다.

주요 변경: Assets/Scripts/ArenaSimulation.cs, PharmacyDroneView.cs, ReferenceAbilityView.cs, CharacterDefinition.cs, Assets/Resources/Characters/*.asset, Assets/Editor/PrototypeBuilder.cs, BalanceValidator.cs, Server/characters.json, Program.cs, reference-sync-test.mjs.
