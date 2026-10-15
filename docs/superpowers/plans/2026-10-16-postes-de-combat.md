# Postes de combat d'escouade — plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Chaque membre d'une escouade enregistre une case par poste (4 emplacements), puis l'escouade y retourne en un clic depuis la carte d'ordres ou un raccourci.

**Architecture:** Les données vivent dans `BarSquads` (déjà sauvegardé) : poste actif par escouade, en liste parallèle à `names`, et postes par pion dans un dictionnaire sauvegardé en quatre listes plates. Une classe statique `SquadStations` porte les actions (enregistrer, aller, effacer avec confirmation) et l'aperçu sur la carte, dessiné depuis `BarSquads.GameComponentUpdate`. La carte d'ordres gagne une section, les raccourcis un septième binding.

**Tech Stack:** C# `net472`, RimWorld 1.6 (`GenDraw`, `JobDefOf.Goto`, `Dialog_MessageBox`), Radius UI Framework (`Palette`), helpers de la barre (`RcbSpatial`, `RcbFont`, `RcbPalette`).

**Spec:** `docs/superpowers/specs/2026-10-16-postes-de-combat-design.md`

**Note d'exécution (2026-10-16) :** après la relecture finale, `SquadStationCount` a été remplacé par `BarSquads.HasLiveStation` et `SquadStatus.State.stationMask` ; `Hover` reçoit la liste des membres ; un membre sans poste voit son ordre joueur interrompu ; le raccourci refuse une escouade sans poste sur la carte (`RCB_Stations_None`). Le code fait foi.

## Global Constraints

- Aucune ligne de [LTO] Colony Groups reprise.
- Namespace `RadiusColonistBar`, DLL `RadiusColonistBar.dll`, Harmony `astryl.radiusui.colonistbar`, noms de classes inchangés.
- Clés de sauvegarde neuves préfixées `mcb` : `mcbActiveStation`, `mcbStationPawn`, `mcbStationSlot`, `mcbStationMap`, `mcbStationCell`. Ne plus jamais les renommer après publication.
- Pas de `Lord`, `DutyDef`, `MapComponent`, ni nouveau `GameComponent`.
- defName du raccourci : `RCB_SquadGoToStation`, à ne plus renommer après publication.
- Style ILSpy du code existant (`num`, `flag`, `get`…), commentaires en anglais seulement sur le code ajouté.
- Tout texte visible passe par une clé `RCB_Stations_*` : anglais dans `Languages/English/Keyed/RadiusColonistBar.xml`, français dans `Languages/French/Keyed/RadiusColonistBarPlus.xml`.
- Pas de tests automatisés dans le dépôt. Preuve de chaque tâche : `./deploy.sh` depuis `RadiusUIColonistBarPlus/` (attendu : `0 Erreur(s)`, `6 Avertissement(s)` comme avant ce lot, ligne `Publié :`), puis les contrôles en jeu de la tâche. Pas de `git commit` sans demande explicite du joueur (`CLAUDE.md`) : les étapes « commit » préparent le message, le joueur décide.

## Review Focus

1. Partie sauvegardée avant ce lot : `mcbActiveStation` absent, liste nulle. Attendu : poste actif 1 pour chaque escouade, aucune exception. Contrôle : tâche 1, étape 4.
2. Escouade dissoute ou déplacée : le poste actif des autres escouades suit leur escouade, pas leur index. Contrôle : tâche 1, étape 4.
3. Case enregistrée devenue impraticable (mur construit) : le pion va à la case libre la plus proche, sans erreur. Contrôle : tâche 2, étape 4.
4. Carte d'ordres fermée pendant un survol : l'aperçu disparaît à l'image suivante, il ne reste pas affiché. Contrôle : tâche 3, étape 5.
5. Membres sur une autre carte que celle affichée : leurs postes ne s'affichent pas et ne comptent pas ; « Aller au poste » est grisé si personne n'a de poste sur la carte affichée. Contrôle : tâche 3, étape 5.

---

### Task 1 : données des postes dans `BarSquads`

**Files:**
- Modify: `Source/RadiusColonistBar/BarSquads.cs` (champs après `cohortTab` l. 28 ; `ExposeData` l. 66-132 ; `NewSquad` l. 210 ; `Disband` l. 219 ; `MoveSquad` l. 258)

**Interfaces:**
- Produces :
  - `public const int StationSlots = 4;`
  - `public List<int> activeStation`
  - `public int ActiveStation(int i)` : 0-3, 0 si index invalide.
  - `public void SetActiveStation(int i, int slot)`
  - `public bool StationOn(Pawn p, int slot, Map map, out IntVec3 cell)` : vrai seulement si le poste existe **sur cette carte**.
  - `public void SetStation(Pawn p, int slot, Map map, IntVec3 cell)`
  - `public int SquadStationCount(int i, int slot)` : membres de l'escouade `i` (toutes cartes, via `squadOf`) qui ont ce poste.
  - `public void ClearSquadStation(int i, int slot)`
  - `public override void GameComponentUpdate()` : appelle `SquadStations.DrawHover()` (classe créée en tâche 2 ; jusque-là, laisser le corps vide).

- [ ] **Step 1 : ajouter les champs**

Après `public int cohortTab;` :

```csharp
	// Battle stations (Colonist Bar Plus): the active station slot (0-3) of each squad, parallel to names.
	public List<int> activeStation = new List<int>();

	public const int StationSlots = 4;

	// Battle stations belong to the pawn, not the squad: disbanding or moving a squad shifts nothing.
	// Key: thingIDNumber * StationSlots + slot.
	private Dictionary<long, Station> stations = new Dictionary<long, Station>();

	private List<int> stationPawnScratch;

	private List<int> stationSlotScratch;

	private List<int> stationMapScratch;

	private List<IntVec3> stationCellScratch;

	private struct Station
	{
		public int map;

		public IntVec3 cell;
	}
```

- [ ] **Step 2 : sauvegarde**

Dans `ExposeData`, juste avant `if (Scribe.mode == LoadSaveMode.PostLoadInit)` :

```csharp
		Scribe_Collections.Look(ref activeStation, "mcbActiveStation", LookMode.Value);
		ExposeStations();
```

Dans le bloc `PostLoadInit`, après la boucle `while (icons.Count < names.Count)` :

```csharp
			if (activeStation == null)
			{
				activeStation = new List<int>();
			}
			if (activeStation.Count > names.Count)
			{
				activeStation.RemoveRange(names.Count, activeStation.Count - names.Count);
			}
			while (activeStation.Count < names.Count)
			{
				activeStation.Add(0);
			}
			for (int i = 0; i < activeStation.Count; i++)
			{
				if (activeStation[i] < 0 || activeStation[i] >= StationSlots)
				{
					activeStation[i] = 0;
				}
			}
```

Nouvelle méthode, après `ExposeData` :

```csharp
	// Saved as four flat lists. Stations of a map that no longer exists are dropped here, and only here: a check on the
	// pawn would miss pawns away from every map (quests, kidnapping) and wipe their stations.
	private void ExposeStations()
	{
		if (Scribe.mode == LoadSaveMode.Saving)
		{
			stationPawnScratch = new List<int>();
			stationSlotScratch = new List<int>();
			stationMapScratch = new List<int>();
			stationCellScratch = new List<IntVec3>();
			HashSet<int> hashSet = new HashSet<int>();
			List<Map> maps = Find.Maps;
			if (maps != null)
			{
				for (int i = 0; i < maps.Count; i++)
				{
					hashSet.Add(maps[i].uniqueID);
				}
			}
			foreach (KeyValuePair<long, Station> station in stations)
			{
				if (hashSet.Contains(station.Value.map))
				{
					stationPawnScratch.Add((int)(station.Key / StationSlots));
					stationSlotScratch.Add((int)(station.Key % StationSlots));
					stationMapScratch.Add(station.Value.map);
					stationCellScratch.Add(station.Value.cell);
				}
			}
		}
		Scribe_Collections.Look(ref stationPawnScratch, "mcbStationPawn", LookMode.Value);
		Scribe_Collections.Look(ref stationSlotScratch, "mcbStationSlot", LookMode.Value);
		Scribe_Collections.Look(ref stationMapScratch, "mcbStationMap", LookMode.Value);
		Scribe_Collections.Look(ref stationCellScratch, "mcbStationCell", LookMode.Value);
		if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			stations = new Dictionary<long, Station>();
			// Missing or mismatched lists (older save, hand-edited file): no station rather than a wrong one.
			if (stationPawnScratch != null && stationSlotScratch != null && stationMapScratch != null && stationCellScratch != null && stationSlotScratch.Count == stationPawnScratch.Count && stationMapScratch.Count == stationPawnScratch.Count && stationCellScratch.Count == stationPawnScratch.Count)
			{
				for (int j = 0; j < stationPawnScratch.Count; j++)
				{
					int num = stationSlotScratch[j];
					if (num >= 0 && num < StationSlots)
					{
						stations[StationKey(stationPawnScratch[j], num)] = new Station
						{
							map = stationMapScratch[j],
							cell = stationCellScratch[j]
						};
					}
				}
			}
		}
		if (Scribe.mode == LoadSaveMode.Saving || Scribe.mode == LoadSaveMode.LoadingVars)
		{
			stationPawnScratch = null;
			stationSlotScratch = null;
			stationMapScratch = null;
			stationCellScratch = null;
		}
	}
```

- [ ] **Step 3 : accesseurs et index d'escouade**

Dans `NewSquad`, après `icons.Add((names.Count - 1) % 8);` :

```csharp
		while (activeStation.Count < names.Count)
		{
			activeStation.Add(0);
		}
```

Dans `Disband`, après le bloc `if (i < icons.Count) { icons.RemoveAt(i); }` :

```csharp
		if (i < activeStation.Count)
		{
			activeStation.RemoveAt(i);
		}
```

Dans `MoveSquad`, après le bloc qui échange `icons` :

```csharp
		if (i < activeStation.Count && num < activeStation.Count)
		{
			int value7 = activeStation[num];
			activeStation[num] = activeStation[i];
			activeStation[i] = value7;
		}
```

Nouvelles méthodes, après `SetIcon` :

```csharp
	private static long StationKey(int pawnId, int slot)
	{
		return (long)pawnId * StationSlots + slot;
	}

	public int ActiveStation(int i)
	{
		return (i >= 0 && i < activeStation.Count) ? activeStation[i] : 0;
	}

	public void SetActiveStation(int i, int slot)
	{
		if (i >= 0 && i < activeStation.Count && slot >= 0 && slot < StationSlots)
		{
			activeStation[i] = slot;
		}
	}

	// True only for a station saved on this map: a station elsewhere means nothing here.
	public bool StationOn(Pawn p, int slot, Map map, out IntVec3 cell)
	{
		cell = IntVec3.Invalid;
		if (p == null || map == null || !stations.TryGetValue(StationKey(p.thingIDNumber, slot), out var value) || value.map != map.uniqueID)
		{
			return false;
		}
		cell = value.cell;
		return true;
	}

	public void SetStation(Pawn p, int slot, Map map, IntVec3 cell)
	{
		if (p != null && map != null && slot >= 0 && slot < StationSlots)
		{
			stations[StationKey(p.thingIDNumber, slot)] = new Station
			{
				map = map.uniqueID,
				cell = cell
			};
		}
	}

	// Members on every map: squadOf holds them by id, so absent pawns count without a Pawn.
	public int SquadStationCount(int i, int slot)
	{
		int num = 0;
		foreach (KeyValuePair<int, int> item in squadOf)
		{
			if (item.Value == i && stations.ContainsKey(StationKey(item.Key, slot)))
			{
				num++;
			}
		}
		return num;
	}

	public void ClearSquadStation(int i, int slot)
	{
		foreach (KeyValuePair<int, int> item in squadOf)
		{
			if (item.Value == i)
			{
				stations.Remove(StationKey(item.Key, slot));
			}
		}
	}

	public override void GameComponentUpdate()
	{
	}
```

- [ ] **Step 4 : build et contrôle**

Run : `cd /Users/davidbaudoin/Documents/GitHub/Rimworld-mods/RadiusUIColonistBarPlus && ./deploy.sh 2>&1 | tail -5`
Expected : `0 Erreur(s)`, `6 Avertissement(s)`, `Publié :`.

Contrôle en jeu (aucune interface encore) : charger une partie faite avant ce lot, avec au moins deux escouades. Attendu : aucune erreur dans le journal. Sauvegarder, ouvrir le `.rws` : nœud `<mcbActiveStation>` avec un `0` par escouade. Dissoudre la première escouade, sauvegarder : un `0` de moins.

- [ ] **Step 5 : commit (sur demande du joueur)**

```bash
git add Source/RadiusColonistBar/BarSquads.cs
git commit -m "Battle stations: saved data in BarSquads"
```

---

### Task 2 : actions `SquadStations` (enregistrer, aller, effacer)

**Files:**
- Create: `Source/RadiusColonistBar/SquadStations.cs`
- Modify: `Source/RadiusColonistBar/SquadOrders.cs:190` (`Try` passe de `private` à `internal`)
- Modify: `Source/RadiusColonistBar/BarSquads.cs` (corps de `GameComponentUpdate`)
- Modify: `Languages/English/Keyed/RadiusColonistBar.xml`, `Languages/French/Keyed/RadiusColonistBarPlus.xml`

**Interfaces:**
- Consumes : tâche 1 (`ActiveStation`, `StationOn`, `SetStation`, `SquadStationCount`, `ClearSquadStation`, `StationSlots`) ; `SquadMembers.Able(Map, int)` → `List<Pawn>` ; `SquadAssault.Get?.End(Pawn)` ; `SquadOrders.Try(Pawn, Action)` ; `SquadStatus.Invalidate()`.
- Produces :
  - `internal static int CountOnMap(List<Pawn> members, BarSquads squads, int slot, Map map)`
  - `internal static void Save(int index)`
  - `internal static void Go(int index)`
  - `internal static void AskClear(int index)`
  - `internal static void Hover(int index, int slot)`
  - `internal static void DrawHover()`

- [ ] **Step 1 : rendre `SquadOrders.Try` accessible**

Dans `SquadOrders.cs`, remplacer `private static void Try(Pawn pawn, Action action)` par `internal static void Try(Pawn pawn, Action action)`.

- [ ] **Step 2 : écrire `SquadStations.cs`**

```csharp
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RadiusColonistBar;

// Battle stations (Colonist Bar Plus): each member of a squad saves a cell per station slot, then runs back to it on
// order. Stations are kept per pawn in BarSquads; a squad only keeps its active slot.
internal static class SquadStations
{
	private static int hoverIndex = -1;

	private static int hoverSlot = -1;

	private static int hoverFrame = -1;

	private static readonly List<IntVec3> edge = new List<IntVec3>(1);

	internal static int CountOnMap(List<Pawn> members, BarSquads squads, int slot, Map map)
	{
		int num = 0;
		for (int i = 0; i < members.Count; i++)
		{
			if (squads.StationOn(members[i], slot, map, out var _))
			{
				num++;
			}
		}
		return num;
	}

	// Every able member takes its current cell, whatever is selected. Absent or downed members keep their old one.
	internal static void Save(int index)
	{
		BarSquads get = BarSquads.Get;
		Map currentMap = Find.CurrentMap;
		if (get == null || currentMap == null)
		{
			return;
		}
		List<Pawn> list = SquadMembers.Able(currentMap, index);
		if (list.Count == 0)
		{
			return;
		}
		int num = get.ActiveStation(index);
		for (int i = 0; i < list.Count; i++)
		{
			get.SetStation(list[i], num, currentMap, list[i].Position);
		}
		Messages.Message("RCB_Stations_Saved".Translate(num + 1, list.Count), MessageTypeDefOf.SilentInput, historical: false);
	}

	internal static void Go(int index)
	{
		BarSquads get = BarSquads.Get;
		Map map = Find.CurrentMap;
		if (get == null || map == null)
		{
			return;
		}
		int slot = get.ActiveStation(index);
		List<Pawn> list = SquadMembers.Able(map, index);
		foreach (Pawn pawn in list)
		{
			SquadOrders.Try(pawn, delegate
			{
				pawn.drafter.Drafted = true;
				SquadAssault.Get?.End(pawn);
				// A member without a station here is drafted and stays put.
				if (get.StationOn(pawn, slot, map, out var cell) && cell.InBounds(map))
				{
					// A wall built on the cell since: the nearest free cell instead.
					IntVec3 intVec = (cell.Standable(map) ? cell : RCellFinder.BestOrderedGotoDestNear(cell, pawn));
					if (intVec.IsValid)
					{
						Job job = JobMaker.MakeJob(JobDefOf.Goto, intVec);
						job.locomotionUrgency = LocomotionUrgency.Sprint;
						pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
					}
				}
			});
		}
		SquadStatus.Invalidate();
	}

	// No undo: clearing wipes the slot for every member on every map, so it asks first. The slot is read now, the
	// dialog is modal, so the squad index cannot shift before the answer.
	internal static void AskClear(int index)
	{
		BarSquads get = BarSquads.Get;
		if (get == null)
		{
			return;
		}
		int slot = get.ActiveStation(index);
		if (get.SquadStationCount(index, slot) == 0)
		{
			return;
		}
		Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("RCB_Stations_ClearConfirm".Translate(slot + 1, get.SquadName(index)), delegate
		{
			BarSquads get2 = BarSquads.Get;
			if (get2 != null)
			{
				get2.ClearSquadStation(index, slot);
				Messages.Message("RCB_Stations_Cleared".Translate(slot + 1), MessageTypeDefOf.SilentInput, historical: false);
			}
		}, destructive: true));
	}

	internal static void Hover(int index, int slot)
	{
		hoverIndex = index;
		hoverSlot = slot;
		hoverFrame = Time.frameCount;
	}

	// Called from BarSquads.GameComponentUpdate. The card sets the hover in OnGUI, which runs after Update in a frame:
	// a hover from the previous frame still counts, an older one means the card is gone or the mouse left.
	internal static void DrawHover()
	{
		if (hoverFrame < Time.frameCount - 1 || WorldRendererUtility.WorldSelected)
		{
			return;
		}
		BarSquads get = BarSquads.Get;
		Map currentMap = Find.CurrentMap;
		if (get == null || currentMap == null)
		{
			return;
		}
		List<Pawn> list = SquadMembers.Able(currentMap, hoverIndex);
		Color color = get.SquadColor(hoverIndex);
		for (int i = 0; i < list.Count; i++)
		{
			if (get.StationOn(list[i], hoverSlot, currentMap, out var cell) && cell.InBounds(currentMap))
			{
				edge.Clear();
				edge.Add(cell);
				GenDraw.DrawFieldEdges(edge, color);
				GenDraw.DrawLineBetween(list[i].DrawPos, cell.ToVector3Shifted(), AltitudeLayer.MetaOverlays.AltitudeFor());
			}
		}
	}
}
```

- [ ] **Step 3 : brancher le dessin et ajouter les textes**

Dans `BarSquads.cs`, remplacer le corps vide de `GameComponentUpdate` :

```csharp
	public override void GameComponentUpdate()
	{
		try
		{
			SquadStations.DrawHover();
		}
		catch (Exception ex)
		{
			// Runs every frame: one log line, not one per frame.
			Log.ErrorOnce("[Radius UI - Colonist Bar] Battle station preview failed: " + ex, 0x52435354);
		}
	}
```

Ajouter `using System;` en tête de `BarSquads.cs` s'il manque.

Anglais, `Languages/English/Keyed/RadiusColonistBar.xml`, après `<RCB_Hotkey_NoSquad>No squad selected</RCB_Hotkey_NoSquad>` :

```xml
  <RCB_Stations_Title>BATTLE STATIONS</RCB_Stations_Title>
  <RCB_Stations_Go>Go to station</RCB_Stations_Go>
  <RCB_Stations_GoTip>Drafts the squad, ends its assault and sends each member running to their station {0}. Members without a station {0} on this map are drafted and stay where they are.</RCB_Stations_GoTip>
  <RCB_Stations_Save>Save</RCB_Stations_Save>
  <RCB_Stations_SaveTip>Saves where every member on this map stands now as station {0}. Members who are absent, downed or in a mental break keep their previous station.</RCB_Stations_SaveTip>
  <RCB_Stations_Clear>Clear</RCB_Stations_Clear>
  <RCB_Stations_ClearTip>Clears station {0} for every member of the squad, on every map. Asks for confirmation.</RCB_Stations_ClearTip>
  <RCB_Stations_PipTip>Station {0}: {1} members saved on this map.\n\nClick to make it the active station.</RCB_Stations_PipTip>
  <RCB_Stations_Saved>Station {0} saved: {1} pawns</RCB_Stations_Saved>
  <RCB_Stations_ClearConfirm>Clear station {0} of squad {1}? This cannot be undone.</RCB_Stations_ClearConfirm>
  <RCB_Stations_Cleared>Station {0} cleared</RCB_Stations_Cleared>
```

Français, `Languages/French/Keyed/RadiusColonistBarPlus.xml`, après `<RCB_Hotkey_NoSquad>Aucune escouade sélectionnée</RCB_Hotkey_NoSquad>` :

```xml
  <!-- Postes de combat -->
  <RCB_Stations_Title>POSTES DE COMBAT</RCB_Stations_Title>
  <RCB_Stations_Go>Aller au poste</RCB_Stations_Go>
  <RCB_Stations_GoTip>Mobilise l'escouade, met fin à son assaut et envoie chaque membre en courant à son poste {0}. Les membres sans poste {0} sur cette carte sont mobilisés et restent sur place.</RCB_Stations_GoTip>
  <RCB_Stations_Save>Enregistrer</RCB_Stations_Save>
  <RCB_Stations_SaveTip>Enregistre la position actuelle de chaque membre présent sur cette carte comme poste {0}. Les membres absents, à terre ou en crise gardent leur poste précédent.</RCB_Stations_SaveTip>
  <RCB_Stations_Clear>Effacer</RCB_Stations_Clear>
  <RCB_Stations_ClearTip>Efface le poste {0} de tous les membres de l'escouade, sur toutes les cartes. Demande confirmation.</RCB_Stations_ClearTip>
  <RCB_Stations_PipTip>Poste {0} : {1} membres enregistrés sur cette carte.\n\nCliquer pour en faire le poste actif.</RCB_Stations_PipTip>
  <RCB_Stations_Saved>Poste {0} enregistré : {1} pions</RCB_Stations_Saved>
  <RCB_Stations_ClearConfirm>Effacer le poste {0} de l'escouade {1} ? Action définitive.</RCB_Stations_ClearConfirm>
  <RCB_Stations_Cleared>Poste {0} effacé</RCB_Stations_Cleared>
```

- [ ] **Step 4 : build et contrôle**

Run : `cd /Users/davidbaudoin/Documents/GitHub/Rimworld-mods/RadiusUIColonistBarPlus && ./deploy.sh 2>&1 | tail -5`
Expected : `0 Erreur(s)`, `6 Avertissement(s)`, `Publié :`.

Pas encore de bouton : le contrôle en jeu des actions (dont la case murée, Review Focus 3) se fait en tâche 3, étape 5.

- [ ] **Step 5 : commit (sur demande du joueur)**

```bash
git add Source/RadiusColonistBar/SquadStations.cs Source/RadiusColonistBar/SquadOrders.cs Source/RadiusColonistBar/BarSquads.cs Languages
git commit -m "Battle stations: save, go, clear with confirmation, map preview"
```

---

### Task 3 : section « Postes de combat » dans la carte d'ordres

**Files:**
- Modify: `Source/RadiusColonistBar/SquadOrderCard.cs` (constantes l. 16-26 ; `ContentHeight` l. 117-127 ; `DoWindowContents` l. 186-187 ; nouvelles méthodes avant `DrawHeader`)

**Interfaces:**
- Consumes : tâche 1 (`StationSlots`, `ActiveStation`, `SetActiveStation`, `SquadStationCount`) ; tâche 2 (`CountOnMap`, `Save`, `Go`, `AskClear`, `Hover`) ; `SquadMembers.Able`.
- Produces : rien pour les autres tâches.

- [ ] **Step 1 : constante et hauteur**

Ajouter `using System.Collections.Generic;` en tête du fichier.

Après `private const float Gap = 4f;` :

```csharp
	// Station slot pips (Colonist Bar Plus).
	private const float PipH = 24f;
```

Dans `ContentHeight`, remplacer :

```csharp
		// Header 24 + state 20 + 2 rows of orders + assault title 18 + assault row, plus the footer when managed.
		float num = Pad + 24f + 20f + 8f + (BtnH * 2f + Gap) + 10f + 18f + BtnH + Pad;
```

par :

```csharp
		// Header 24 + state 20 + 2 rows of orders + stations (title 18, pips, buttons) + assault title 18 + assault row,
		// plus the footer when managed.
		float num = Pad + 24f + 20f + 8f + (BtnH * 2f + Gap) + 10f + (18f + PipH + Gap + BtnH) + 10f + 18f + BtnH + Pad;
```

- [ ] **Step 2 : insérer la section**

Dans `DoWindowContents`, remplacer :

```csharp
		y += BtnH + 10f;
		RcbPalette.MicroLabel(new Rect(x, y, num, 18f), "RCB_Card_AssaultTitle".Translate(), RcbPalette.TextDim2, TextAnchor.MiddleLeft, shadow: false);
```

par :

```csharp
		y += BtnH + 10f;
		if (DrawStations(x, ref y, num, get))
		{
			return;
		}
		y += 10f;
		RcbPalette.MicroLabel(new Rect(x, y, num, 18f), "RCB_Card_AssaultTitle".Translate(), RcbPalette.TextDim2, TextAnchor.MiddleLeft, shadow: false);
```

- [ ] **Step 3 : écrire `DrawStations` et `Pip`**

Avant `private void DrawHeader(` :

```csharp
	// Battle stations (Colonist Bar Plus): slot pips, then Go / Save / Clear on the active slot. One Able call for the
	// whole section: it sorts and reads the bar, five calls a frame would add up. Returns true when the card closed.
	private bool DrawStations(float x, ref float y, float w, BarSquads squads)
	{
		Map currentMap = Find.CurrentMap;
		List<Pawn> list = SquadMembers.Able(currentMap, index);
		int num = squads.ActiveStation(index);
		RcbPalette.MicroLabel(new Rect(x, y, w, 18f), "RCB_Stations_Title".Translate(), RcbPalette.TextDim2, TextAnchor.MiddleLeft, shadow: false);
		y += 18f;
		float num2 = (w - Gap * 3f) / 4f;
		for (int i = 0; i < BarSquads.StationSlots; i++)
		{
			Rect r = new Rect(x + (num2 + Gap) * (float)i, y, num2, PipH);
			if (Mouse.IsOver(r))
			{
				SquadStations.Hover(index, i);
			}
			if (Pip(r, i, SquadStations.CountOnMap(list, squads, i, currentMap), i == num))
			{
				squads.SetActiveStation(index, i);
			}
		}
		y += PipH + Gap;
		float num3 = (w - Gap * 2f) / 3f;
		Rect rect = new Rect(x, y, num3, BtnH);
		if (Mouse.IsOver(rect))
		{
			SquadStations.Hover(index, num);
		}
		if (Button(rect, null, "RCB_Stations_Go".Translate(), "RCB_Stations_GoTip".Translate(num + 1), SquadStations.CountOnMap(list, squads, num, currentMap) > 0, active: false))
		{
			SquadStations.Go(index);
		}
		if (Button(new Rect(x + num3 + Gap, y, num3, BtnH), null, "RCB_Stations_Save".Translate(), "RCB_Stations_SaveTip".Translate(num + 1), list.Count > 0, active: false))
		{
			SquadStations.Save(index);
		}
		if (Button(new Rect(x + (num3 + Gap) * 2f, y, num3, BtnH), null, "RCB_Stations_Clear".Translate(), "RCB_Stations_ClearTip".Translate(num + 1), squads.SquadStationCount(index, num) > 0, active: false))
		{
			// The card sits above dialogs: left open, it would cover the confirmation and keep its input.
			Close();
			SquadStations.AskClear(index);
			return true;
		}
		y += BtnH;
		return false;
	}

	// Active slot: accent. Slot saved on this map for at least one present member: filled. Otherwise: faint.
	private static bool Pip(Rect r, int slot, int saved, bool active)
	{
		bool flag = Mouse.IsOver(r);
		Color c = (active ? RcbPalette.Accent : Color.Lerp(RcbPalette.Ink, Color.white, (saved > 0) ? (flag ? 0.2f : 0.12f) : (flag ? 0.08f : 0.03f)));
		RcbSpatial.Rounded(r, c);
		Color col = (active ? Palette.Flat.Rail : ((saved > 0) ? RcbPalette.Stat : RcbPalette.TextDim2));
		RcbFont.Label(r, (slot + 1).ToString(), bold: true, GameFont.Small, col, TextAnchor.MiddleCenter);
		if (flag)
		{
			TooltipHandler.TipRegion(r, "RCB_Stations_PipTip".Translate(slot + 1, saved));
		}
		if (Widgets.ButtonInvisible(r))
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			return true;
		}
		return false;
	}
```

- [ ] **Step 4 : build**

Run : `cd /Users/davidbaudoin/Documents/GitHub/Rimworld-mods/RadiusUIColonistBarPlus && ./deploy.sh 2>&1 | tail -5`
Expected : `0 Erreur(s)`, `6 Avertissement(s)`, `Publié :`.

- [ ] **Step 5 : contrôle en jeu**

Une escouade de trois pions, deux escouades au total.
1. Ouvrir la carte d'ordres : section « POSTES DE COMBAT » entre Regroupement et Assaut, rien de coupé en bas ; pastille 1 en accent ; « Aller au poste » et « Effacer » grisés.
2. Enregistrer : message « Poste 1 enregistré : 3 pions » ; pastille 1 pleine.
3. Déplacer les pions, survoler la pastille 1 puis « Aller au poste » : contour de chaque case et ligne pion-case. Fermer la carte pendant le survol : l'aperçu disparaît aussitôt (Review Focus 4).
4. « Aller au poste » : chacun mobilisé, en sprint, jusqu'à sa case.
5. Lancer un assaut, puis « Aller au poste » : épée retirée du portrait et de l'onglet, les pions vont au poste.
6. Construire un mur sur une case enregistrée, « Aller au poste » : le pion s'arrête à côté, aucune erreur (Review Focus 3).
7. Pastille 2 : vide, « Aller au poste » et « Effacer » grisés.
8. Pastille 1, « Effacer » : la carte se ferme, la confirmation s'ouvre ; Annuler garde les postes ; recommencer et Confirmer : message « Poste 1 effacé », pastille 1 vide.
9. Afficher une autre carte (seconde colonie ou carte de quête) : pastilles vides, « Aller au poste » grisé (Review Focus 5).
10. Sauvegarder, recharger : postes et poste actif conservés.
11. Dissoudre l'escouade du dessus depuis le Centre de commandement : le poste actif de l'autre escouade ne change pas (Review Focus 2).

- [ ] **Step 6 : commit (sur demande du joueur)**

```bash
git add Source/RadiusColonistBar/SquadOrderCard.cs
git commit -m "Battle stations section in the squad order card"
```

---

### Task 4 : raccourci « Aller au poste actif »

**Files:**
- Modify: `Defs/KeyBindings_Squads.xml` (après `RCB_SquadAssaultFleeing`)
- Modify: `Source/RadiusColonistBar/SquadHotkeys.cs` (champs, `OnGUI`, `Bind`)
- Modify: `Languages/French/DefInjected/KeyBindingDef/Squads.xml`

**Interfaces:**
- Consumes : `SquadStations.Go(int index)` (tâche 2).
- Produces : `KeyBindingDef` `RCB_SquadGoToStation`.

- [ ] **Step 1 : def et traduction**

Dans `Defs/KeyBindings_Squads.xml`, avant `</Defs>` :

```xml
  <KeyBindingDef>
    <defName>RCB_SquadGoToStation</defName>
    <label>go to the active battle station</label>
    <category>RCB_Squads</category>
  </KeyBindingDef>
```

Dans `Languages/French/DefInjected/KeyBindingDef/Squads.xml`, avant `</LanguageData>` :

```xml
  <RCB_SquadGoToStation.label>aller au poste de combat actif</RCB_SquadGoToStation.label>
```

- [ ] **Step 2 : lire la touche**

Dans `SquadHotkeys.cs`, après `private static KeyBindingDef fleeing;` :

```csharp
	private static KeyBindingDef station;
```

Dans `OnGUI`, après le bloc `else if (Pressed(fleeing)) { ... }` :

```csharp
		else if (Pressed(station))
		{
			Run(SquadStations.Go);
		}
```

Dans `Bind`, après la ligne `fleeing = ...` :

```csharp
			station = DefDatabase<KeyBindingDef>.GetNamedSilentFail("RCB_SquadGoToStation");
```

- [ ] **Step 3 : build et contrôle**

Run : `cd /Users/davidbaudoin/Documents/GitHub/Rimworld-mods/RadiusUIColonistBarPlus && ./deploy.sh 2>&1 | tail -5`
Expected : `0 Erreur(s)`, `6 Avertissement(s)`, `Publié :`.

Contrôle en jeu : Options > Commandes > « Radius UI - Escouades » : septième ligne « aller au poste de combat actif », sans touche. Lui assigner une touche libre, sélectionner un pion d'une escouade qui a un poste enregistré, appuyer : l'escouade va au poste actif. Sans pion d'escouade sélectionné : message « Aucune escouade sélectionnée ».

- [ ] **Step 4 : commit (sur demande du joueur)**

```bash
git add Defs/KeyBindings_Squads.xml Source/RadiusColonistBar/SquadHotkeys.cs Languages/French/DefInjected
git commit -m "Battle stations: go-to-station hotkey"
```

---

### Task 5 : documentation et contrôle final

**Files:**
- Modify: `README.md` (nouvelle section après « Ordres d'escouade », avant « Sauvegardes » ; section « Sauvegardes »)
- Modify: `CHANGELOG.md` (nouvelle entrée en tête)
- Modify: `docs/ROADMAP.md` (lot 2)

**Interfaces:** aucune.

- [ ] **Step 1 : README**

Avant `## Sauvegardes`, ajouter :

```markdown
## Postes de combat

La carte d'ordres a une section « Postes de combat » : quatre postes par escouade, dont un actif (pastille surlignée, un clic pour changer).

- **Enregistrer** : chaque membre présent sur la carte prend sa position actuelle comme poste actif. Les membres absents, à terre ou en crise gardent leur poste précédent. Pour retoucher un seul pion : le déplacer, puis réenregistrer.
- **Aller au poste** : l'escouade se mobilise, sort de l'assaut et chaque membre court à sa case. Une case devenue impraticable est remplacée par la case libre la plus proche. Un membre sans poste sur cette carte est mobilisé et reste sur place.
- **Effacer** : retire le poste actif de tous les membres de l'escouade, sur toutes les cartes, après confirmation.

Survoler une pastille ou « Aller au poste » montre sur la carte la case de chaque membre. Un poste appartient au pion et ne vaut que pour la carte où il a été enregistré : un pion qui change d'escouade garde ses postes. Un septième raccourci, « aller au poste de combat actif », se règle avec les autres.
```

Dans `## Sauvegardes`, ajouter à la fin du paragraphe :

```markdown
Les postes de combat sont sauvegardés avec les escouades. Une partie rouverte avec le Colonist Bar d'origine les perd sans autre erreur.
```

- [ ] **Step 2 : CHANGELOG**

Après `# Journal des modifications` :

```markdown
## 1.1.0 — non publiée

- **Postes de combat** dans la carte d'ordres : quatre postes par escouade. Enregistrer prend la position actuelle des membres présents ; Aller au poste mobilise l'escouade, arrête son assaut et envoie chacun en sprint à sa case ; Effacer demande confirmation. Le survol montre les cases sur la carte.
- Raccourci « aller au poste de combat actif », sans touche par défaut.
- Les postes appartiennent au pion et à la carte où ils ont été enregistrés ; ils sont sauvegardés avec les escouades.
```

- [ ] **Step 3 : roadmap**

Dans `docs/ROADMAP.md`, ajouter une ligne au tableau « Fait » :

```markdown
| 2. Postes de combat | `2026-10-16-postes-de-combat-design.md` | Quatre postes par escouade, enregistrés par pion et par carte dans `BarSquads` ; Aller, Enregistrer, Effacer (avec confirmation) ; aperçu au survol ; raccourci `RCB_SquadGoToStation`. |
```

Retirer la sous-section `### Lot 2 : postes de combat` de « Lots suivants », et ajouter à « À évaluer, sans lot pour l'instant » :

```markdown
- Capture des ennemis à terre (sortie du lot 2) : choix du lit de prisonnier, de l'ennemi et du porteur.
```

Remplacer la ligne « Prochaine étape : spec du lot 2. » par « Prochaine étape : contrôles en jeu du lot 2, puis publication 1.1.0. »

- [ ] **Step 4 : déployer et contrôle final**

Run : `cd /Users/davidbaudoin/Documents/GitHub/Rimworld-mods/RadiusUIColonistBarPlus && ./deploy.sh 2>&1 | tail -5`
Expected : `0 Erreur(s)`, `6 Avertissement(s)`, `Publié :`.

Contrôle en jeu restant (spec § 7, point 9) : sauvegarder une partie avec postes, désactiver Plus, activer Radius UI - Colonist Bar d'origine, charger. Attendu : seule l'erreur déjà connue de `SquadAssault`, aucune erreur sur les postes, escouades intactes.

- [ ] **Step 5 : commit (sur demande du joueur)**

```bash
git add README.md CHANGELOG.md docs/ROADMAP.md
git commit -m "Docs: battle stations"
```
