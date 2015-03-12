UnitBlueprint {
	AI = {
		GuardRadius = "int"
		GuardScanRadius = "int",
		NeedUnpack = "bool",
		TargetBones = "list(string)",
	},
	Audio = "map(string -> Sound)",
	Buffs = {
		Regen = {
			Level1 = "int",
			Level2 = "int",
			Level3 = "int",
			Level4 = "int",
			Level5 = "int",
		},
	},
	BuildIconSortPriority = "int",
	Categories = "list(string)",
	Defense = {
		AirThreatLevel = "int",
		ArmorType = "string",
		EconomyThreatLevel = "int",
		Health = "int",
		MaxHealth = "int",
		RegenRate = "int",
		SubThreatLevel = "int",
		SurfaceThreatLevel = "int",
	},
	Description = "string",
	Display = {
		Abilities = "list(string)",
		AnimationOpen = "string",
		AnimationWalk = "string",
		AttackReticleSize = "int",
		DamageEffects = "list(DamageEffect)",
		Mesh = {
			IconFadeInZoom = "int",
			LODs = "list(LOD)",
		},
	},
}