# Ordres médicaux d'escouade — plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Trois ordres d'escouade (Secourir, Soigner, Au lit) dans une section « Soins » de la carte d'ordres, avec aperçu au survol et trois raccourcis.

**Architecture:** Une classe statique `SquadMedical` sur le modèle de `SquadStations` : listes de patients bon marché pour l'affichage, appariement patient-membre au clic, jobs vanilla par `TryTakeOrderedJob`. La carte d'ordres gagne une section du gabarit de l'assaut. Rien de sauvegardé.

**Tech Stack:** C# `net472`, RimWorld 1.6 (`HealthAIUtility`, `RestUtility`, `JobDefOf.Rescue/TendPatient/LayDown`, `GenDraw`), Radius UI Framework (`IconSet`, `Palette`), helpers de la barre (`RcbPalette`, `RcbFont`).

**Spec:** `docs/superpowers/specs/2026-10-17-ordres-medicaux-design.md`

## Global Constraints

- Aucune ligne de [LTO] Colony Groups reprise.
- Aucune donnée sauvegardée, aucun `Lord`, `DutyDef`, `MapComponent` ni `GameComponent` neuf.
- defNames des raccourcis : `RCB_SquadRescue`, `RCB_SquadTend`, `RCB_SquadToBed`, à ne plus renommer après publication.
- Style ILSpy du code existant (`num`, `flag`, `get`…), commentaires en anglais seulement sur le code ajouté.
- Textes neufs : clés `RCB_Medical_*`, anglais dans `Languages/English/Keyed/RadiusColonistBar.xml`, français dans `Languages/French/Keyed/RadiusColonistBarPlus.xml`. Clés d'origine reprises sans redéfinition : `RCB_Cond_BleedingOut`, `RCB_Cond_Downed`, `RCB_Cond_NeedsTend` ; clés vanilla : `CannotRescue`, `NoNonPrisonerBed`, `NoSlaveBed`.
- Pas de tests automatisés. Preuve : `./deploy.sh` (attendu `0 Erreur(s)`, `6 Avertissement(s)`, ligne `Publié :`), puis contrôles en jeu de la spec § 6. Pas de `git commit` sans demande explicite.

## Review Focus

1. Bébé de la colonie (toujours « à terre ») : absent de Secourir. Filtre `DownedIsIncidental` en tâche 1.
2. Esclave à terre : porté vers un lit d'esclave (`GuestStatus.Slave`), message `NoSlaveBed` sinon. Tâche 1.
3. Lit double, trois patients dans le même clic : au plus deux pions vers ce lit. Compteur de places en tâche 1.
4. Deuxième clic pendant un secours : les membres déjà en `Rescue`/`TendPatient` ne sont pas réaffectés. Tâche 1.
5. Carte fermée pendant un survol : contours disparus à l'image suivante (règle `hoverFrame` de `SquadStations`). Tâche 1.

---

### Task 1 : `SquadMedical`

**Files:**
- Create: `Source/RadiusColonistBar/SquadMedical.cs`
- Modify: `Source/RadiusColonistBar/BarSquads.cs` (`GameComponentUpdate`, l. 363)

**Interfaces:**
- Consumes : `SquadMembers.Able(Map, int)`, `SquadOrders.Try(Pawn, Action)`, `SquadAssault.Get?.End(Pawn)`, `SquadStatus.Invalidate()`, `PawnStatusUtil.DownedIsIncidental(Pawn)`, `PawnUiCache.Get(Pawn).needsTend`, `BarSquads.SquadColor(int)`, `BarSquads.SquadIndexOf(Pawn)`.
- Produces :
  - `internal enum SquadMedical.Kind { Rescue, Tend, Bed }`
  - `internal static List<Pawn> Targets(Map map, int index, Kind kind)` : patients (Rescue, Tend) ou membres concernés (Bed), triés par priorité, sans test de chemin ni de lit. Tend lit `needsTend` du cache.
  - `internal static bool AnyDoctor(List<Pawn> members)` : un membre apte peut soigner (Médecin actif, manipulation).
  - `internal static void Rescue(int index)`, `Tend(int index)`, `ToBed(int index)`.
  - `internal static void Hover(int index, List<Pawn> pawns)`, `internal static void DrawHover()`.
  - `internal static string Condition(Pawn p)` : texte « état » de l'infobulle (clé `RCB_Cond_*`).

- [ ] **Step 1 : listes et priorité.** Pions soignables = `FreeColonistsSpawned` + `SlavesOfColonySpawned` sans doublon. Clé de tri : (hémorragie mortelle ? 0 : 1, membre ? 0 : 1, `TicksUntilDeathDueToBloodLoss`, `thingIDNumber`). Hémorragie mortelle = critère de `RcbAlertBand.Worst` (`BleedRateTotal > 0.01f` et `< 60000`). Rescue : `WantsToBeRescued && !DownedIsIncidental && medCare != NoCare`. Tend (affichage) : `needsTend && !InAggroMentalState`. Bed : membres aptes avec `ShouldSeekMedicalRest`.
- [ ] **Step 2 : appariement.** Membres libres = `Able` sans job en cours `Rescue` ni `TendPatient`. Pour chaque patient dans l'ordre : membre libre le plus proche (`DistanceToSquared`) qui passe le test ; retiré des libres une fois apparié. Rescue : manipulation, `CanRescueNow(r, p, forced: true)`, `CanReserve(p)` (`forced` ignore les réservations), lit trouvé. Tend : refaire `ShouldBeTendedNowByPlayer` ; `!WorkTypeIsDisabled(Doctor)`, manipulation, `CanReserveAndReach(p, ClosestTouch, Deadly)` ; soi-même seulement si `playerSettings.selfTend`.
- [ ] **Step 3 : lits.** `Dictionary<Building_Bed, int>` des places données pendant le clic ; un lit dont le compte atteint `SleepingSlotsCount` est refusé. Recherche : `FindBedFor(patient, porteur, false, false, statut)` puis `ignoreOtherReservations: true`, `statut = IsSlaveOfColony ? GuestStatus.Slave : null`.
- [ ] **Step 4 : jobs.** Rescue : `End` assaut, `Rescue(patient, lit)`, `count = 1`. Tend : `End` assaut, job comme `WorkGiver_Tend.JobOnThing` (cible C = `SpawnedParentOrMe` si différent), `count = 1`, `draftedTend = !patient.InBed()`. Bed : `Drafted = false`, puis `LayDown(lit)`, `restUntilHealed = true`. Chaque pion dans `SquadOrders.Try`. Messages : `RCB_Medical_RescueDone` / `TendDone` / `BedDone` ({0} = nombre, `SilentInput`) si > 0 ; sans lit : `"CannotRescue".Translate() + ": " + NoNonPrisonerBed/NoSlaveBed` (`RejectInput`, une fois par clic). `SquadStatus.Invalidate()` à la fin.
- [ ] **Step 5 : survol.** Copier le schéma de `SquadStations.Hover/DrawHover` : contour `DrawFieldEdges` des cases `Position` des pions passés, couleur de l'escouade, sans ligne. Appeler `SquadMedical.DrawHover()` dans `BarSquads.GameComponentUpdate` dans le même `try`.
- [ ] **Step 6 : textes** des messages (EN + FR).
- [ ] **Step 7 : build** `./deploy.sh` : 0 erreur, 6 avertissements.

### Task 2 : section « Soins » de la carte d'ordres

**Files:**
- Modify: `Source/RadiusColonistBar/SquadOrderCard.cs` (`ContentHeight`, `DoWindowContents` entre l'assaut et `DrawMembers`)

**Interfaces:**
- Consumes : tâche 1 ; `SquadHotkeys.Key.Rescue/Tend/Bed` (tâche 3) ; `Tip`, `Button`.
- Produces : `private void DrawMedical(float x, ref float y, float w, List<Pawn> able)`.

- [ ] **Step 1 :** `ContentHeight` + `10f + 18f + BtnH` (mettre à jour le commentaire).
- [ ] **Step 2 :** `DrawMedical` : titre `RCB_Medical_Title` (MicroLabel comme l'assaut), trois boutons au tiers : icônes `IconSet.Alert.Crit`, `IconSet.Status.Medical`, `IconSet.Status.Zzz` ; libellés `RCB_Medical_Rescue/Tend/Bed` ({0} = n). Une seule `Able` et un seul `Targets` par bouton et par image ; survol du bouton : `SquadMedical.Hover(index, liste)`.
- [ ] **Step 3 : infobulles.** `RCB_Medical_*Tip`, puis jusqu'à 6 lignes « `LabelShortCap` · `Condition(p)` », puis `RCB_Medical_More` ({0}) au-delà, puis `SquadHotkeys.WithKey`. Raisons grisées : `RCB_Orders_NoMember` (aucun apte), `RCB_Medical_NoDowned`, `RCB_Medical_NoWounded`, `RCB_Medical_NoDoctor`, `RCB_Medical_NoRest`.
- [ ] **Step 4 : textes** EN + FR des clés de la section.
- [ ] **Step 5 : build**, puis contrôles en jeu 1-8, 10-12 de la spec.

### Task 3 : raccourcis

**Files:**
- Modify: `Defs/KeyBindings_Squads.xml`, `Languages/French/DefInjected/KeyBindingDef/Squads.xml`, `Source/RadiusColonistBar/SquadHotkeys.cs`

- [ ] **Step 1 :** trois `KeyBindingDef` (`rescue downed with squad`, `tend wounded with squad`, `send squad wounded to bed`), catégorie `RCB_Squads`, sans touche ; libellés FR en `DefInjected`.
- [ ] **Step 2 :** champs, `Bind`, branches `Pressed` → `Run(SquadMedical.Rescue/Tend/ToBed)`, valeurs `Key.Rescue/Tend/Bed` dans `WithKey`.
- [ ] **Step 3 : build**, contrôle en jeu 9.

### Task 4 : documentation

- [ ] `README.md` section « Soins » après « Postes de combat » ; `CHANGELOG.md` section « 1.2.0 — non publiée » ; `docs/ROADMAP.md` lot 3 en cours puis fait ; `python3 ../RadiusUIFrench/tools/check_translations.py` OK.
