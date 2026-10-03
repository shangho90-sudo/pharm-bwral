# Robot aim and poison durations

The elite robot now renders its imported front axis (-X) along its authoritative firing direction (+Z after model correction). Each robot stores its own aim in the simulation and snapshot, so nearby projectiles from another summon cannot change its facing. Targets are acquired from the robot's position instead of the summoner's position. Elite movement speed is 4.5 instead of 3, with collision substeps retained; support robot movement remains 3.

Poison skill areas last 4 seconds instead of 5. Ultimate clouds last 5 seconds instead of 8. Damage, radius, cooldown and charge requirements are unchanged. Character descriptions, regeneration templates and tests use the new durations.

Validation: eight-direction model/front alignment, conflicting-projectile regression, authoritative aim matching projectile direction, elite travel speed, exact poison expiry, existing robot/respawn regressions, nine character ability validations and 40 map/hero matches.
