# Carte d'ordres d'escouade — plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remplacer le menu déroulant vanilla des ordres d'escouade par une carte Radius, et ajouter l'état sur les onglets, l'épée d'assaut sur les portraits et six raccourcis clavier.

**Architecture:** Un état d'escouade calculé une fois (`SquadStatus`, avec cache court) alimente la carte (`SquadOrderCard : Window`), les onglets (`RcbIsland`) et les raccourcis (`SquadHotkeys`). La carte appelle les ordres existants de `SquadOrders`, plus une seule méthode neuve, `StopAssault`. Le menu `SquadOrderMenu` disparaît.

**Tech Stack:** C# `net472`, Harmony, RimWorld 1.6, Radius UI Framework (`CardChrome`, `Palette`, `IconSet`), helpers de la barre (`RcbSpatial`, `RcbFont`, `RcbPalette`).

**Spec:** `docs/superpowers/specs/2026-10-15-carte-ordres-design.md`

## Global Constraints

- Aucune ligne de [LTO] Colony Groups reprise.
- Namespace `RadiusColonistBar`, DLL `RadiusColonistBar.dll`, Harmony `astryl.radiusui.colonistbar` inchangés.
- Style ILSpy du code existant (`num`, `flag`, `text`…), commentaires seulement sur le code ajouté ou retouché.
- Tout texte visible passe par une clé : anglais dans `Languages/English/Keyed/RadiusColonistBar.xml` (bloc Colonist Bar Plus), français dans `Languages/French/Keyed/RadiusColonistBarPlus.xml`.
- Les defName des raccourcis ne changent plus une fois publiés.
- Pas de tests automatisés dans le dépôt : chaque tâche se prouve par `./deploy.sh` (attendu : `0 Erreur(s)`, `6 Avertissement(s)`, ligne `Publié :`), puis par le contrôle en jeu de la tâche 7. Pas de `git commit` sans demande explicite du joueur (`CLAUDE.md`) : les étapes « commit » préparent le message, le joueur décide.

## Review Focus

1. Clic droit sur l'onglet quand la carte de la même escouade est ouverte : elle doit se fermer, pas se rouvrir (la fermeture par clic extérieur et le clic droit arrivent dans la même image).
2. Escouade dissoute ou renommée pendant que la carte est ouverte (depuis le Centre de commandement) : la carte se ferme ou suit le nouveau nom, sans exception.
3. Carte ouverte puis changement de carte (autre colonie, caravane) : « présents » se recalcule sur la carte affichée, boutons grisés si personne.
4. Raccourci pressé pendant la saisie d'un texte (renommer une escouade) : aucun ordre ne part.
5. Coût par image : le dessin des onglets ne parcourt pas les pions de chaque escouade à chaque image.

---

### Task 1 : `SquadStatus` et `SquadOrders.StopAssault`

**Files:**
- Create: `Source/RadiusColonistBar/SquadStatus.cs`
- Modify: `Source/RadiusColonistBar/SquadOrders.cs` (ajout après `Assault`)

**Interfaces:**
- Produces: `SquadStatus.Of(int index)` → `SquadStatus.State` (struct : `present`, `total`, `drafted`, `fireAtWill`, `assault`, `hasCommonMode`, `commonMode`), `SquadStatus.Invalidate()`, `SquadOrders.StopAssault(int index)`.

- [ ] **Step 1 : écrire `SquadStatus.cs`**

```csharp
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

// Squad orders (Colonist Bar Plus): one squad's state on the current map, shared by the order card, the squad tabs
// and the hotkeys. Cached for a few frames: the tabs ask for it every frame.
internal static class SquadStatus
{
	internal struct State
	{
		public int present;

		public int total;

		public int drafted;

		public int fireAtWill;

		public int assault;

		public bool hasCommonMode;

		public AssaultMode commonMode;
	}

	private const int CacheFrames = 20;

	private static readonly Dictionary<int, State> cache = new Dictionary<int, State>();

	private static int cacheFrame = -1;

	private static Map cacheMap;

	internal static void Invalidate()
	{
		cache.Clear();
	}

	internal static State Of(int index)
	{
		Map currentMap = Find.CurrentMap;
		if (Time.frameCount - cacheFrame > CacheFrames || cacheMap != currentMap)
		{
			cache.Clear();
			cacheFrame = Time.frameCount;
			cacheMap = currentMap;
		}
		if (!cache.TryGetValue(index, out var value))
		{
			value = Compute(index, currentMap);
			cache[index] = value;
		}
		return value;
	}

	private static State Compute(int index, Map map)
	{
		State result = default(State);
		BarSquads get = BarSquads.Get;
		if (get == null || index < 0)
		{
			return result;
		}
		List<Pawn> list = SquadMembers.Able(map, index);
		result.present = list.Count;
		SquadAssault get2 = SquadAssault.Get;
		for (int i = 0; i < list.Count; i++)
		{
			Pawn pawn = list[i];
			if (pawn.Drafted)
			{
				result.drafted++;
			}
			if (pawn.drafter != null && pawn.drafter.FireAtWill)
			{
				result.fireAtWill++;
			}
			if (get2 != null && get2.TryGetMode(pawn, out var _))
			{
				result.assault++;
			}
		}
		result.hasCommonMode = SquadOrders.CommonMode(list, out result.commonMode);
		List<Pawn> list2 = Find.ColonistBar?.GetColonistsInOrder();
		if (list2 != null)
		{
			for (int j = 0; j < list2.Count; j++)
			{
				if (list2[j] != null && !list2[j].Dead && get.SquadIndexOf(list2[j]) == index)
				{
					result.total++;
				}
			}
		}
		result.total = Mathf.Max(result.total, result.present);
		return result;
	}
}
```

- [ ] **Step 2 : ajouter `StopAssault` dans `SquadOrders.cs`, juste après la méthode `Assault`**

```csharp
	// Squad orders (Colonist Bar Plus): ends the assault of the members on this map. They stay drafted.
	internal static void StopAssault(int index)
	{
		SquadAssault squadAssault = SquadAssault.Get;
		if (squadAssault == null)
		{
			return;
		}
		foreach (Pawn pawn in SquadMembers.Able(Find.CurrentMap, index))
		{
			Try(pawn, delegate
			{
				squadAssault.End(pawn);
			});
		}
		SquadStatus.Invalidate();
	}
```

Ajouter aussi `SquadStatus.Invalidate();` en dernière ligne de `Draft`, `Undraft`, `ToggleFireAtWill`, `Regroup` et `Assault`, pour que la carte montre le nouvel état dès l'image suivante.

- [ ] **Step 3 : build**

Run : `cd RadiusUIColonistBarPlus && ./deploy.sh 2>&1 | grep -E "Erreur|Avertissement|Publié"`
Expected : `0 Erreur(s)`, `6 Avertissement(s)`, `Publié : …`

- [ ] **Step 4 : message de commit préparé** — `Squad status cache and StopAssault`

### Task 2 : la carte d'ordres depuis la barre

**Files:**
- Create: `Source/RadiusColonistBar/SquadOrderCard.cs`
- Modify: `Source/RadiusColonistBar/RcbIsland.cs` (handler du clic droit, autour de `SquadTabAt`) ; `SquadTabAt` renvoie aussi le rect de l'onglet
- Delete: `Source/RadiusColonistBar/SquadOrderMenu.cs`
- Modify: `Languages/English/Keyed/RadiusColonistBar.xml`, `Languages/French/Keyed/RadiusColonistBarPlus.xml`

**Interfaces:**
- Consumes : `SquadStatus.Of`, `SquadOrders.*`, `SquadOrders.StopAssault`.
- Produces : `SquadOrderCard.Toggle(int index, Rect anchor)` (barre), `SquadOrderCard.OpenManaged(int index, BarSquads sq, Action refresh)` (Centre de commandement, tâche 3).

- [ ] **Step 1 : clés de texte**

Anglais, dans le bloc Colonist Bar Plus de `RadiusColonistBar.xml` :

```xml
  <RCB_Card_Present>{0}/{1} on this map</RCB_Card_Present>
  <RCB_Card_Drafted>Drafted</RCB_Card_Drafted>
  <RCB_Card_DraftedPart>Partly drafted</RCB_Card_DraftedPart>
  <RCB_Card_Undrafted>Undrafted</RCB_Card_Undrafted>
  <RCB_Card_Fire>Fire at will</RCB_Card_Fire>
  <RCB_Card_FirePart>Partly firing at will</RCB_Card_FirePart>
  <RCB_Card_FireOff>Holding fire</RCB_Card_FireOff>
  <RCB_Card_AssaultMode>Assault: {0}</RCB_Card_AssaultMode>
  <RCB_Card_AssaultPart>Partly assaulting</RCB_Card_AssaultPart>
  <RCB_Card_AssaultTitle>ASSAULT</RCB_Card_AssaultTitle>
  <RCB_Card_AssaultStopTip>Click the active mode to stop the assault. Members stay drafted.</RCB_Card_AssaultStopTip>
  <RCB_Card_Strongest>Strongest</RCB_Card_Strongest>
  <RCB_Card_Weakest>Weakest</RCB_Card_Weakest>
  <RCB_Card_Fleeing>Fleeing</RCB_Card_Fleeing>
  <RCB_Card_DraftShort>Draft</RCB_Card_DraftShort>
  <RCB_Card_UndraftShort>Undraft</RCB_Card_UndraftShort>
  <RCB_Card_FireShort>Fire at will</RCB_Card_FireShort>
  <RCB_Card_HoldShort>Hold fire</RCB_Card_HoldShort>
  <RCB_Card_RegroupShort>Regroup</RCB_Card_RegroupShort>
```

Français, dans `RadiusColonistBarPlus.xml` :

```xml
  <!-- Carte d'ordres -->
  <RCB_Card_Present>{0}/{1} sur la carte</RCB_Card_Present>
  <RCB_Card_Drafted>Mobilisée</RCB_Card_Drafted>
  <RCB_Card_DraftedPart>Mobilisée en partie</RCB_Card_DraftedPart>
  <RCB_Card_Undrafted>Démobilisée</RCB_Card_Undrafted>
  <RCB_Card_Fire>Tir à volonté</RCB_Card_Fire>
  <RCB_Card_FirePart>Tir à volonté en partie</RCB_Card_FirePart>
  <RCB_Card_FireOff>Feu cessé</RCB_Card_FireOff>
  <RCB_Card_AssaultMode>Assaut : {0}</RCB_Card_AssaultMode>
  <RCB_Card_AssaultPart>Assaut en partie</RCB_Card_AssaultPart>
  <RCB_Card_AssaultTitle>ASSAUT</RCB_Card_AssaultTitle>
  <RCB_Card_AssaultStopTip>Cliquer sur le mode actif arrête l'assaut. Les membres restent mobilisés.</RCB_Card_AssaultStopTip>
  <RCB_Card_Strongest>Plus forts</RCB_Card_Strongest>
  <RCB_Card_Weakest>Plus faibles</RCB_Card_Weakest>
  <RCB_Card_Fleeing>Fuyards</RCB_Card_Fleeing>
  <RCB_Card_DraftShort>Mobiliser</RCB_Card_DraftShort>
  <RCB_Card_UndraftShort>Démobiliser</RCB_Card_UndraftShort>
  <RCB_Card_FireShort>Tir à volonté</RCB_Card_FireShort>
  <RCB_Card_HoldShort>Cesser le feu</RCB_Card_HoldShort>
  <RCB_Card_RegroupShort>Regrouper</RCB_Card_RegroupShort>
```

Retirer des deux fichiers les clés qui ne servent plus après suppression de `SquadOrderMenu` : `RCB_Orders_Draft`, `RCB_Orders_Undraft`, `RCB_Orders_FireAtWillOn`, `RCB_Orders_FireAtWillOff`, `RCB_Orders_Regroup`, `RCB_Orders_Assault`, `RCB_Orders_AssaultStrongest`, `RCB_Orders_AssaultWeakest`, `RCB_Orders_AssaultFleeing`, `RCB_Orders_ActiveMark` — après avoir vérifié par `grep -rn "<clé>" Source` qu'aucun autre fichier ne les lit. Les infobulles `RCB_Orders_*Tip` et `RCB_Orders_NoMember` restent.

- [ ] **Step 2 : écrire `SquadOrderCard.cs`**

```csharp
using System;
using RadiusUI.Framework;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RadiusColonistBar;

// Squad orders (Colonist Bar Plus): the order card, opened by right-clicking a squad tab or from the Squads module.
// It re-reads the squad state every frame and stays open after an order, so the new state shows at once.
public class SquadOrderCard : Window
{
	private const float Width = 330f;

	private const float Pad = 12f;

	private const float BtnH = 30f;

	private const float Gap = 4f;

	private readonly int index;

	private readonly Rect anchor;

	private readonly bool managed;

	private readonly BarSquads? managedSquads;

	private readonly Action? refresh;

	// The outside click that closes a card and the right-click on its tab come in the same frame: without this the
	// tab would reopen the card it just closed.
	private static int closedIndex = -1;

	private static int closedFrame = -1;

	private static SquadOrderCard? open;

	public override Vector2 InitialSize => new Vector2(Width, ContentHeight());

	protected override float Margin => 0f;

	private SquadOrderCard(int index, Rect anchor, bool managed, BarSquads? squads, Action? refresh)
	{
		this.index = index;
		this.anchor = anchor;
		this.managed = managed;
		managedSquads = squads;
		this.refresh = refresh;
		doWindowBackground = false;
		drawShadow = false;
		doCloseX = false;
		closeOnClickedOutside = true;
		closeOnCancel = true;
		absorbInputAroundWindow = false;
		preventCameraMotion = false;
		onlyOneOfTypeAllowed = true;
		layer = WindowLayer.Super;
		soundAppear = null;
		soundClose = null;
	}

	internal static void Toggle(int index, Rect anchor)
	{
		if (open != null && open.IsOpen && open.index == index)
		{
			open.Close();
			return;
		}
		if (closedIndex == index && closedFrame >= Time.frameCount - 1)
		{
			return;
		}
		Find.WindowStack.Add(new SquadOrderCard(index, anchor, managed: false, null, null));
	}

	internal static void OpenManaged(int index, BarSquads squads, Action refresh)
	{
		Vector2 mousePositionOnUIInverted = UI.MousePositionOnUIInverted;
		Find.WindowStack.Add(new SquadOrderCard(index, new Rect(mousePositionOnUIInverted.x, mousePositionOnUIInverted.y, 0f, 0f), managed: true, squads, refresh));
	}

	public override void PostOpen()
	{
		base.PostOpen();
		open = this;
	}

	public override void PostClose()
	{
		base.PostClose();
		closedIndex = index;
		closedFrame = Time.frameCount;
		if (open == this)
		{
			open = null;
		}
	}

	protected override void SetInitialSizeAndPosition()
	{
		Vector2 initialSize = InitialSize;
		float x = Mathf.Clamp(anchor.x, 4f, (float)UI.screenWidth - initialSize.x - 4f);
		float y = Mathf.Clamp(anchor.yMax + 4f, 4f, (float)UI.screenHeight - initialSize.y - 4f);
		windowRect = new Rect(x, y, initialSize.x, initialSize.y).Rounded();
	}

	private float ContentHeight()
	{
		// Header 24 + state 20 + 2 rows of orders + assault title 18 + assault row, plus the footer when managed.
		float num = Pad + 24f + 20f + 8f + (BtnH * 2f + Gap) + 10f + 18f + BtnH + Pad;
		if (managed)
		{
			num += 8f + BtnH;
		}
		return num;
	}

	public override void DoWindowContents(Rect inRect)
	{
		BarSquads get = BarSquads.Get;
		if (get == null || index < 0 || index >= get.names.Count)
		{
			Close();
			return;
		}
		RcbSpatial.Rounded(inRect, Palette.WithAlpha(RcbPalette.Ink, 0.96f), 14f);
		SquadStatus.State state = SquadStatus.Of(index);
		bool flag = state.present > 0;
		float x = inRect.x + Pad;
		float num = inRect.width - Pad * 2f;
		float y = inRect.y + Pad;
		DrawHeader(new Rect(x, y, num, 24f), get, state);
		y += 24f;
		RcbFont.Label(new Rect(x, y, num, 20f), flag ? StateLine(state) : "RCB_Orders_NoMember".Translate().ToString(), bold: false, GameFont.Tiny, RcbPalette.TextDim);
		y += 28f;
		float num2 = (num - Gap) / 2f;
		bool flag2 = flag && state.drafted == state.present;
		if (Button(new Rect(x, y, num2, BtnH), IconSet.Action.Draft, (flag2 ? "RCB_Card_UndraftShort" : "RCB_Card_DraftShort").Translate(), (flag2 ? "RCB_Orders_UndraftTip" : "RCB_Orders_DraftTip").Translate(), flag, active: false))
		{
			if (flag2)
			{
				SquadOrders.Undraft(index);
			}
			else
			{
				SquadOrders.Draft(index);
			}
		}
		bool flag3 = flag && state.fireAtWill == state.present;
		if (Button(new Rect(x + num2 + Gap, y, num2, BtnH), IconSet.Alert.Threat, (flag3 ? "RCB_Card_HoldShort" : "RCB_Card_FireShort").Translate(), "RCB_Orders_FireAtWillTip".Translate(), flag, active: false))
		{
			SquadOrders.ToggleFireAtWill(index);
		}
		y += BtnH + Gap;
		if (Button(new Rect(x, y, num2, BtnH), IconSet.Action.DefPos, "RCB_Card_RegroupShort".Translate(), "RCB_Orders_RegroupTip".Translate(), flag, active: false))
		{
			SquadOrders.Regroup(index);
		}
		y += BtnH + 10f;
		RcbPalette.MicroLabel(new Rect(x, y, num, 18f), "RCB_Card_AssaultTitle".Translate(), RcbPalette.TextDim2, TextAnchor.MiddleLeft, shadow: false);
		y += 18f;
		float num3 = (num - Gap * 2f) / 3f;
		AssaultButton(new Rect(x, y, num3, BtnH), AssaultMode.Strongest, "RCB_Card_Strongest", "RCB_Orders_AssaultStrongestTip", state, flag);
		AssaultButton(new Rect(x + num3 + Gap, y, num3, BtnH), AssaultMode.Weakest, "RCB_Card_Weakest", "RCB_Orders_AssaultWeakestTip", state, flag);
		AssaultButton(new Rect(x + (num3 + Gap) * 2f, y, num3, BtnH), AssaultMode.Fleeing, "RCB_Card_Fleeing", "RCB_Orders_AssaultFleeingTip", state, flag);
		y += BtnH;
		if (managed && managedSquads != null)
		{
			y += 8f;
			DrawManageRow(new Rect(x, y, num, BtnH), managedSquads);
		}
	}

	private void DrawHeader(Rect r, BarSquads squads, SquadStatus.State state)
	{
		RcbSpatial.Dot(new Rect(r.x, r.center.y - 5f, 10f, 10f), squads.SquadColor(index));
		string text = "RCB_Card_Present".Translate(state.present, state.total);
		float num = RcbFont.Width(text, bold: false, GameFont.Tiny) + 4f;
		RcbFont.Label(new Rect(r.x + 16f, r.y, r.width - 16f - num, r.height), RcbFont.Fit(squads.SquadName(index) ?? "", r.width - 16f - num, bold: true, GameFont.Small), bold: true, GameFont.Small, RcbPalette.Stat);
		RcbFont.Label(new Rect(r.xMax - num, r.y, num, r.height), text, bold: false, GameFont.Tiny, RcbPalette.TextDim, TextAnchor.MiddleRight);
	}

	private static string StateLine(SquadStatus.State s)
	{
		string text = ((s.drafted == s.present) ? "RCB_Card_Drafted" : ((s.drafted > 0) ? "RCB_Card_DraftedPart" : "RCB_Card_Undrafted")).Translate();
		string text2 = ((s.fireAtWill == s.present) ? "RCB_Card_Fire" : ((s.fireAtWill > 0) ? "RCB_Card_FirePart" : "RCB_Card_FireOff")).Translate();
		string text3 = text + " · " + text2;
		if (s.hasCommonMode)
		{
			text3 = text3 + " · " + "RCB_Card_AssaultMode".Translate(ModeLabel(s.commonMode));
		}
		else if (s.assault > 0)
		{
			text3 = text3 + " · " + "RCB_Card_AssaultPart".Translate();
		}
		return text3;
	}

	private static string ModeLabel(AssaultMode mode)
	{
		return mode switch
		{
			AssaultMode.Weakest => "RCB_Card_Weakest".Translate(), 
			AssaultMode.Fleeing => "RCB_Card_Fleeing".Translate(), 
			_ => "RCB_Card_Strongest".Translate(), 
		};
	}

	private void AssaultButton(Rect r, AssaultMode mode, string labelKey, string tipKey, SquadStatus.State state, bool enabled)
	{
		bool flag = state.hasCommonMode && state.commonMode == mode;
		string tip = tipKey.Translate() + (flag ? ("\n\n" + "RCB_Card_AssaultStopTip".Translate()) : "");
		if (Button(r, IconSet.Common.Sword, labelKey.Translate(), tip, enabled, flag))
		{
			if (flag)
			{
				SquadOrders.StopAssault(index);
			}
			else
			{
				SquadOrders.Assault(index, mode);
			}
		}
	}

	private void DrawManageRow(Rect r, BarSquads squads)
	{
		float num = (r.width - Gap * 3f) / 4f;
		int count = squads.names.Count;
		if (Button(new Rect(r.x, r.y, num, r.height), null, "RCB_CC_SquadRename".Translate(), null, enabled: true, active: false))
		{
			Find.WindowStack.Add(new Dialog_RenameSquad(squads, index));
		}
		if (Button(new Rect(r.x + (num + Gap), r.y, num, r.height), null, "RCB_CC_SquadMoveUp".Translate(), null, index > 0, active: false))
		{
			squads.MoveSquad(index, -1);
			Changed(closeCard: true);
		}
		if (Button(new Rect(r.x + (num + Gap) * 2f, r.y, num, r.height), null, "RCB_CC_SquadMoveDown".Translate(), null, index < count - 1, active: false))
		{
			squads.MoveSquad(index, 1);
			Changed(closeCard: true);
		}
		if (Button(new Rect(r.x + (num + Gap) * 3f, r.y, num, r.height), null, "RCB_CC_SquadDisband".Translate(), null, enabled: true, active: false))
		{
			squads.Disband(index);
			Changed(closeCard: true);
		}
	}

	// Moving or disbanding changes the squad indices: the card would point at another squad, so it closes.
	private void Changed(bool closeCard)
	{
		SquadStatus.Invalidate();
		refresh?.Invoke();
		if (closeCard)
		{
			Close();
		}
	}

	private static bool Button(Rect r, Texture2D? icon, string label, string? tip, bool enabled, bool active)
	{
		bool flag = enabled && Mouse.IsOver(r);
		Color c = (active ? RcbPalette.Accent : Color.Lerp(RcbPalette.Ink, Color.white, flag ? 0.16f : 0.08f));
		RcbSpatial.Rounded(r, enabled ? c : Palette.WithAlpha(c, 0.45f));
		Color color = ((!enabled) ? RcbPalette.TextDim2 : (active ? Palette.Flat.Rail : (flag ? RcbPalette.Stat : RcbPalette.TextDim)));
		float num = r.x + 8f;
		if (icon != null)
		{
			Rect position = new Rect(r.x + 6f, r.center.y - 8f, 16f, 16f);
			GUI.color = color;
			GUI.DrawTexture(position, icon);
			GUI.color = Color.white;
			num = position.xMax + 5f;
		}
		float num2 = Mathf.Max(10f, r.xMax - num - 6f);
		RcbFont.Label(new Rect(num, r.y, num2, r.height), RcbFont.Fit(label, num2, bold: false, GameFont.Small), bold: false, GameFont.Small, color);
		if (!tip.NullOrEmpty() && Mouse.IsOver(r))
		{
			TooltipHandler.TipRegion(r, tip);
		}
		if (enabled && Widgets.ButtonInvisible(r))
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			return true;
		}
		return false;
	}
}
```

Notes pour l'exécutant :
- `RcbPalette.Accent` est une propriété (`RcbPalette.cs:58`) ; `Palette.Flat.Rail` sert d'encre sur fond d'accent.
- `IconSet.Action.Draft`, `IconSet.Alert.Threat`, `IconSet.Action.DefPos`, `IconSet.Common.Sword` sont des `RadiusIcon` convertis implicitement en `Texture2D` (comme `HoverPopout.InitIcons`). Si la conversion implicite n'existe pas dans le sens `RadiusIcon` → `Texture2D?` nullable, passer par une variable `Texture2D`.
- `Dialog_RenameSquad(BarSquads, int)` : signature à confirmer dans `Dialog_RenameSquad.cs` ; le module l'appelle déjà ainsi.
- Si `Window.Margin` n'est pas surchargeable dans cette version, mettre `inRect` à zéro marge en dessinant dans `new Rect(0, 0, windowRect.width, windowRect.height)`.

- [ ] **Step 3 : brancher le clic droit dans `RcbIsland.cs`**

Changer `SquadTabAt` pour rendre aussi le rect de l'onglet :

```csharp
	private static int SquadTabAt(Vector2 pos, out Rect tab)
	{
		tab = default(Rect);
		for (int i = 0; i < tabRects.Count && i < railTok.Count; i++)
		{
			if (tabRects[i].Contains(pos) && RcbCohorts.TokenIsSquad(railTok[i], out var si))
			{
				tab = tabRects[i];
				return si;
			}
		}
		return -1;
	}
```

Dans le handler du clic droit, remplacer :

```csharp
				int squadTab = SquadTabAt(current2.mousePosition);
				if (squadTab >= 0)
				{
					SoundDefOf.Click.PlayOneShotOnCamera();
					SquadOrderMenu.Open(squadTab);
				}
```

par :

```csharp
				int squadTab = SquadTabAt(current2.mousePosition, out var tabRect);
				if (squadTab >= 0)
				{
					SoundDefOf.Click.PlayOneShotOnCamera();
					SquadOrderCard.Toggle(squadTab, tabRect);
				}
```

Mettre à jour le commentaire au-dessus (« right-click a squad tab for its order card »).

- [ ] **Step 4 : supprimer `SquadOrderMenu.cs`** et faire passer `Module_Squads.SquadMenu` temporairement par la carte (la tâche 3 le finalise) : remplacer tout le corps de `SquadMenu` par `SquadOrderCard.OpenManaged(si, sq, ctx.requestRefresh);`.

- [ ] **Step 5 : build** — même commande et même attendu qu'en tâche 1. `grep -rn "SquadOrderMenu" Source` : attendu, aucun résultat.

- [ ] **Step 6 : message de commit préparé** — `Squad order card replaces the vanilla float menu`

### Task 3 : le module Escouades

**Files:**
- Modify: `Source/RadiusColonistBar/Module_Squads.cs` (`SquadMenu`)

**Interfaces:**
- Consumes : `SquadOrderCard.OpenManaged(int, BarSquads, Action)`.

- [ ] **Step 1 : finaliser `SquadMenu`**

```csharp
	// Squad orders (Colonist Bar Plus): the order card, with the squad's management in its footer.
	private static void SquadMenu(BarSquads sq, int si, CCContext ctx)
	{
		SquadOrderCard.OpenManaged(si, sq, ctx.requestRefresh);
	}
```

Vérifier le type de `ctx.requestRefresh` (`grep -n "requestRefresh" CCContext.cs` ou équivalent) : si ce n'est pas un `Action`, l'envelopper (`delegate { ctx.requestRefresh(); }`).

- [ ] **Step 2 : build** — attendu identique.

- [ ] **Step 3 : message de commit préparé** — `Squads module opens the order card`

### Task 4 : état sur l'onglet d'escouade

**Files:**
- Modify: `Source/RadiusColonistBar/RcbIsland.cs` (`TabWidth(int, bool)` ligne ~1171 ; boucle de dessin des onglets ligne ~1001)

**Interfaces:**
- Consumes : `SquadStatus.Of(int)`.

- [ ] **Step 1 : place réservée dans `TabWidth`**

```csharp
	private static float TabWidth(int t, bool shortNames)
	{
		TabCaption(t, shortNames);
		float num = ((t >= 100) ? (shortNames ? sqCapSW[t - 100] : sqCapW[t - 100]) : tabCapWidth[t]);
		return num + 14f + ((RcbCohorts.TokenSeverity(t) >= 0) ? 9f : 0f) + SquadMarksWidth(t);
	}

	// Squad orders (Colonist Bar Plus): room for the drafted dot and the assault sword on a squad tab.
	private static float SquadMarksWidth(int t)
	{
		if (!RcbCohorts.TokenIsSquad(t, out var si))
		{
			return 0f;
		}
		SquadStatus.State state = SquadStatus.Of(si);
		return ((state.drafted > 0) ? 8f : 0f) + ((state.assault > 0) ? 13f : 0f);
	}
```

- [ ] **Step 2 : dessin dans la boucle des onglets**

Dans la boucle `for (int i = 0; i < railTok.Count; i++)`, remplacer le calcul de `num10` et le label :

```csharp
			int num9 = RcbCohorts.TokenSeverity(num7);
			float num10 = ((num9 >= 0) ? 9f : 0f);
			float marks = SquadMarksWidth(num7);
			RcbPalette.MicroLabel(new Rect(rect.x + 7f, rect.y, rect.width - 14f - num10 - marks, rect.height), TabCaption(num7, shortNames), (flag || flag2) ? RcbPalette.Stat : Dim2, TextAnchor.MiddleLeft, shadow: false);
			if (marks > 0f)
			{
				DrawSquadMarks(rect.xMax - 7f - num10 - marks, rect.center.y, num7);
			}
```

et ajouter la méthode :

```csharp
	// Squad orders (Colonist Bar Plus): drafted dot, then the assault sword.
	private static void DrawSquadMarks(float x, float cy, int t)
	{
		if (!RcbCohorts.TokenIsSquad(t, out var si))
		{
			return;
		}
		SquadStatus.State state = SquadStatus.Of(si);
		if (state.drafted > 0)
		{
			RcbSpatial.Dot(new Rect(x + 1f, cy - 2.5f, 5f, 5f), RcbPalette.Accent);
			x += 8f;
		}
		if (state.assault > 0)
		{
			GUI.color = RcbPalette.Accent;
			GUI.DrawTexture(new Rect(x, cy - 5.5f, 11f, 11f), IconSet.Common.Sword);
			GUI.color = Color.white;
		}
	}
```

`using RadiusUI.Framework;` est déjà présent dans `RcbIsland.cs` (utilise `CardChrome`) ; sinon l'ajouter.

- [ ] **Step 3 : build** — attendu identique.

- [ ] **Step 4 : message de commit préparé** — `Squad tabs show drafted and assault state`

### Task 5 : épée sur le portrait

**Files:**
- Modify: `Source/RadiusColonistBar/RcbIsland.cs` (bloc des badges du portrait, après le `NumBox` de Defensive Positions, ligne ~1882)

- [ ] **Step 1 : ajouter le badge**

Juste après le bloc `if (s.showSquadBadges && DefensivePositionsCompat.Active) { … }` :

```csharp
		// Squad orders (Colonist Bar Plus): a sword in the bottom-right corner while the pawn follows an assault.
		SquadAssault squadAssault = SquadAssault.Get;
		if (squadAssault != null && squadAssault.TryGetMode(p, out var _))
		{
			BadgeLayer.Icon(new Rect(rect4.xMax - num18 - 2f, rect4.yMax - num18 - 2f, num18, num18), IconSet.Common.Sword, RcbPalette.Accent);
		}
```

Le coin bas droit est libre : le haut gauche porte l'état (`PawnStatusUtil`), le haut droit le numéro Defensive Positions, le bas de la carte les marques.

- [ ] **Step 2 : build** — attendu identique.

- [ ] **Step 3 : message de commit préparé** — `Assault sword on bar portraits`

### Task 6 : raccourcis clavier

**Files:**
- Create: `Defs/KeyBindings_Squads.xml`
- Create: `Source/RadiusColonistBar/SquadHotkeys.cs`
- Create: `Languages/French/DefInjected/KeyBindingCategoryDef/Squads.xml`, `Languages/French/DefInjected/KeyBindingDef/Squads.xml`
- Modify: `Source/RadiusColonistBar/Patch_ColonistBarOnGUI_Popout.cs`
- Modify: `Languages/English/Keyed/RadiusColonistBar.xml`, `Languages/French/Keyed/RadiusColonistBarPlus.xml`

- [ ] **Step 1 : defs**

`Defs/KeyBindings_Squads.xml` :

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <!-- Squad orders (Colonist Bar Plus). No default keys: vanilla binds almost every key (see KeyBindings.xml).
       The defNames carry the players' bindings: never rename them once published. -->
  <KeyBindingCategoryDef>
    <defName>RCB_Squads</defName>
    <label>Radius UI - Squads</label>
    <description>Orders for the squads of the selected pawns.</description>
  </KeyBindingCategoryDef>

  <KeyBindingDef>
    <defName>RCB_SquadDraft</defName>
    <label>draft / undraft squad</label>
    <category>RCB_Squads</category>
  </KeyBindingDef>
  <KeyBindingDef>
    <defName>RCB_SquadFireAtWill</defName>
    <label>fire at will / hold fire</label>
    <category>RCB_Squads</category>
  </KeyBindingDef>
  <KeyBindingDef>
    <defName>RCB_SquadRegroup</defName>
    <label>regroup on the leader</label>
    <category>RCB_Squads</category>
  </KeyBindingDef>
  <KeyBindingDef>
    <defName>RCB_SquadAssaultStrongest</defName>
    <label>assault: strongest first</label>
    <category>RCB_Squads</category>
  </KeyBindingDef>
  <KeyBindingDef>
    <defName>RCB_SquadAssaultWeakest</defName>
    <label>assault: weakest first</label>
    <category>RCB_Squads</category>
  </KeyBindingDef>
  <KeyBindingDef>
    <defName>RCB_SquadAssaultFleeing</defName>
    <label>assault: chase the fleeing</label>
    <category>RCB_Squads</category>
  </KeyBindingDef>
</Defs>
```

Vérifier dans le jeu (`ilspycmd -t Verse.KeyBindingDef`) qu'un `KeyBindingDef` sans `defaultKeyCodeA` est accepté (valeur `None`) ; sinon mettre `<defaultKeyCodeA>None</defaultKeyCodeA>`.

DefInjected français :

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>
  <RCB_Squads.label>Radius UI - Escouades</RCB_Squads.label>
  <RCB_Squads.description>Ordres pour les escouades des pions sélectionnés.</RCB_Squads.description>
</LanguageData>
```

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>
  <RCB_SquadDraft.label>mobiliser / démobiliser l'escouade</RCB_SquadDraft.label>
  <RCB_SquadFireAtWill.label>tir à volonté / cesser le feu</RCB_SquadFireAtWill.label>
  <RCB_SquadRegroup.label>regrouper sur le chef</RCB_SquadRegroup.label>
  <RCB_SquadAssaultStrongest.label>assaut : les plus forts d'abord</RCB_SquadAssaultStrongest.label>
  <RCB_SquadAssaultWeakest.label>assaut : les plus faibles d'abord</RCB_SquadAssaultWeakest.label>
  <RCB_SquadAssaultFleeing.label>assaut : poursuivre les fuyards</RCB_SquadAssaultFleeing.label>
</LanguageData>
```

Clés Keyed : anglais `<RCB_Hotkey_NoSquad>No squad selected</RCB_Hotkey_NoSquad>`, français `<RCB_Hotkey_NoSquad>Aucune escouade sélectionnée</RCB_Hotkey_NoSquad>`.

Le dossier `Languages/French/DefInjected/` du fork n'entre pas en conflit avec la traduction française : elle ne traduit que `RCB_FollowCam`.

- [ ] **Step 2 : `SquadHotkeys.cs`**

```csharp
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

// Squad orders (Colonist Bar Plus): hotkeys for the squads of the selected pawns. Read with the bar's other keys
// (Patch_ColonistBarOnGUI_Popout), never while a text field has focus.
internal static class SquadHotkeys
{
	private static KeyBindingDef? draft;

	private static KeyBindingDef? fire;

	private static KeyBindingDef? regroup;

	private static KeyBindingDef? strongest;

	private static KeyBindingDef? weakest;

	private static KeyBindingDef? fleeing;

	private static bool bound;

	private static readonly List<int> targets = new List<int>();

	internal static void OnGUI()
	{
		if (Event.current.type != EventType.KeyDown || GUIUtility.keyboardControl != 0)
		{
			return;
		}
		Bind();
		if (Pressed(draft))
		{
			Run(delegate(int i)
			{
				SquadStatus.State state = SquadStatus.Of(i);
				if (state.present > 0 && state.drafted == state.present)
				{
					SquadOrders.Undraft(i);
				}
				else
				{
					SquadOrders.Draft(i);
				}
			});
		}
		else if (Pressed(fire))
		{
			Run(SquadOrders.ToggleFireAtWill);
		}
		else if (Pressed(regroup))
		{
			Run(SquadOrders.Regroup);
		}
		else if (Pressed(strongest))
		{
			Run(delegate(int i)
			{
				SquadOrders.Assault(i, AssaultMode.Strongest);
			});
		}
		else if (Pressed(weakest))
		{
			Run(delegate(int i)
			{
				SquadOrders.Assault(i, AssaultMode.Weakest);
			});
		}
		else if (Pressed(fleeing))
		{
			Run(delegate(int i)
			{
				SquadOrders.Assault(i, AssaultMode.Fleeing);
			});
		}
	}

	private static void Bind()
	{
		if (!bound)
		{
			bound = true;
			draft = DefDatabase<KeyBindingDef>.GetNamedSilentFail("RCB_SquadDraft");
			fire = DefDatabase<KeyBindingDef>.GetNamedSilentFail("RCB_SquadFireAtWill");
			regroup = DefDatabase<KeyBindingDef>.GetNamedSilentFail("RCB_SquadRegroup");
			strongest = DefDatabase<KeyBindingDef>.GetNamedSilentFail("RCB_SquadAssaultStrongest");
			weakest = DefDatabase<KeyBindingDef>.GetNamedSilentFail("RCB_SquadAssaultWeakest");
			fleeing = DefDatabase<KeyBindingDef>.GetNamedSilentFail("RCB_SquadAssaultFleeing");
		}
	}

	private static bool Pressed(KeyBindingDef? def)
	{
		return def != null && def.MainKey != KeyCode.None && def.KeyDownEvent;
	}

	// Each squad of the selection once. Assault selects the squad's members, so the targets are read first.
	private static void Run(System.Action<int> order)
	{
		Event.current.Use();
		targets.Clear();
		BarSquads get = BarSquads.Get;
		if (get != null)
		{
			foreach (Pawn selectedPawn in Find.Selector.SelectedPawns)
			{
				int num = get.SquadIndexOf(selectedPawn);
				if (num >= 0 && !targets.Contains(num))
				{
					targets.Add(num);
				}
			}
		}
		if (targets.Count == 0)
		{
			Messages.Message("RCB_Hotkey_NoSquad".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			return;
		}
		for (int i = 0; i < targets.Count; i++)
		{
			order(targets[i]);
		}
	}
}
```

Note : `Find.Selector.SelectedPawns` renvoie une liste qu'`Assault` vide (`ClearSelection`) ; la liste `targets` est remplie avant tout ordre, ce qui protège la boucle.

- [ ] **Step 3 : appeler `SquadHotkeys.OnGUI` dans `Patch_ColonistBarOnGUI_Popout.Postfix`**, juste après le bloc `FollowCam.OnGUI()` :

```csharp
		try
		{
			SquadHotkeys.OnGUI();
		}
		catch
		{
		}
```

- [ ] **Step 4 : build** — attendu identique ; en plus, `grep -c "<KeyBindingDef>" Defs/KeyBindings_Squads.xml` : attendu `6`.

- [ ] **Step 5 : message de commit préparé** — `Squad order hotkeys (no default keys)`

### Task 7 : documentation et contrôle en jeu

**Files:**
- Modify: `README.md`, `CHANGELOG.md`

- [ ] **Step 1 : README** — section « Ordres d'escouade » : la carte remplace le menu ; état sur l'onglet (point, épée) ; épée sur le portrait ; raccourcis sans touche par défaut dans Options > Commandes > « Radius UI - Escouades », qui visent l'escouade des pions sélectionnés.

- [ ] **Step 2 : CHANGELOG** — section 1.0.0 : une ligne par point (carte d'ordres, module, état d'onglet, épée de portrait, raccourcis).

- [ ] **Step 3 : build et publication** — `./deploy.sh`, attendu identique.

- [ ] **Step 4 : contrôle en jeu (à faire par le joueur)** — les huit points de la section 5 de la spec, plus les cinq points de « Review Focus » ci-dessus.

- [ ] **Step 5 : message de commit préparé** — `Docs: order card, tab state, portrait sword, hotkeys`
