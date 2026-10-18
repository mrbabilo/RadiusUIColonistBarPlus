# Onglet Travail de la carte d'ordres — plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Troisième onglet « Travail » : pauses (manger, dormir, détente, reprendre), tâches par type de travail (mode Maintenant ou Priorités), copie des priorités et de l'emploi du temps du chef.

**Architecture:** `SquadWork` (statique) porte les ordres et la recherche de tâche (tests de `JobGiver_Work`, `forced: false`, job par `TryTakeOrderedJobPrioritizedWork`). La carte lit les comptes dans son instantané, seulement onglet Travail ouvert, et dessine une grille avec défilement dans la zone d'onglet de hauteur fixe.

**Tech Stack:** C# `net472`, RimWorld 1.6 (`WorkGiver_Scanner`, `GenClosest`, `JobGiver_GetFood`, `JobGiver_GetJoy`, `Pawn_WorkSettings`, `Pawn_TimetableTracker`), Radius UI Framework (`FlatScroll`, `IconSet`).

**Spec:** `docs/superpowers/specs/2026-10-18-travail-escouade-design.md`

**Note d'exécution (2026-10-18) :** chef pris dans l'instantané de la carte (`able[0]`, sinon premier membre vivant) ; recherche protégée par donneur (`try/catch`, `Log.ErrorOnce`) ; pauses démobilisent seulement après avoir trouvé un job ; grille limitée aux lignes visibles et au survol de la grille. Le code fait foi.

## Global Constraints

- Rien de sauvegardé, aucun composant neuf, aucun état réappliqué en continu.
- Aucune ligne de Colony Groups reprise.
- Style ILSpy, commentaires en anglais seulement sur le code ajouté.
- Clés `RCB_Tab_Work`, `RCB_Work_*` en anglais et en français, sans doublon.
- Preuve : `./deploy.sh` (0 erreur, 6 avertissements), grep des doublons, `check_translations.py`, contrôles en jeu de la spec § 9. Pas de commit sans demande.

## Review Focus

1. Membre à priorité 0 : jamais envoyé en mode Maintenant, compté « non affectés ».
2. Recherche en `forced: false` : la zone autorisée est respectée.
3. Copie des priorités : un type que le chef ne peut pas faire n'est pas copié.
4. Molette sur la grille : pas de zoom de la caméra.
5. `FlatScroll` toujours refermé (`EndOrUnwind` dans un `finally`).

---

### Task 1 : `SquadWork`

**Files:** Create `Source/RadiusColonistBar/SquadWork.cs`

- Produces :
  - `internal enum SquadWork.Pause { Eat, Sleep, Joy, Resume }`
  - `internal static List<Pawn> PauseTargets(List<Pawn> able, Pause pause)` ; `internal static void DoPause(int index, Pause pause)`
  - `internal static List<WorkTypeDef> Types(bool now)` : types visibles, sans `Patient` / `PatientBedRest` en mode Maintenant.
  - `internal static bool Capable(Pawn, WorkTypeDef)`, `internal static bool Assigned(Pawn, WorkTypeDef)`
  - `internal static void WorkNow(int index, WorkTypeDef work)`
  - `internal static int SharedPriority(List<Pawn> members, WorkTypeDef work, out bool mixed, out int capable)` (−1 si aucun capable)
  - `internal static void ShiftPriority(int index, WorkTypeDef work, int dir)` (dir −1 : clic gauche, +1 : clic droit)
  - `internal static void SetManual(bool on)`
  - `internal static Pawn Leader(int index)`, `internal static void CopyPriorities(int index)`, `internal static void CopyTimetable(int index)`
- [ ] Écrire la classe (spec § 2 à § 4).

### Task 2 : onglet Travail

**Files:** Modify `Source/RadiusColonistBar/SquadOrderCard.cs`

- [ ] Troisième onglet ; `private static bool workNow = true` (mode retenu pour la session).
- [ ] Instantané (onglet 2) : listes de pause, types du mode, par type : membres en train de faire ce travail, affectés, capables, priorité commune et mixte.
- [ ] `DrawWork` : PAUSE (quatre boutons), ligne TÂCHES avec bascules, grille trois colonnes avec `FlatScroll` et molette consommée, clic droit lu à part, ligne du bas.

### Task 3 : textes et documentation

- [ ] Clés EN + FR ; README section « Travail » ; CHANGELOG « 1.4.0 — non publiée » ; ROADMAP (4b code fait).
