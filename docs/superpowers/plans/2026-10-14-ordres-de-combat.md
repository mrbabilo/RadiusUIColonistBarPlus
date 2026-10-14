# Colonist Bar Plus : ordres de combat d'escouade — plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Forker Radius UI - Colonist Bar en « Radius UI - Colonist Bar Plus » et y ajouter des ordres de combat d'escouade (mobiliser, tir à volonté, regroupement, assaut plus fort / plus faible / fuyards).

**Architecture:** Copie du mod décompilé dans `RadiusUIColonistBarPlus/`, en gardant namespace, DLL, identifiant Harmony et noms de classes. Un `GameComponent` (`SquadAssault`) retient le mode d'assaut de chaque pion ; un `ThinkTreeDef` branché sur le crochet vanilla `Humanlike_PostMentalState` lance `JobGiver_SquadAssault` (sous-classe de `JobGiver_AIFightEnemies`) pour les pions mobilisés qui ont un mode. Les ordres partent d'un menu ouvert par clic droit sur l'onglet d'une escouade ou depuis le module Escouades du Centre de commandement.

**Tech Stack:** C# `net472`, RimWorld 1.6 (`Assembly-CSharp`), Harmony 2 (`0Harmony.dll`), Radius UI Framework (`RadiusUI.Framework.dll`), `dotnet build`, XML de defs et de traduction.

**Spec:** `RadiusUIColonistBarPlus/docs/superpowers/specs/2026-10-14-ordres-de-combat-design.md` (révision 2). Contexte : `Rimworld-mods/AUDIT-colonistbar.md`.

## Global Constraints

- Mod : nom « Radius UI - Colonist Bar Plus », packageId `mrbabilo.RadiusUI.ColonistBarPlus`, auteur mrbabilo, `supportedVersions` 1.6.
- `incompatibleWith` : `astryl.RadiusUI.ColonistBar`, `DerekBickley.LTOColonyGroupsFinal`.
- À ne jamais renommer : namespace `RadiusColonistBar`, DLL `RadiusColonistBar.dll`, identifiant Harmony `astryl.radiusui.colonistbar`, classes `BarSquads`, `CCKits`, `CCHistory`, defName `RCB_FollowCam`.
- Le code de Colony Groups (aucune licence) n'est jamais recopié : il ne sert que de référence de comportement.
- Pas de `Lord`, `LordJob` ni `DutyDef`. Pas de patch XPath de l'arbre `Humanlike`.
- `hostilityResponse` n'est modifié que le temps d'un appel, rétabli dans un `finally`.
- Traductions : nouvelles clés `RCB_Orders_*` et `RCB_Sub_RightClickTip` en anglais dans `Languages/English/Keyed/RadiusColonistBar.xml` ; le français du fork va dans `Languages/French/Keyed/RadiusColonistBarPlus.xml` et ne contient **que** ces clés ajoutées (les clés d'origine sont traduites par le mod `RadiusUIFrench`).
- Multijoueur non pris en charge (à dire dans la description).
- Le projet n'active pas les types référence nullables : pas d'annotation `?` sur un type référence (elle ajouterait l'avertissement CS8632).
- Style du code : style ILSpy du reste du mod (tabulations, accolades sur leur ligne, `namespace RadiusColonistBar;`), commentaires en anglais seulement là où le code est ajouté ou retouché.
- Aucun test automatisé dans ce dépôt : la porte de chaque tâche est le build (0 erreur, pas de nouvel avertissement par rapport aux 6 d'origine), plus des contrôles `grep`/`xmllint`. Le comportement se vérifie en jeu (tâche 6).
- Build : `dotnet build Source/RadiusColonistBar.csproj -c Release -o $S/build -p:BaseIntermediateOutputPath=$S/obj/` avec `S=$(mktemp -d)`. Sans le volume `/Volumes/BABILOGAMES` monté, aucun build ne passe.
- Ne pas lancer `git push` : le dépôt n'a pas de distant. Commits locaux seulement.

## Review Focus

1. **Escouade répartie sur plusieurs cartes ou en caravane** : un ordre ne touche que les membres de la carte affichée ; les autres gardent leur état. Si aucun membre n'est sur la carte, le menu n'offre qu'une ligne désactivée « aucun membre sur cette carte ». (Tâche 4 : `SquadMembers.Able` ne lit que `map.mapPawns` ; tâche 5 : ligne désactivée.)
2. **Pion couché, en crise ou démobilisé pendant l'assaut** : il sort de l'assaut au plus tard 250 ticks après, et ne retourne jamais seul en assaut. (Tâche 2 : ménage `Clean` ; contrôle en jeu tâche 6, point 4.)
3. **Ordre manuel du joueur pendant l'assaut** (clic droit « aller ici », attaque désignée) : l'ordre est exécuté, le pion sort de l'assaut, les autres membres continuent. (Tâche 2 : patch `TryTakeOrderedJob` ; tâche 6, point 5.)
4. **Aucun ennemi visible, puis un raid arrive** : l'escouade en assaut attend en réévaluant toutes les 120 à 250 ticks et attaque dès qu'un ennemi est à 20 cases. (Tâche 3 : repli `Wait_Combat` avec expiration ; tâche 6, point 3.)
5. **Escouade contenant un mécanoïde ou un esclave** : le mécanoïde est ignoré par les ordres ; l'esclave reçoit les ordres comme un colon. (Tâche 4 : exclusion `IsColonyMech`/`IsSubhuman`, inclusion `SlavesOfColonySpawned` ; tâche 6, point 6.)

---

## Structure des fichiers

Dans `RadiusUIColonistBarPlus/` (copie de `RadiusUIColonistBar/`) :

| Fichier | Rôle | Tâche |
|---|---|---|
| `About/About.xml` | Identité du fork | 1 |
| `LICENSE`, `.gitignore`, `deploy.sh`, `README.md`, `CHANGELOG.md` | Projet | 1, 6 |
| `Source/RadiusColonistBar.csproj` | `LangVersion`, chemin Harmony | 1 |
| `Source/RadiusColonistBar/{RcbDevToolbar,Patch_Recache_SquadsAndHidden,FollowCam,RcbIsland}.cs` | Corrections de décompilation | 1 |
| `Source/RadiusColonistBar/AssaultMode.cs` | Énumération des modes | 2 |
| `Source/RadiusColonistBar/SquadAssault.cs` | `GameComponent` des modes | 2 |
| `Source/RadiusColonistBar/Patch_SquadAssault_Undraft.cs` | Démobilisation efface le mode | 2 |
| `Source/RadiusColonistBar/Patch_SquadAssault_PlayerOrder.cs` | Ordre du joueur efface le mode | 2 |
| `Source/RadiusColonistBar/ThinkNode_ConditionalSquadAssault.cs` | Condition du nœud | 3 |
| `Source/RadiusColonistBar/JobGiver_SquadAssault.cs` | Choix de cible et jobs d'assaut | 3 |
| `Defs/ThinkTreeDefs/SquadAssault.xml` | Branchement dans l'arbre | 3 |
| `Source/RadiusColonistBar/SquadMembers.cs` | Membres aptes et chef | 4 |
| `Source/RadiusColonistBar/SquadOrders.cs` | Exécution des ordres | 4 |
| `Source/RadiusColonistBar/SquadOrderMenu.cs` | Menu des ordres | 5 |
| `Source/RadiusColonistBar/RcbIsland.cs` | Clic droit sur l'onglet | 5 |
| `Source/RadiusColonistBar/Module_Squads.cs` | Ordres dans le module Escouades | 5 |
| `Source/RadiusColonistBar/FloatMenuOptionSub.cs` | Infobulle traduite | 5 |
| `Languages/English/Keyed/RadiusColonistBar.xml`, `Languages/French/Keyed/RadiusColonistBarPlus.xml` | Textes | 5 |

---

### Task 1: Fork compilable et publiable

**Files:**
- Create (copie) : `RadiusUIColonistBarPlus/` depuis `RadiusUIColonistBar/` (sans `About/PublishedFileId.txt`)
- Modify : `RadiusUIColonistBarPlus/Source/RadiusColonistBar.csproj`
- Modify : `RadiusUIColonistBarPlus/Source/RadiusColonistBar/RcbDevToolbar.cs:56,58`, `Patch_Recache_SquadsAndHidden.cs:17,48`, `FollowCam.cs:23`, `RcbIsland.cs:1748`
- Modify : `RadiusUIColonistBarPlus/About/About.xml`, `RadiusUIColonistBarPlus/LICENSE`
- Create : `RadiusUIColonistBarPlus/.gitignore`, `RadiusUIColonistBarPlus/deploy.sh`, `RadiusUIColonistBarPlus/README.md`, `RadiusUIColonistBarPlus/CHANGELOG.md`

**Interfaces:**
- Consumes : rien.
- Produces : un projet qui compile en `RadiusColonistBar.dll` et `deploy.sh` qui publie `Mods/RadiusUIColonistBarPlus`.

- [ ] **Step 1 : Copier le mod d'origine**

Le dossier `RadiusUIColonistBarPlus/` existe déjà (il contient `docs/` et `.git/`). Ne pas écraser `docs/`.

```bash
cd /Users/davidbaudoin/Documents/GitHub/Rimworld-mods
rsync -a --exclude 'About/PublishedFileId.txt' --exclude '.DS_Store' RadiusUIColonistBar/ RadiusUIColonistBarPlus/
find RadiusUIColonistBarPlus -name PublishedFileId.txt
```

Attendu : la commande `find` n'affiche rien (le fichier Workshop d'astryl n'est pas copié ; sinon une publication écraserait son mod).

- [ ] **Step 2 : Vérifier que le build échoue tel quel**

```bash
cd /Users/davidbaudoin/Documents/GitHub/Rimworld-mods/RadiusUIColonistBarPlus
S=$(mktemp -d); dotnet build Source/RadiusColonistBar.csproj -c Release -o $S/build -p:BaseIntermediateOutputPath=$S/obj/ 2>&1 | grep -E "error" | sort -u
```

Attendu : `error CS1617: Option non valide '15.0' pour /langversion`.

- [ ] **Step 3 : Corriger le `.csproj`**

Dans `Source/RadiusColonistBar.csproj`, remplacer `<LangVersion>15.0</LangVersion>` par `<LangVersion>latest</LangVersion>`, et dans le `HintPath` de `0Harmony.dll` remplacer `Mods/728314182/Assemblies` par `Mods/3708476081/Assemblies` (le Harmony installé, celui qu'utilise Inspector Plus) :

```bash
sed -i '' 's|<LangVersion>15.0</LangVersion>|<LangVersion>latest</LangVersion>|; s|Mods/728314182/Assemblies|Mods/3708476081/Assemblies|' Source/RadiusColonistBar.csproj
grep -n "LangVersion\|0Harmony" Source/RadiusColonistBar.csproj
```

Attendu : `<LangVersion>latest</LangVersion>` et un chemin `.../Mods/3708476081/Assemblies/0Harmony.dll`.

- [ ] **Step 4 : Corriger les erreurs de décompilation**

ILSpy a écrit `FieldRef<…>` sans le préfixe de sa classe englobante `AccessTools`, et `MathF.PI` n'existe pas dans la BCL `net472` :

```bash
cd Source/RadiusColonistBar
sed -i '' -E 's/([^.A-Za-z])FieldRef</\1AccessTools.FieldRef</g' RcbDevToolbar.cs Patch_Recache_SquadsAndHidden.cs FollowCam.cs
sed -i '' 's/MathF\.PI/Mathf.PI/' RcbIsland.cs
grep -n "AccessTools.FieldRef<" RcbDevToolbar.cs Patch_Recache_SquadsAndHidden.cs FollowCam.cs
cd ../..
```

Attendu : 5 lignes (`RcbDevToolbar.cs` 56 et 58, `Patch_Recache_SquadsAndHidden.cs` 17 et 48, `FollowCam.cs` 23).

- [ ] **Step 5 : Vérifier que le build passe**

```bash
S=$(mktemp -d); dotnet build Source/RadiusColonistBar.csproj -c Release -o $S/build -p:BaseIntermediateOutputPath=$S/obj/ 2>&1 | grep -E "warning|Erreur\(s\)|Avertissement\(s\)" | sed -E 's/\[.*//' | sort -u
```

Attendu : `0 Erreur(s)`, `6 Avertissement(s)` (CS0649 `CCContext.select`, `CCPattern.Row.linkTip`, `TlEvent.age`, `TlEvent.sub` ; CS9113 `CCHistory`, `CCKits`). Ces six avertissements sont la référence : aucune tâche ne doit en ajouter.

- [ ] **Step 6 : Identité du fork**

Dans `About/About.xml`, remplacer les trois premières balises et la description, et ajouter `incompatibleWith` avant `</ModMetaData>` :

```xml
  <name>Radius UI - Colonist Bar Plus</name>
  <packageId>mrbabilo.RadiusUI.ColonistBarPlus</packageId>
  <author>mrbabilo</author>
  <description>A fork of [url=https://steamcommunity.com/sharedfiles/filedetails/?id=3788278207]Radius UI - Colonist Bar[/url] by astryl, with squad combat orders. Right-click a squad tab on the bar (or open a squad in the Command Center) to draft or undraft the squad, toggle fire at will, regroup on the squad leader, or send it on an assault that targets the strongest enemies, the weakest ones, or those fleeing.

Replaces Radius UI - Colonist Bar: disable it. A save made with Colonist Bar keeps its squads, kits and histories. Not compatible with [LTO] Colony Groups. Multiplayer is not supported.

Everything below comes from the original Colonist Bar by astryl.

Replaces the colonist bar with a single slab across the top of the screen: each colonist gets a portrait, their name, role and current task, and two meters you choose. An alert band underneath names whoever needs attention. A Command Center window handles health, weapons, loadouts and squads, and the bar folds away when you are not using it.</description>
```

```xml
  <incompatibleWith>
    <li>astryl.RadiusUI.ColonistBar</li>
    <li>DerekBickley.LTOColonyGroupsFinal</li>
  </incompatibleWith>
```

Mettre `<modVersion>1.0.0</modVersion>`. Vérifier : `xmllint --noout About/About.xml` ne dit rien.

- [ ] **Step 7 : Licence, `.gitignore`, `deploy.sh`**

`LICENSE` : ajouter sous `Copyright (c) 2026 astryl` la ligne `Copyright (c) 2026 mrbabilo (Radius UI - Colonist Bar Plus changes)`.

`.gitignore` :

```
# Build output: deploy.sh builds out of tree and copies the DLL here.
Assemblies/*.dll
Assemblies/*.pdb
bin/
obj/
.vs/
*.user
.DS_Store
```

`deploy.sh` (puis `chmod +x deploy.sh`) :

```sh
#!/bin/sh
# Build RadiusColonistBar and publish this folder (minus sources and docs) to RimWorld's Mods folder.
set -eu

HERE=$(cd "$(dirname "$0")" && pwd)
GAME="/Volumes/BABILOGAMES/JEUX EN COURS/RimWorld.app"
MODS="$GAME/Mods"
DEST="$MODS/RadiusUIColonistBarPlus"

[ -d "$MODS" ] || { echo "Dossier Mods du jeu introuvable (volume BABILOGAMES non monté ?) : $MODS" >&2; exit 1; }

# Build out of tree so Source/ never gets bin/ or obj/.
TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT
dotnet build "$HERE/Source/RadiusColonistBar.csproj" -c Release -nologo -v q \
	-o "$TMP/build" -p:BaseIntermediateOutputPath="$TMP/obj/"

cp "$TMP/build/RadiusColonistBar.dll" "$HERE/Assemblies/"
rsync -a --delete --exclude Source/ --exclude docs/ --exclude deploy.sh --exclude .DS_Store --exclude .git/ --exclude .gitignore "$HERE/" "$DEST/"
echo "Publié : $DEST"
```

- [ ] **Step 8 : README et CHANGELOG minimaux**

`README.md` :

```markdown
# Radius UI - Colonist Bar Plus

Mod RimWorld 1.6 : fork de **Radius UI - Colonist Bar** (astryl) avec des ordres de combat d'escouade. Il remplace Colonist Bar : désactivez-le. Incompatible avec [LTO] Colony Groups. Multijoueur non pris en charge.

- packageId : `mrbabilo.RadiusUI.ColonistBarPlus`
- Dépendances : Harmony et Radius UI Framework

Pour rester compatible avec les parties existantes, le fork garde le namespace `RadiusColonistBar`, la DLL `RadiusColonistBar.dll`, l'identifiant Harmony `astryl.radiusui.colonistbar` et les noms de classes du mod d'origine.

## Compiler

`deploy.sh` compile hors de l'arbre, copie la DLL dans `Assemblies/` et publie le mod dans le dossier `Mods` du jeu (chemins en tête du script).

## Licence

MIT, voir `LICENSE`. La notice d'astryl est conservée.
```

`CHANGELOG.md` :

```markdown
# Journal des modifications

## 1.0.0 — non publiée

- Fork de Radius UI - Colonist Bar (astryl). Corrections de décompilation pour compiler les sources.
```

- [ ] **Step 9 : Publier et commiter**

```bash
./deploy.sh 2>&1 | tail -1
git add -A
git status --short | grep -c PublishedFileId
git commit -q -m "Fork Radius UI - Colonist Bar as Colonist Bar Plus

Decompiled sources build after four fixes (LangVersion, Harmony path,
AccessTools.FieldRef, Mathf.PI). New identity, incompatible with the
original and with Colony Groups.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

Attendu : `Publié : /Volumes/BABILOGAMES/JEUX EN COURS/RimWorld.app/Mods/RadiusUIColonistBarPlus`, puis `0` pour `PublishedFileId`.

---

### Task 2: Mode d'assaut par pion

**Files:**
- Create : `Source/RadiusColonistBar/AssaultMode.cs`
- Create : `Source/RadiusColonistBar/SquadAssault.cs`
- Create : `Source/RadiusColonistBar/Patch_SquadAssault_Undraft.cs`
- Create : `Source/RadiusColonistBar/Patch_SquadAssault_PlayerOrder.cs`

**Interfaces:**
- Consumes : rien (classes vanilla seulement).
- Produces :
  - `public enum AssaultMode { Strongest, Weakest, Fleeing }`
  - `public class SquadAssault : GameComponent` avec `public static SquadAssault Get { get; }` (null hors partie), `public void Set(Pawn pawn, AssaultMode mode)`, `public bool TryGetMode(Pawn pawn, out AssaultMode mode)`, `public void Clear(Pawn pawn)`.

- [ ] **Step 1 : `AssaultMode.cs`**

```csharp
namespace RadiusColonistBar;

// Squad assault (Colonist Bar Plus): which enemies an assaulting pawn goes for first.
public enum AssaultMode
{
	Strongest,
	Weakest,
	Fleeing
}
```

- [ ] **Step 2 : `SquadAssault.cs`**

```csharp
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RadiusColonistBar;

// Squad assault (Colonist Bar Plus): the assault mode of each pawn sent on an assault. ThinkNode_ConditionalSquadAssault
// reads it. Only drafted, spawned, standing pawns are kept; a mode never comes back on its own once removed.
public class SquadAssault : GameComponent
{
	private const int CleanInterval = 250;

	private Dictionary<Pawn, AssaultMode> modes = new Dictionary<Pawn, AssaultMode>();

	private List<Pawn> tmpPawns;

	private List<AssaultMode> tmpModes;

	private static readonly List<Pawn> stale = new List<Pawn>();

	private static SquadAssault cachedComp;

	private static Game cachedGame;

	public static SquadAssault Get
	{
		get
		{
			Game game = Current.Game;
			if (game == null)
			{
				cachedGame = null;
				cachedComp = null;
				return null;
			}
			if (game != cachedGame)
			{
				cachedGame = game;
				cachedComp = game.GetComponent<SquadAssault>();
			}
			return cachedComp;
		}
	}

	public SquadAssault(Game game)
	{
	}

	public void Set(Pawn pawn, AssaultMode mode)
	{
		if (pawn != null)
		{
			modes[pawn] = mode;
		}
	}

	public bool TryGetMode(Pawn pawn, out AssaultMode mode)
	{
		mode = AssaultMode.Strongest;
		if (pawn == null)
		{
			return false;
		}
		return modes.TryGetValue(pawn, out mode);
	}

	public void Clear(Pawn pawn)
	{
		if (pawn != null)
		{
			modes.Remove(pawn);
		}
	}

	public override void GameComponentTick()
	{
		if (modes.Count > 0 && Find.TickManager.TicksGame % CleanInterval == 0)
		{
			Clean();
		}
	}

	private void Clean()
	{
		stale.Clear();
		foreach (KeyValuePair<Pawn, AssaultMode> mode in modes)
		{
			Pawn key = mode.Key;
			if (key.Dead || key.Destroyed || !key.Spawned || key.Downed || key.drafter == null || !key.drafter.Drafted)
			{
				stale.Add(key);
			}
		}
		for (int i = 0; i < stale.Count; i++)
		{
			modes.Remove(stale[i]);
		}
		stale.Clear();
	}

	public override void ExposeData()
	{
		if (Scribe.mode == LoadSaveMode.Saving)
		{
			Clean();
		}
		// logNullErrors: false — a pawn gone since the save is skipped quietly; Clean() handles the rest.
		Scribe_Collections.Look(ref modes, "squadAssault", LookMode.Reference, LookMode.Value, ref tmpPawns, ref tmpModes, logNullErrors: false);
		if (modes == null)
		{
			modes = new Dictionary<Pawn, AssaultMode>();
		}
	}
}
```

- [ ] **Step 3 : `Patch_SquadAssault_Undraft.cs`**

```csharp
using HarmonyLib;
using RimWorld;
using Verse;

namespace RadiusColonistBar;

// Squad assault (Colonist Bar Plus): undrafting a pawn ends its assault. ExposeData writes draftedInt directly,
// so loading a save never comes through here.
[HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted), MethodType.Setter)]
internal static class Patch_SquadAssault_Undraft
{
	private static void Postfix(Pawn_DraftController __instance)
	{
		if (Current.Game != null && !__instance.Drafted)
		{
			SquadAssault.Get?.Clear(__instance.pawn);
		}
	}
}
```

- [ ] **Step 4 : `Patch_SquadAssault_PlayerOrder.cs`**

```csharp
using HarmonyLib;
using Verse;
using Verse.AI;

namespace RadiusColonistBar;

// Squad assault (Colonist Bar Plus): an order the player gives a pawn ends its assault. The assault node runs
// before the job queue, so without this the queued order would never be taken while a target exists.
[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.TryTakeOrderedJob))]
internal static class Patch_SquadAssault_PlayerOrder
{
	private static void Postfix(bool __result, Pawn ___pawn)
	{
		if (__result)
		{
			SquadAssault.Get?.Clear(___pawn);
		}
	}
}
```

- [ ] **Step 5 : Build**

```bash
cd /Users/davidbaudoin/Documents/GitHub/Rimworld-mods/RadiusUIColonistBarPlus
S=$(mktemp -d); dotnet build Source/RadiusColonistBar.csproj -c Release -o $S/build -p:BaseIntermediateOutputPath=$S/obj/ 2>&1 | grep -E "error|Erreur\(s\)|Avertissement\(s\)" | sed -E 's/\[.*//' | sort -u
```

Attendu : `0 Erreur(s)`, `6 Avertissement(s)`. Si le compilateur signale que `pawn` n'est pas un champ de `Pawn_JobTracker` (nom différent), lire le champ avec `~/.dotnet/tools/ilspycmd -t Verse.AI.Pawn_JobTracker "<Managed>/Assembly-CSharp.dll" | grep "Pawn pawn"` et corriger le nom du paramètre `___pawn`.

- [ ] **Step 6 : Commit**

```bash
git add Source/RadiusColonistBar/AssaultMode.cs Source/RadiusColonistBar/SquadAssault.cs Source/RadiusColonistBar/Patch_SquadAssault_Undraft.cs Source/RadiusColonistBar/Patch_SquadAssault_PlayerOrder.cs
git commit -q -m "Squad assault mode per pawn, cleared on undraft and player orders

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: IA d'assaut (nœud, giver, ThinkTreeDef)

**Files:**
- Create : `Source/RadiusColonistBar/ThinkNode_ConditionalSquadAssault.cs`
- Create : `Source/RadiusColonistBar/JobGiver_SquadAssault.cs`
- Create : `Defs/ThinkTreeDefs/SquadAssault.xml`

**Interfaces:**
- Consumes : `SquadAssault.Get`, `SquadAssault.TryGetMode(Pawn, out AssaultMode)`, `AssaultMode` (tâche 2).
- Produces : `public class ThinkNode_ConditionalSquadAssault`, `public class JobGiver_SquadAssault` (référencés par le XML, donc publics) ; `ThinkTreeDef` `RCB_SquadAssault`.

- [ ] **Step 1 : `ThinkNode_ConditionalSquadAssault.cs`**

```csharp
using Verse;
using Verse.AI;

namespace RadiusColonistBar;

// Squad assault (Colonist Bar Plus): lets JobGiver_SquadAssault run for drafted pawns sent on an assault.
public class ThinkNode_ConditionalSquadAssault : ThinkNode_Conditional
{
	protected override bool Satisfied(Pawn pawn)
	{
		SquadAssault squadAssault = SquadAssault.Get;
		if (squadAssault == null || !pawn.Drafted)
		{
			return false;
		}
		return squadAssault.TryGetMode(pawn, out _);
	}
}
```

- [ ] **Step 2 : `JobGiver_SquadAssault.cs`**

```csharp
using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RadiusColonistBar;

// Squad assault (Colonist Bar Plus). The vanilla fight giver picks cover and melee; this subclass narrows its
// target to the pawn's assault mode, shoots that target instead of letting Wait_Combat auto-target, and never
// leaves the pawn waiting forever when no enemy is around.
public class JobGiver_SquadAssault : JobGiver_AIFightEnemies
{
	private const int MaxTierPasses = 3;

	private static readonly IntRange IdleRecheck = new IntRange(120, 250);

	// Filter of the pass in progress, read by ExtraTargetValidator; null outside a filtered pass. Think nodes are
	// shared between pawns and the game runs them on one thread, so a static is enough.
	private static Predicate<Thing> passFilter;

	private static readonly List<float> tiers = new List<float>();

	protected override Job TryGiveJob(Pawn pawn)
	{
		// The vanilla giver gives nothing to a colonist whose hostility response is not Attack: set it for the
		// call only, so nothing stays on the pawn.
		Pawn_PlayerSettings playerSettings = pawn.playerSettings;
		HostilityResponseMode hostilityResponseMode = playerSettings?.hostilityResponse ?? HostilityResponseMode.Attack;
		Job job;
		try
		{
			if (playerSettings != null)
			{
				playerSettings.hostilityResponse = HostilityResponseMode.Attack;
			}
			job = base.TryGiveJob(pawn);
		}
		finally
		{
			if (playerSettings != null)
			{
				playerSettings.hostilityResponse = hostilityResponseMode;
			}
		}
		if (job == null)
		{
			return JobMaker.MakeJob(JobDefOf.Wait_Combat, IdleRecheck.RandomInRange, checkOverrideOnExpiry: true);
		}
		if (job.def == JobDefOf.Wait_Combat)
		{
			Thing enemyTarget = pawn.mindState.enemyTarget;
			Verb verb = ((enemyTarget == null) ? null : pawn.TryGetAttackVerb(enemyTarget));
			if (verb != null && !verb.verbProps.IsMeleeAttack && verb.CanHitTarget(enemyTarget))
			{
				Job job2 = JobMaker.MakeJob(JobDefOf.AttackStatic, enemyTarget);
				job2.expiryInterval = ExpiryInterval_ShooterSucceeded.RandomInRange;
				job2.checkOverrideOnExpire = true;
				job2.endIfCantShootTargetFromCurPos = true;
				return job2;
			}
		}
		return job;
	}

	protected override Thing FindAttackTarget(Pawn pawn)
	{
		SquadAssault squadAssault = SquadAssault.Get;
		if (squadAssault == null || !squadAssault.TryGetMode(pawn, out var mode))
		{
			return base.FindAttackTarget(pawn);
		}
		Thing thing = null;
		if (mode == AssaultMode.Fleeing)
		{
			thing = FilteredPass(pawn, IsFleeing);
		}
		else
		{
			CollectTiers(pawn, mode == AssaultMode.Strongest);
			for (int i = 0; i < tiers.Count && i < MaxTierPasses && thing == null; i++)
			{
				float tier = tiers[i];
				thing = FilteredPass(pawn, (Thing t) => t is Pawn p && Mathf.Approximately(p.kindDef.combatPower, tier));
			}
		}
		return thing ?? base.FindAttackTarget(pawn);
	}

	protected override bool ExtraTargetValidator(Pawn pawn, Thing target)
	{
		if (!base.ExtraTargetValidator(pawn, target))
		{
			return false;
		}
		return passFilter == null || passFilter(target);
	}

	private Thing FilteredPass(Pawn pawn, Predicate<Thing> filter)
	{
		passFilter = filter;
		try
		{
			return base.FindAttackTarget(pawn);
		}
		finally
		{
			passFilter = null;
		}
	}

	// Distinct combat powers of the enemy pawns within acquire range, strongest or weakest first. Turrets and other
	// non-pawn targets have no combat power: only the vanilla fallback takes them.
	private void CollectTiers(Pawn pawn, bool strongestFirst)
	{
		tiers.Clear();
		List<IAttackTarget> potentialTargetsFor = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn);
		float num = targetAcquireRadius * targetAcquireRadius;
		for (int i = 0; i < potentialTargetsFor.Count; i++)
		{
			if (potentialTargetsFor[i].Thing is Pawn pawn2 && !pawn2.Downed && !pawn2.Fogged() && (float)(pawn2.Position - pawn.Position).LengthHorizontalSquared <= num)
			{
				float combatPower = pawn2.kindDef.combatPower;
				if (!tiers.Contains(combatPower))
				{
					tiers.Add(combatPower);
				}
			}
		}
		tiers.Sort();
		if (strongestFirst)
		{
			tiers.Reverse();
		}
	}

	// Fleeing covers panicked pawns and raiders leaving the map, not only the Flee jobs.
	private static bool IsFleeing(Thing t)
	{
		if (!(t is Pawn pawn))
		{
			return false;
		}
		JobDef curJobDef = pawn.CurJobDef;
		if (curJobDef == JobDefOf.Flee || curJobDef == JobDefOf.FleeAndCower)
		{
			return true;
		}
		if (pawn.MentalStateDef == MentalStateDefOf.PanicFlee)
		{
			return true;
		}
		DutyDef dutyDef = pawn.mindState?.duty?.def;
		if (dutyDef == DutyDefOf.ExitMapRandom || dutyDef == DutyDefOf.ExitMapBest)
		{
			return true;
		}
		return pawn.CurJob != null && pawn.CurJob.exitMapOnArrival;
	}
}
```

- [ ] **Step 3 : `Defs/ThinkTreeDefs/SquadAssault.xml`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- Squad assault (Colonist Bar Plus). Humanlike_PostMentalState is the modder hook of the Humanlike think tree,
     right before ThinkNode_QueuedJob and the drafted branch: no XPath patch needed. -->
<Defs>
  <ThinkTreeDef>
    <defName>RCB_SquadAssault</defName>
    <insertTag>Humanlike_PostMentalState</insertTag>
    <insertPriority>100</insertPriority>
    <thinkRoot Class="RadiusColonistBar.ThinkNode_ConditionalSquadAssault">
      <subNodes>
        <li Class="RadiusColonistBar.JobGiver_SquadAssault">
          <targetAcquireRadius>20</targetAcquireRadius>
          <targetKeepRadius>23</targetKeepRadius>
        </li>
      </subNodes>
    </thinkRoot>
  </ThinkTreeDef>
</Defs>
```

- [ ] **Step 4 : Build et XML**

```bash
xmllint --noout Defs/ThinkTreeDefs/SquadAssault.xml
S=$(mktemp -d); dotnet build Source/RadiusColonistBar.csproj -c Release -o $S/build -p:BaseIntermediateOutputPath=$S/obj/ 2>&1 | grep -E "error|Erreur\(s\)|Avertissement\(s\)" | sed -E 's/\[.*//' | sort -u
```

Attendu : `xmllint` silencieux ; `0 Erreur(s)`, `6 Avertissement(s)`. En cas d'erreur sur un membre vanilla (signature différente), vérifier avec `ilspycmd -t RimWorld.JobGiver_AIFightEnemy` / `RimWorld.JobGiver_AIFightEnemies` et ajuster sans changer le comportement décrit.

- [ ] **Step 5 : Commit**

```bash
git add Source/RadiusColonistBar/ThinkNode_ConditionalSquadAssault.cs Source/RadiusColonistBar/JobGiver_SquadAssault.cs Defs/ThinkTreeDefs/SquadAssault.xml
git commit -q -m "Squad assault think tree node and fight giver

Targets by combat power tiers or fleeing state, shoots the chosen target,
re-checks every 120-250 ticks when idle, falls back to vanilla targeting.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: Membres d'escouade et exécution des ordres

**Files:**
- Create : `Source/RadiusColonistBar/SquadMembers.cs`
- Create : `Source/RadiusColonistBar/SquadOrders.cs`

**Interfaces:**
- Consumes : `BarSquads.Get`, `BarSquads.SquadIndexOf(Pawn)` (existant) ; `SquadAssault` (tâche 2) ; `AssaultMode` (tâche 2).
- Produces :
  - `internal static List<Pawn> SquadMembers.Able(Map map, int index)` — membres aptes, triés dans l'ordre de la barre ; le premier est le chef.
  - `internal static void SquadOrders.Draft(int index)`, `Undraft(int index)`, `ToggleFireAtWill(int index)`, `Regroup(int index)`, `Assault(int index, AssaultMode mode)`.
  - `internal static bool SquadOrders.AllDrafted(List<Pawn> pawns)`, `AllFireAtWill(List<Pawn> pawns)`, `CommonMode(List<Pawn> pawns, out AssaultMode mode)`.

- [ ] **Step 1 : `SquadMembers.cs`**

```csharp
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RadiusColonistBar;

// Squad orders (Colonist Bar Plus): the members of a squad who can take an order on one map. BarSquads keeps no
// ordered member list, so the order comes from the colonist bar, then thingIDNumber; the first one leads.
internal static class SquadMembers
{
	private static readonly Dictionary<Pawn, int> barRank = new Dictionary<Pawn, int>();

	internal static List<Pawn> Able(Map map, int index)
	{
		List<Pawn> list = new List<Pawn>();
		BarSquads get = BarSquads.Get;
		if (map == null || get == null || index < 0)
		{
			return list;
		}
		Add(list, map.mapPawns.FreeColonistsSpawned, get, index);
		Add(list, map.mapPawns.SlavesOfColonySpawned, get, index);
		RankByBar();
		list.Sort(Compare);
		barRank.Clear();
		return list;
	}

	private static void Add(List<Pawn> into, List<Pawn> from, BarSquads squads, int index)
	{
		for (int i = 0; i < from.Count; i++)
		{
			Pawn pawn = from[i];
			// Mechanoids and subhumans run other think trees: the assault node never reaches them.
			if (squads.SquadIndexOf(pawn) == index && !pawn.IsColonyMech && !pawn.IsSubhuman && !pawn.Downed && !pawn.InMentalState && !pawn.IsPrisoner && pawn.drafter != null && !into.Contains(pawn))
			{
				into.Add(pawn);
			}
		}
	}

	private static void RankByBar()
	{
		barRank.Clear();
		List<ColonistBar.Entry> list = Find.ColonistBar?.Entries;
		if (list == null)
		{
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			Pawn pawn = list[i].pawn;
			if (pawn != null && !barRank.ContainsKey(pawn))
			{
				barRank[pawn] = i;
			}
		}
	}

	private static int Compare(Pawn a, Pawn b)
	{
		int value;
		int num = (barRank.TryGetValue(a, out value) ? value : int.MaxValue);
		int value2;
		int num2 = (barRank.TryGetValue(b, out value2) ? value2 : int.MaxValue);
		if (num != num2)
		{
			return num.CompareTo(num2);
		}
		return a.thingIDNumber.CompareTo(b.thingIDNumber);
	}
}
```

- [ ] **Step 2 : `SquadOrders.cs`**

```csharp
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RadiusColonistBar;

// Squad orders (Colonist Bar Plus): run on the able members of a squad on the current map, one pawn at a time;
// a pawn that throws is logged once and the others still get the order.
internal static class SquadOrders
{
	internal static void Draft(int index)
	{
		foreach (Pawn pawn in SquadMembers.Able(Find.CurrentMap, index))
		{
			Try(pawn, delegate
			{
				pawn.drafter.Drafted = true;
			});
		}
	}

	internal static void Undraft(int index)
	{
		foreach (Pawn pawn in SquadMembers.Able(Find.CurrentMap, index))
		{
			Try(pawn, delegate
			{
				pawn.drafter.Drafted = false;
			});
		}
	}

	// All on: switch it off for everyone, which also ends their assault. Otherwise switch it on for everyone.
	internal static void ToggleFireAtWill(int index)
	{
		List<Pawn> list = SquadMembers.Able(Find.CurrentMap, index);
		bool allOn = AllFireAtWill(list);
		foreach (Pawn pawn in list)
		{
			Try(pawn, delegate
			{
				pawn.drafter.FireAtWill = !allOn;
				if (allOn)
				{
					SquadAssault.Get?.Clear(pawn);
				}
			});
		}
	}

	internal static void Regroup(int index)
	{
		List<Pawn> list = SquadMembers.Able(Find.CurrentMap, index);
		if (list.Count == 0)
		{
			return;
		}
		Pawn leader = list[0];
		foreach (Pawn pawn in list)
		{
			Try(pawn, delegate
			{
				pawn.drafter.Drafted = true;
				SquadAssault.Get?.Clear(pawn);
				if (pawn != leader)
				{
					IntVec3 intVec = RCellFinder.BestOrderedGotoDestNear(leader.Position, pawn);
					if (intVec.IsValid)
					{
						Job job = JobMaker.MakeJob(JobDefOf.Goto, intVec);
						job.locomotionUrgency = LocomotionUrgency.Sprint;
						pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
					}
				}
			});
		}
	}

	internal static void Assault(int index, AssaultMode mode)
	{
		SquadAssault squadAssault = SquadAssault.Get;
		if (squadAssault == null)
		{
			return;
		}
		List<Pawn> list = SquadMembers.Able(Find.CurrentMap, index);
		if (list.Count == 0)
		{
			return;
		}
		Find.Selector.ClearSelection();
		foreach (Pawn pawn in list)
		{
			Try(pawn, delegate
			{
				pawn.drafter.Drafted = true;
				pawn.drafter.FireAtWill = true;
				squadAssault.Set(pawn, mode);
				Find.Selector.Select(pawn, playSound: false);
				// End the current job so the think tree picks the assault node right away.
				if (pawn.CurJob != null)
				{
					pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
				}
			});
		}
	}

	internal static bool AllDrafted(List<Pawn> pawns)
	{
		if (pawns.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < pawns.Count; i++)
		{
			if (!pawns[i].Drafted)
			{
				return false;
			}
		}
		return true;
	}

	internal static bool AllFireAtWill(List<Pawn> pawns)
	{
		if (pawns.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < pawns.Count; i++)
		{
			if (!pawns[i].drafter.FireAtWill)
			{
				return false;
			}
		}
		return true;
	}

	// The mode every member shares, if they all have the same one.
	internal static bool CommonMode(List<Pawn> pawns, out AssaultMode mode)
	{
		mode = AssaultMode.Strongest;
		SquadAssault squadAssault = SquadAssault.Get;
		if (squadAssault == null || pawns.Count == 0 || !squadAssault.TryGetMode(pawns[0], out mode))
		{
			return false;
		}
		for (int i = 1; i < pawns.Count; i++)
		{
			if (!squadAssault.TryGetMode(pawns[i], out var mode2) || mode2 != mode)
			{
				return false;
			}
		}
		return true;
	}

	private static void Try(Pawn pawn, Action action)
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			Log.ErrorOnce("[Radius UI - Colonist Bar] Squad order failed for " + pawn.LabelShort + ": " + ex, pawn.thingIDNumber ^ 0x52434253);
		}
	}
}
```

- [ ] **Step 3 : Build**

```bash
S=$(mktemp -d); dotnet build Source/RadiusColonistBar.csproj -c Release -o $S/build -p:BaseIntermediateOutputPath=$S/obj/ 2>&1 | grep -E "error|Erreur\(s\)|Avertissement\(s\)" | sed -E 's/\[.*//' | sort -u
```

Attendu : `0 Erreur(s)`, `6 Avertissement(s)`.

- [ ] **Step 4 : Commit**

```bash
git add Source/RadiusColonistBar/SquadMembers.cs Source/RadiusColonistBar/SquadOrders.cs
git commit -q -m "Squad members and squad orders

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: Menus des ordres et textes

**Files:**
- Create : `Source/RadiusColonistBar/SquadOrderMenu.cs`
- Modify : `Source/RadiusColonistBar/RcbIsland.cs` (branche du clic droit, autour de la ligne 1488 ; nouvelle méthode `SquadTabAt`)
- Modify : `Source/RadiusColonistBar/Module_Squads.cs:116-145` (`SquadMenu`)
- Modify : `Source/RadiusColonistBar/FloatMenuOptionSub.cs` (ligne `tooltip = "Right-click: …"`)
- Modify : `Languages/English/Keyed/RadiusColonistBar.xml`
- Create : `Languages/French/Keyed/RadiusColonistBarPlus.xml`

**Interfaces:**
- Consumes : `SquadMembers.Able`, `SquadOrders.*`, `AssaultMode` (tâches 2 et 4) ; `RcbFloatMenu`, `FloatMenuOptionSub`, `RcbCohorts.TokenIsSquad(int, out int)` (existants).
- Produces : `internal static List<FloatMenuOption> SquadOrderMenu.Options(int index)`, `internal static void SquadOrderMenu.Open(int index)`.

- [ ] **Step 1 : `SquadOrderMenu.cs`**

```csharp
using System;
using System.Collections.Generic;
using Verse;

namespace RadiusColonistBar;

// Squad orders (Colonist Bar Plus): the order list shared by the squad tab right-click and the Squads module.
internal static class SquadOrderMenu
{
	internal static void Open(int index)
	{
		Find.WindowStack.Add(new RcbFloatMenu(Options(index)));
	}

	internal static List<FloatMenuOption> Options(int index)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		List<Pawn> list2 = SquadMembers.Able(Find.CurrentMap, index);
		if (list2.Count == 0)
		{
			// A null action draws the option disabled.
			list.Add(new FloatMenuOption("RCB_Orders_NoMember".Translate(), null));
			return list;
		}
		bool flag = SquadOrders.AllDrafted(list2);
		list.Add(Option(flag ? "RCB_Orders_Undraft" : "RCB_Orders_Draft", flag ? "RCB_Orders_UndraftTip" : "RCB_Orders_DraftTip", delegate
		{
			if (flag)
			{
				SquadOrders.Undraft(index);
			}
			else
			{
				SquadOrders.Draft(index);
			}
		}));
		bool flag2 = SquadOrders.AllFireAtWill(list2);
		list.Add(Option(flag2 ? "RCB_Orders_FireAtWillOff" : "RCB_Orders_FireAtWillOn", "RCB_Orders_FireAtWillTip", delegate
		{
			SquadOrders.ToggleFireAtWill(index);
		}));
		list.Add(Option("RCB_Orders_Regroup", "RCB_Orders_RegroupTip", delegate
		{
			SquadOrders.Regroup(index);
		}));
		bool hasMode = SquadOrders.CommonMode(list2, out var current);
		list.Add(new FloatMenuOptionSub("RCB_Orders_Assault".Translate(), () => AssaultOptions(index, hasMode, current)));
		return list;
	}

	private static List<FloatMenuOption> AssaultOptions(int index, bool hasMode, AssaultMode current)
	{
		return new List<FloatMenuOption>
		{
			AssaultOption(index, AssaultMode.Strongest, "RCB_Orders_AssaultStrongest", "RCB_Orders_AssaultStrongestTip", hasMode && current == AssaultMode.Strongest),
			AssaultOption(index, AssaultMode.Weakest, "RCB_Orders_AssaultWeakest", "RCB_Orders_AssaultWeakestTip", hasMode && current == AssaultMode.Weakest),
			AssaultOption(index, AssaultMode.Fleeing, "RCB_Orders_AssaultFleeing", "RCB_Orders_AssaultFleeingTip", hasMode && current == AssaultMode.Fleeing)
		};
	}

	private static FloatMenuOption AssaultOption(int index, AssaultMode mode, string labelKey, string tipKey, bool active)
	{
		string text = labelKey.Translate();
		if (active)
		{
			text = "RCB_Orders_ActiveMark".Translate(text);
		}
		return new FloatMenuOption(text, delegate
		{
			SquadOrders.Assault(index, mode);
		})
		{
			tooltip = new TipSignal(tipKey.Translate())
		};
	}

	private static FloatMenuOption Option(string labelKey, string tipKey, Action action)
	{
		return new FloatMenuOption(labelKey.Translate(), action)
		{
			tooltip = new TipSignal(tipKey.Translate())
		};
	}
}
```

- [ ] **Step 2 : Clic droit sur l'onglet d'escouade (`RcbIsland.cs`)**

Remplacer le bloc existant (repérer la ligne `if (RcbInput.RightPress(current2) && RcbGuard.Finite(lastSlab) …`) :

```csharp
			if (RcbInput.RightPress(current2) && RcbGuard.Finite(lastSlab) && lastSlab.Contains(current2.mousePosition) && !OverACard(current2.mousePosition))
			{
				if (RcbGuard.Finite(lastRailBand) && lastRailBand.height > 1f && lastRailBand.Contains(current2.mousePosition) && RcbCohorts.Token != 0)
				{
					SoundDefOf.Click.PlayOneShotOnCamera();
					RcbCohorts.SetToken(0);
				}
				chromeClaimedDown = true;
				current2.Use();
			}
```

par :

```csharp
			if (RcbInput.RightPress(current2) && RcbGuard.Finite(lastSlab) && lastSlab.Contains(current2.mousePosition) && !OverACard(current2.mousePosition))
			{
				// Squad orders (Colonist Bar Plus): right-click a squad tab for its orders; elsewhere on the rail it
				// still goes back to "All".
				int num6 = SquadTabAt(current2.mousePosition);
				if (num6 >= 0)
				{
					SoundDefOf.Click.PlayOneShotOnCamera();
					SquadOrderMenu.Open(num6);
				}
				else if (RcbGuard.Finite(lastRailBand) && lastRailBand.height > 1f && lastRailBand.Contains(current2.mousePosition) && RcbCohorts.Token != 0)
				{
					SoundDefOf.Click.PlayOneShotOnCamera();
					RcbCohorts.SetToken(0);
				}
				chromeClaimedDown = true;
				current2.Use();
			}
```

Avant d'écrire `num6`, vérifier avec `grep -n "num6" Source/RadiusColonistBar/RcbIsland.cs` que ce nom n'est pas déjà utilisé dans la méthode ; sinon prendre le premier `numN` libre.

Ajouter dans `RcbIsland`, juste après `OpenSpillMenu()` :

```csharp
	// Squad orders (Colonist Bar Plus): squad index of the rail tab under the mouse, or -1. Tabs folded into the
	// "+N" menu have no rect here; the Squads module covers them.
	private static int SquadTabAt(Vector2 pos)
	{
		for (int i = 0; i < tabRects.Count && i < railTok.Count; i++)
		{
			if (tabRects[i].Contains(pos) && RcbCohorts.TokenIsSquad(railTok[i], out var si))
			{
				return si;
			}
		}
		return -1;
	}
```

- [ ] **Step 3 : Ordres dans le module Escouades (`Module_Squads.cs`)**

Dans `SquadMenu`, remplacer :

```csharp
		List<FloatMenuOption> list = new List<FloatMenuOption>
		{
			new FloatMenuOption("RCB_CC_SquadRename".Translate(), delegate
			{
				Find.WindowStack.Add(new Dialog_RenameSquad(sq, si));
			})
		};
```

par :

```csharp
		// Squad orders (Colonist Bar Plus) first, then the squad's own management.
		List<FloatMenuOption> list = SquadOrderMenu.Options(si);
		list.Add(new FloatMenuOption("RCB_CC_SquadRename".Translate(), delegate
		{
			Find.WindowStack.Add(new Dialog_RenameSquad(sq, si));
		}));
```

et, à la fin de `SquadMenu`, remplacer `Find.WindowStack.Add(new FloatMenu(list));` par `Find.WindowStack.Add(new RcbFloatMenu(list));` (le sous-menu Assaut s'ouvre au clic droit seulement dans un `RcbFloatMenu`).

- [ ] **Step 4 : Infobulle traduite de `FloatMenuOptionSub`**

Dans `FloatMenuOptionSub.cs`, remplacer :

```csharp
		tooltip = "Right-click: open as a submenu without closing this menu.";
```

par :

```csharp
		// Colonist Bar Plus: translated instead of hard-coded English.
		tooltip = new TipSignal("RCB_Sub_RightClickTip".Translate());
```

- [ ] **Step 5 : Textes anglais**

Ajouter avant `</LanguageData>` dans `Languages/English/Keyed/RadiusColonistBar.xml` :

```xml
  <!-- Squad orders (Colonist Bar Plus) -->
  <RCB_Sub_RightClickTip>Right-click: open as a submenu without closing this menu.</RCB_Sub_RightClickTip>
  <RCB_Orders_NoMember>No squad member on this map</RCB_Orders_NoMember>
  <RCB_Orders_Draft>Draft squad</RCB_Orders_Draft>
  <RCB_Orders_DraftTip>Draft every member of this squad on this map.</RCB_Orders_DraftTip>
  <RCB_Orders_Undraft>Undraft squad</RCB_Orders_Undraft>
  <RCB_Orders_UndraftTip>Undraft every member of this squad on this map. This also ends their assault.</RCB_Orders_UndraftTip>
  <RCB_Orders_FireAtWillOn>Fire at will</RCB_Orders_FireAtWillOn>
  <RCB_Orders_FireAtWillOff>Hold fire</RCB_Orders_FireAtWillOff>
  <RCB_Orders_FireAtWillTip>Turns fire at will on for the whole squad, or off if everyone already has it. Holding fire ends the assault.</RCB_Orders_FireAtWillTip>
  <RCB_Orders_Regroup>Regroup</RCB_Orders_Regroup>
  <RCB_Orders_RegroupTip>Drafts the squad, ends its assault and sends everyone running to the squad leader (the first member in bar order).</RCB_Orders_RegroupTip>
  <RCB_Orders_Assault>Assault</RCB_Orders_Assault>
  <RCB_Orders_AssaultStrongest>Strongest first</RCB_Orders_AssaultStrongest>
  <RCB_Orders_AssaultStrongestTip>Drafts the squad and attacks on its own, going for the strongest enemies within reach first. Undraft or give a member an order to stop.</RCB_Orders_AssaultStrongestTip>
  <RCB_Orders_AssaultWeakest>Weakest first</RCB_Orders_AssaultWeakest>
  <RCB_Orders_AssaultWeakestTip>Drafts the squad and attacks on its own, going for the weakest enemies within reach first. Undraft or give a member an order to stop.</RCB_Orders_AssaultWeakestTip>
  <RCB_Orders_AssaultFleeing>Pursue the fleeing</RCB_Orders_AssaultFleeing>
  <RCB_Orders_AssaultFleeingTip>Drafts the squad and goes after enemies who are fleeing or leaving the map, then any enemy within reach. Undraft or give a member an order to stop.</RCB_Orders_AssaultFleeingTip>
  <RCB_Orders_ActiveMark>{0} (current)</RCB_Orders_ActiveMark>
```

- [ ] **Step 6 : Textes français (seulement les clés ajoutées)**

Créer `Languages/French/Keyed/RadiusColonistBarPlus.xml` :

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- Only the keys Colonist Bar Plus adds. The original Colonist Bar keys are translated by the
     "Radius UI - Traduction française" mod: a key defined twice in one language is refused. -->
<LanguageData>
  <RCB_Sub_RightClickTip>Clic droit : ouvrir en sous-menu sans fermer ce menu.</RCB_Sub_RightClickTip>
  <RCB_Orders_NoMember>Aucun membre de l'escouade sur cette carte</RCB_Orders_NoMember>
  <RCB_Orders_Draft>Mobiliser l'escouade</RCB_Orders_Draft>
  <RCB_Orders_DraftTip>Mobilise tous les membres de cette escouade présents sur cette carte.</RCB_Orders_DraftTip>
  <RCB_Orders_Undraft>Démobiliser l'escouade</RCB_Orders_Undraft>
  <RCB_Orders_UndraftTip>Démobilise tous les membres de cette escouade présents sur cette carte. Leur assaut prend fin.</RCB_Orders_UndraftTip>
  <RCB_Orders_FireAtWillOn>Tir à volonté</RCB_Orders_FireAtWillOn>
  <RCB_Orders_FireAtWillOff>Cesser le feu</RCB_Orders_FireAtWillOff>
  <RCB_Orders_FireAtWillTip>Active le tir à volonté pour toute l'escouade, ou le coupe si tout le monde l'a déjà. Cesser le feu met fin à l'assaut.</RCB_Orders_FireAtWillTip>
  <RCB_Orders_Regroup>Regroupement</RCB_Orders_Regroup>
  <RCB_Orders_RegroupTip>Mobilise l'escouade, met fin à son assaut et envoie chacun en courant vers le chef (le premier membre dans l'ordre de la barre).</RCB_Orders_RegroupTip>
  <RCB_Orders_Assault>Assaut</RCB_Orders_Assault>
  <RCB_Orders_AssaultStrongest>Les plus forts d'abord</RCB_Orders_AssaultStrongest>
  <RCB_Orders_AssaultStrongestTip>Mobilise l'escouade, qui attaque d'elle-même en visant d'abord les ennemis les plus forts à sa portée. Démobilisez un membre ou donnez-lui un ordre pour l'arrêter.</RCB_Orders_AssaultStrongestTip>
  <RCB_Orders_AssaultWeakest>Les plus faibles d'abord</RCB_Orders_AssaultWeakest>
  <RCB_Orders_AssaultWeakestTip>Mobilise l'escouade, qui attaque d'elle-même en visant d'abord les ennemis les plus faibles à sa portée. Démobilisez un membre ou donnez-lui un ordre pour l'arrêter.</RCB_Orders_AssaultWeakestTip>
  <RCB_Orders_AssaultFleeing>Poursuivre les fuyards</RCB_Orders_AssaultFleeing>
  <RCB_Orders_AssaultFleeingTip>Mobilise l'escouade, qui poursuit les ennemis en fuite ou qui quittent la carte, puis tout ennemi à sa portée. Démobilisez un membre ou donnez-lui un ordre pour l'arrêter.</RCB_Orders_AssaultFleeingTip>
  <RCB_Orders_ActiveMark>{0} (en cours)</RCB_Orders_ActiveMark>
</LanguageData>
```

- [ ] **Step 7 : Vérifier XML, parité des nouvelles clés, build**

```bash
xmllint --noout Languages/English/Keyed/RadiusColonistBar.xml Languages/French/Keyed/RadiusColonistBarPlus.xml
diff <(grep -oE "<RCB_(Orders|Sub)_[A-Za-z]+>" Languages/English/Keyed/RadiusColonistBar.xml | sort) <(grep -oE "<RCB_(Orders|Sub)_[A-Za-z]+>" Languages/French/Keyed/RadiusColonistBarPlus.xml | sort) && echo parite
grep -ohE "\"RCB_(Orders|Sub)_[A-Za-z]+\"" Source/RadiusColonistBar/*.cs | tr -d '"' | sort -u | while read k; do grep -q "<$k>" Languages/English/Keyed/RadiusColonistBar.xml || echo "MANQUE $k"; done
S=$(mktemp -d); dotnet build Source/RadiusColonistBar.csproj -c Release -o $S/build -p:BaseIntermediateOutputPath=$S/obj/ 2>&1 | grep -E "error|Erreur\(s\)|Avertissement\(s\)" | sed -E 's/\[.*//' | sort -u
```

Attendu : `xmllint` silencieux, `parite`, aucune ligne `MANQUE`, `0 Erreur(s)`, `6 Avertissement(s)`.

- [ ] **Step 8 : Commit**

```bash
git add Source/RadiusColonistBar/SquadOrderMenu.cs Source/RadiusColonistBar/RcbIsland.cs Source/RadiusColonistBar/Module_Squads.cs Source/RadiusColonistBar/FloatMenuOptionSub.cs Languages/English/Keyed/RadiusColonistBar.xml Languages/French/Keyed/RadiusColonistBarPlus.xml
git commit -q -m "Squad order menu on squad tabs and in the Squads module

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: Documentation, publication locale, contrôle en jeu

**Files:**
- Modify : `README.md`, `CHANGELOG.md`
- Modify : `/Users/davidbaudoin/Documents/GitHub/Rimworld-mods/CLAUDE.md` (tableau des dossiers : ligne `RadiusUIColonistBarPlus/`)

**Interfaces:**
- Consumes : tout ce qui précède.
- Produces : mod publié dans `Mods/RadiusUIColonistBarPlus`, liste de contrôle en jeu.

- [ ] **Step 1 : README**

Ajouter après le paragraphe d'introduction :

```markdown
## Ordres d'escouade

Clic droit sur l'onglet d'une escouade dans la barre (ou menu d'une escouade dans le module Escouades du Centre de commandement) :

- **Mobiliser / Démobiliser l'escouade** ;
- **Tir à volonté / Cesser le feu** ;
- **Regroupement** : tout le monde court vers le chef (le premier membre dans l'ordre de la barre) ;
- **Assaut** : l'escouade attaque d'elle-même, en visant d'abord les ennemis les plus forts, les plus faibles, ou ceux qui fuient. Elle se met à couvert comme en vanilla et attaque n'importe quel ennemi à portée quand aucun ne correspond. L'assaut s'arrête quand on démobilise un membre, qu'on lui donne un ordre, ou qu'on coupe le tir à volonté.

Les ordres ne touchent que les membres présents sur la carte affichée. Les mécanoïdes sont ignorés.

Les onglets d'escouade n'apparaissent que si le réglage des onglets de groupes est actif et qu'il y a au moins deux onglets.

## Sauvegardes

Le mod ajoute un composant de partie (`SquadAssault`) qui retient le mode d'assaut de chaque pion. Le retirer en cours de partie donne une erreur au chargement ; seuls les assauts en cours sont perdus.

## Crédits

| Composant | Auteur | Source | Licence |
|---|---|---|---|
| Radius UI - Colonist Bar (base du mod) | astryl | [Steam Workshop 3788278207](https://steamcommunity.com/sharedfiles/filedetails/?id=3788278207) | MIT |
| [LTO] Colony Groups (idée des ordres de groupe, comportement de référence ; code non repris) | DerekBickley | [Steam Workshop 2345493945](https://steamcommunity.com/sharedfiles/filedetails/?id=2345493945) | non indiquée |
```

- [ ] **Step 2 : CHANGELOG**

Remplacer la section `1.0.0` par :

```markdown
## 1.0.0 — non publiée

- Fork de Radius UI - Colonist Bar (astryl). Corrections de décompilation pour compiler les sources.
- **Ordres d'escouade** par clic droit sur l'onglet d'une escouade ou depuis le module Escouades : mobiliser ou démobiliser, tir à volonté, regroupement sur le chef, assaut (les plus forts d'abord, les plus faibles d'abord, poursuite des fuyards).
- L'assaut garde le comportement vanilla de couverture et de corps à corps, attaque n'importe quel ennemi à portée quand aucun ne correspond au mode, et se réveille seul quand de nouveaux ennemis arrivent.
- Un ordre donné à un pion, sa démobilisation ou « Cesser le feu » met fin à son assaut.
- Infobulle des sous-menus traduite.
```

- [ ] **Step 3 : CLAUDE.md du dossier parent**

Dans le tableau « Nature du dépôt » de `/Users/davidbaudoin/Documents/GitHub/Rimworld-mods/CLAUDE.md`, ajouter après la ligne `RadiusUIColonistBar/` :

```markdown
| `RadiusUIColonistBarPlus/` | **Projet de travail** : « Radius UI - Colonist Bar Plus » (auteur mrbabilo), fork de Colonist Bar avec ordres de combat d'escouade. Dépôt git local (branche `main`, pas de distant). Namespace, DLL, identifiant Harmony et noms de classes d'origine conservés. Publication : `RadiusUIColonistBarPlus/deploy.sh` | `mrbabilo.RadiusUI.ColonistBarPlus` |
```

- [ ] **Step 4 : Publier et commiter**

```bash
cd /Users/davidbaudoin/Documents/GitHub/Rimworld-mods/RadiusUIColonistBarPlus
./deploy.sh 2>&1 | grep -E "Erreur\(s\)|Publié"
find "/Volumes/BABILOGAMES/JEUX EN COURS/RimWorld.app/Mods/RadiusUIColonistBarPlus" -name SquadAssault.xml -o -name RadiusColonistBar.dll -o -name RadiusColonistBarPlus.xml
git add README.md CHANGELOG.md
git commit -q -m "Docs: squad orders, saves, credits

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

Attendu : `0 Erreur(s)`, `Publié : …`, puis trois fichiers trouvés (`Defs/ThinkTreeDefs/SquadAssault.xml`, `Assemblies/RadiusColonistBar.dll`, `Languages/French/Keyed/RadiusColonistBarPlus.xml`).

- [ ] **Step 5 : Contrôle en jeu (à faire par le joueur)**

Activer Colonist Bar Plus, désactiver Colonist Bar d'origine, relancer le jeu. Cocher chaque point :

1. `Player.log` : pas d'erreur `[Radius UI - Colonist Bar]`, ligne `[Radius UI - Colonist Bar] initialized.`, aucune erreur de def sur `RCB_SquadAssault`.
2. Partie venant de Colonist Bar d'origine : escouades, kits et historiques présents.
3. Raid : « Assaut → Les plus forts d'abord » ; les tireurs visent l'ennemi le plus fort à portée ; quand le raid fuit ou meurt, l'escouade attend, puis attaque seule un nouvel ennemi qui arrive (Review Focus 4).
4. Un membre est mis à terre : il sort de l'assaut ; relevé, il ne reprend pas l'assaut seul (Review Focus 2).
5. Clic droit « aller ici » sur un membre en assaut : il obéit et sort de l'assaut, les autres continuent (Review Focus 3).
6. Escouade avec un esclave : il reçoit les ordres ; un mécanoïde placé dans l'escouade n'en reçoit aucun (Review Focus 5).
7. Membres sur une autre carte ou en caravane : non touchés ; escouade sans membre sur la carte affichée : ligne désactivée « Aucun membre de l'escouade sur cette carte » (Review Focus 1).
8. Démobiliser : réponse aux hostiles des pions inchangée par rapport à avant l'assaut.
9. Sauvegarder pendant un assaut, recharger : l'assaut reprend.
10. Retirer le mod, charger cette sauvegarde : une seule erreur (« Could not find class RadiusColonistBar.SquadAssault »), pions normaux.
11. Module Escouades : les ordres apparaissent en tête du menu d'une escouade ; « Assaut » ouvre son sous-menu.
12. Jeu en français : textes des ordres en français.
