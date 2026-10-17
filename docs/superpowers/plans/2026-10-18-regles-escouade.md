# Onglets et règles d'escouade — plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Carte d'ordres en deux onglets (Combat, Règles) ; l'onglet Règles pose tenue, nourriture, drogues, soins médicaux, zone et réaction aux menaces sur toute l'escouade.

**Architecture:** `SquadRules` (statique) lit et pose les règles vanilla sur les membres ; la carte lit ses valeurs dans son instantané (6 images ou après un ordre) et dessine l'onglet choisi dans une zone de hauteur fixe. Le menu Règles du portrait passe ses textes par des clés.

**Tech Stack:** C# `net472`, RimWorld 1.6 (`outfitDatabase`, `foodRestrictionDatabase`, `drugPolicyDatabase`, `Pawn_PlayerSettings`, `Dialog_Manage*`), helpers de la barre (`RcbFloatMenu`, `RcbPalette`, `RcbFont`, `CardChrome`).

**Spec:** `docs/superpowers/specs/2026-10-18-regles-escouade-design.md`

**Note d'exécution (2026-10-18) :** `OpenEditor(Rule, List<Pawn>)` (et non `OpenEditor(int, Rule)`), `Shared(members, rule, into, out mixed)` remplit la liste des membres concernés, `OpenMenu(index, rule, current)` reçoit la valeur commune ; la ligne des vêtements forcés est sous les six règles. Le code fait foi.

## Global Constraints

- Rien de sauvegardé ; aucun `GameComponent`, `MapComponent`, `Lord` ou `DutyDef` neuf.
- Style ILSpy du code existant, commentaires en anglais seulement sur le code ajouté.
- Clés neuves `RCB_Tab_*`, `RCB_Rules_*`, `RCB_Policies_*` : anglais dans `Languages/English/Keyed/RadiusColonistBar.xml`, français dans `Languages/French/Keyed/RadiusColonistBarPlus.xml`. Clés vanilla reprises : `NoAreaAllowed`, `AssignTabEdit`.
- Preuve : `./deploy.sh` (0 erreur, 6 avertissements), contrôles en jeu de la spec § 7, clés sans doublon (`grep -o '<RCB_[A-Za-z_]*>' … | sort | uniq -d` vide). Pas de commit sans demande.

## Review Focus

1. Membre en caravane : reçoit tenue, nourriture, drogues, soins, menaces ; pas la zone.
2. Membre sans `outfits` / `drugs` (esclave selon les DLC, enfant) : ignoré sans exception.
3. Fenêtre vanilla ouverte depuis Modifier : reçoit les clics (carte fermée avant).
4. Passage d'onglet : hauteur de la carte inchangée.
5. Escouade sans membre vivant : lignes grisées, aucune exception.

---

### Task 1 : `SquadRules`

**Files:** Create `Source/RadiusColonistBar/SquadRules.cs`

- Produces :
  - `internal enum SquadRules.Rule { Apparel, Food, Drugs, Medical, Area, Hostility }`
  - `internal static List<Pawn> Members(int index)` : membres vivants toutes cartes ; `internal static List<Pawn> OnMap(int index, Map map)` pour la zone.
  - `internal static bool Applies(Pawn p, Rule rule)` : suivi présent (zone : `SupportsAllowedAreas`).
  - `internal static string ValueLabel(Pawn p, Rule rule)` ; `internal static string Shared(List<Pawn> pawns, Rule rule, out bool mixed)` (null si aucun concerné).
  - `internal static void OpenMenu(int index, Rule rule)` : `RcbFloatMenu` des choix, actuelle suivie de `RCB_Rules_Current`.
  - `internal static bool HasEditor(Rule rule)`, `internal static void OpenEditor(int index, Rule rule)`.
  - `internal static int ForcedCount(List<Pawn> pawns)`, `internal static void ReleaseForced(int index)`.
- [ ] Écrire la classe ; chaque pose dans `SquadOrders.Try`, message `RCB_Rules_Set` ({0} règle, {1} valeur, {2} nombre), `SquadStatus.Invalidate()`.

### Task 2 : onglets et onglet Règles dans la carte

**Files:** Modify `Source/RadiusColonistBar/SquadOrderCard.cs`

- [ ] `ContentHeight` : hauteur commune + bande d'onglets (22 + 8) + max(Combat, Règles).
- [ ] `private static int lastTab` ; `DrawTabs(Rect)` ; zone d'onglet de hauteur fixe ; Members et ligne de gestion sous cette zone.
- [ ] Instantané : pour chaque règle, valeur commune, mixte, nombre concernés ; nombre de vêtements forcés.
- [ ] `DrawRules` : six lignes (§ 2), ligne « vêtements forcés » si > 0, infobulles détaillées construites au survol.

### Task 3 : menu Règles du portrait

**Files:** Modify `Source/RadiusColonistBar/Patch_HandleClicks_RightClickMenu.cs`

- [ ] Remplacer les textes en dur par `RCB_Policies_*` et `RCB_Rules_Mixed`.

### Task 4 : textes et documentation

- [ ] Clés EN + FR ; README section « Règles » ; CHANGELOG « 1.3.0 — non publiée » ; ROADMAP (4a code fait, 4b à faire) ; `check_translations.py`.
