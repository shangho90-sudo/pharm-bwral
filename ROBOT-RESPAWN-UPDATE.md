# Robot, respawn and ability audio update

- Summoner: three active robots per owner, counting support and elite together. A fourth summon replaces the owned robot with the least remaining lifetime. Other owners are unaffected. Existing HP, damage, cooldown and lifetime values are preserved.
- Respawn: two seconds of server-authoritative immunity to damage, poison application and knockback. No blocked hit event, damage stat or ultimate charge is awarded. Movement and attacks remain available. The snapshot transmits remaining protection time; character and weapon renderers blink every 0.1 seconds and restore visibility at expiry. Initial match spawn is unchanged.
- Runway: 20 independently generated skill/ultimate cues, one second for skills and two seconds for ultimates. The existing audio resource names and Unity GUIDs are retained. See ABILITY-AUDIO.json for task IDs and the 30-credit generation ledger.

Families: capsule pneumatic pop; lightning ionic crack; arrow metallic laser whistle; wave crystal pulse; burst mechanical rapid pops; artillery mortar/whistle; assassin reverse whoosh; summoner servo/chirp; fan watery sweep/chimes; poison bubbling hiss.

Validation: Server `dotnet run -- --combat-test`; Unity `SummonRespawnValidator.Build` checks all 20 unique decoded audio assets and ten blinking rigs, existing skill/ultimate presentation checks, and four-map battle checks before WebGL build.
