# Colonist Bar Plus — onglet Travail de la carte d'ordres

Date : 2026-10-18. Lot 4 de `docs/ROADMAP.md`, seconde partie (4b). Suite de `2026-10-18-regles-escouade-design.md` (4a : onglets Combat et Règles).

Le joueur a demandé une spec qui suit l'UX de [LTO] Colony Groups et l'améliore. Les choix non dictés par Colony Groups ou par le code existant sont marqués « choix de conception ».

## 0. Référence : le menu Travail de Colony Groups

`WorkMenu` et `WorkSearchUtility` de Colony Groups (comportement de référence ; pas de licence, aucune ligne reprise) :

- une grille des types de travail visibles, en deux colonnes ; un clic gauche lance une recherche de travail de ce type pour tout le groupe et fait passer la case par trois états sauvegardés (vide, « travail actif » bleu, « travail forcé » rouge) que le groupe réapplique ensuite ;
- à côté de chaque type, une case de priorité de groupe, avec un bouton de remise à zéro ;
- quatre icônes de pause : discuter (`JoyGiver_SocialRelax`), se détendre (`JobGiver_GetJoy`), aller manger, aller dormir ;
- la case globale « Priorités manuelles », un bouton vers l'écran de recherche, un bouton vers le menu caravane.

Ce qui gêne dans cette UX, et que ce lot améliore :

- les états actif/forcé sont **sauvegardés** dans le groupe et réappliqués en continu : contraire à la règle du fork « rien dans la sauvegarde, pas de comportement continu » ;
- l'état d'une case ne dit pas qui travaille vraiment, ni qui ne peut pas faire ce travail ;
- la priorité de groupe s'ajoute à celle des pions sans montrer la valeur réelle de chacun ;
- aucune information sur le résultat d'un clic (combien sont partis, combien n'ont rien trouvé) ;
- les icônes de pause n'ont qu'une infobulle, sans compte.

## Objectif

Donner à une escouade un travail ou une pause en un clic, et régler ses priorités de travail sans ouvrir l'onglet Travail vanilla, en voyant qui peut faire quoi.

Critères de réussite :

- un clic sur un type de travail envoie chaque membre capable sur une tâche de ce type, chacun sur une tâche différente, et dit combien sont partis ;
- les priorités de chaque type montrent la valeur commune de l'escouade ou « Mixte », et un clic les change pour tous ;
- manger, dormir, se détendre et reprendre le travail se donnent en un clic ;
- rien n'est ajouté à la sauvegarde.

## 1. Place dans la carte

Troisième onglet « TRAVAIL », après Combat et Règles. Même zone de hauteur fixe (`TabAreaH`) : la carte ne grandit pas. Membres et ligne de gestion restent dessous.

Ordre vertical de l'onglet, dans les 266 px de `TabAreaH` :

1. **PAUSE** : titre micro 18, ligne de quatre boutons `BtnH` 30, écart 10 (58 px).
2. **TÂCHES** : ligne de titre 22, avec à droite une bascule à deux segments « Maintenant | Priorités » et, en mode Priorités, la bascule « Manuelles : oui/non » à sa gauche, sur la même ligne (pas de ligne de plus) ; écart 4.
3. Grille des types de travail, avec défilement (`FlatScroll`, `EndOrUnwind` dans un `finally`) : environ 142 px, soit 4 à 5 lignes visibles de pastilles 26 + 4. La carte ne bloque pas la caméra (`preventCameraMotion = false`) : la molette sur la grille est consommée (`Event.current.Use()` sur `ScrollWheel` quand la souris est dans la grille), sinon elle ferait aussi zoomer la carte du jeu.
4. Écart 10, ligne du bas 30 : « Copier priorités », « Copier horaires » (libellés courts pour tenir dans une demi-largeur ; l'infobulle nomme le chef).

## 2. Membres concernés

- Comme les autres ordres : `SquadMembers.Able(Find.CurrentMap, index)`, membres présents et en état d'agir. Le travail et les pauses se font sur la carte.
- Exception (choix de conception) : priorités et emploi du temps touchent **tous les membres vivants** (`SquadRules.Members`), comme les règles de 4a : ce sont des réglages du pion, pas des ordres sur la carte.
- **Chef** : premier membre apte dans l'ordre de la barre (celui du Regroupement). Sans membre apte, le premier membre vivant de `SquadRules.Members`. L'infobulle des boutons de copie le nomme.
- **Pion capable d'un type** : `!WorkTypeIsDisabled(type)`. **Pion affecté à un type** : capable et priorité non nulle (`workSettings.GetPriority(type) > 0`).

## 3. Pause

Quatre boutons, d'un quart de largeur, icônes du framework.

| Bouton | Concernés | Ordre |
|---|---|---|
| Manger | membres aptes dont la nourriture est sous 90 % (`needs.food`) | démobilisés ; `new JobGiver_GetFood()` (classe vanilla : sans réglage, elle n'a pas de seuil de faim) puis `TryIssueJobPackage` : distributeurs, inventaire des animaux de bât, nombre de portions et règle de nourriture comme le jeu |
| Dormir | membres aptes qui peuvent s'endormir (`RestUtility.CanFallAsleep`) : un pion reposé resterait couché éveillé | démobilisés ; `RestUtility.FindBedFor(pion)` puis `LayDown` ; sans lit, pion sauté et compté (pas de sommeil par terre) |
| Détente | membres aptes dont la détente est sous 99 % (seuil de `JobGiver_GetJoy`, qui refuse au-delà) | démobilisés ; `JobGiver_GetJoy` (classe vanilla, `ResolveReferences`, `TryIssueJobPackage`) ; un résultat `GotoWander` est refusé |
| Reprendre | membres aptes | démobilisés ; un membre dont le job en cours est une flânerie ou une attente (`CurJobDef` parmi `GotoWander`, `Wait_Wander`, `Wait` ; pas `Wait_MaintainPosture`, que le jeu donne à un patient qui attend son soigneur, y compris par l'ordre Soigner) ou une détente (`CurJobDef.joyKind != null`) arrête ce job (`EndCurrentJob(InterruptForced)`, si interruptible) pour que l'arbre de pensée lui donne du travail. Repas et sommeil ne sont jamais coupés. |

- Comptes sur les boutons (« Manger (n) »), bon marché (besoins seulement), relus dans l'instantané de la carte, et seulement quand l'onglet Travail est ouvert (comme les règles en 4a).
- Manger, Dormir, Détente cherchent d'abord la nourriture, le lit ou la détente, et ne démobilisent que le membre qui en a trouvé : celui qui n'a rien trouvé reste mobilisé. Reprendre démobilise toujours (c'est son but).
- Démobiliser pendant un raid est un vrai risque : l'infobulle de chaque pause et du mode Maintenant le dit (« Démobilise les membres »).
- Infobulle : ce que fait l'ordre, puis les noms concernés (6 au plus), puis la touche s'il y en a une.
- Message après clic : « Manger : 3 en route, 1 sans nourriture accessible » (`SilentInput`, et `RejectInput` si personne n'est parti).
- Démobiliser met fin à l'assaut (patch existant).

## 4. Tâches

### Grille

- Types : `WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder` où `visible`, dans cet ordre (celui de l'onglet Travail vanilla).
- Mode Maintenant : sans « Patient » ni « Repos au lit » (`Patient`, `PatientBedRest`) : ce ne sont pas des tâches à donner, l'onglet Combat a déjà « Au lit » et « Soigner ». Ils restent en mode Priorités, où leur priorité compte.
- Trois colonnes de pastilles, hauteur 26, écart `Gap` (choix de conception : trois colonnes de libellés courts tiennent dans 356 px, deux en demanderaient le double de lignes).
- Pastille : `labelShort` du type (ajusté avec `RcbFont.Fit`), à droite un badge (mode Maintenant : nombre de membres en train de faire ce travail ; mode Priorités : la priorité).
- Pastille grisée quand aucun membre concerné ne peut faire ce travail (`WorkTypeIsDisabled`).
- Infobulle : `description` du type, puis « Affectés : n · Non affectés : noms · Incapables : noms », puis, en mode Priorités, la valeur de chaque membre si elle est mixte.

### Mode Maintenant (amélioration du clic de Colony Groups)

- Clic gauche : chaque membre apte **affecté** à ce travail, dont le job en cours n'est pas déjà de ce type (`CurJob.workGiverDef.workType`), est démobilisé puis reçoit une tâche de ce type. Un membre capable mais non affecté (priorité 0) est sauté et compté « non affectés », comme le menu « Prioriser » vanilla, qui le refuse (`CannotPrioritizeNotAssignedToWorkType`) : on l'affecte en mode Priorités.
  - Recherche par donneur de travail du type (`workGiversByPriority`), en sautant ceux que le pion ne peut pas utiliser, avec **tous** les tests de `JobGiver_Work.PawnCanUseWorkGiver` : `nonColonistsCanDo`, `WorkTagIsDisabled(workTags)`, `WorkTypeIsDisabled`, `ShouldSkip`, `MissingRequiredCapacity(pion) == null`, `canBeDoneByMechs`.
  - `NonScanJob` d'abord, puis, pour un `WorkGiver_Scanner`, une chose (`scanThings`) ou une case (`scanCells`) pour laquelle `HasJobOnThing` / `HasJobOnCell` est vrai avec **`forced: false`**, non interdite et atteignable (`TraverseParms.For(pion, MaxPathDanger)`). `forced: false` comme le travail normal du pion : un balayage de toute la carte en `forced: true` passerait outre les contrôles que le jeu réserve à une cible cliquée par le joueur (zone autorisée, limites). Pour un donneur `Prioritized`, la cible de meilleure `GetPriority`, puis la plus proche ; sinon la plus proche.
  - Les donneurs sont pris par `priorityInType` décroissant ; dans un même niveau, la cible la plus proche gagne, comme `JobGiver_Work`. Job donné par `TryTakeOrderedJobPrioritizedWork` (comme « Prioriser ») : le pion enchaîne ensuite les tâches voisines du même donneur.
- Pas d'état sauvegardé (amélioration) : un clic = une affectation. Le badge montre ensuite qui fait vraiment ce travail, relu dans l'instantané.
- Les membres se répartissent seuls : la réservation prise par le premier job empêche le suivant de viser la même cible.
- Message : « {travail} : {n} au travail, {m} sans tâche trouvée, {a} non affectés, {k} incapables » (parties à zéro omises).
- Coût : la recherche ne tourne qu'au clic.

### Mode Priorités (amélioration de la priorité de groupe)

- La priorité affichée est la **vraie** valeur des pions (`workSettings.GetPriority`), commune ou « Mixte » ; aucune priorité de groupe à part.
- Priorités manuelles actives (`Find.PlaySettings.useWorkPriorities`) : badge 1 à 4 dans la couleur vanilla (`WidgetsWork.ColorOfPriority`), vide pour 0. Clic gauche : valeur − 1 (0 devient 4), clic droit : + 1 (4 devient 0), comme la case vanilla. Sur une valeur mixte, le calcul part de la priorité du premier membre capable de ce type.
- Clic droit : le bouton de la carte et `Widgets.ButtonInvisible` ne prennent que le clic gauche. Le clic droit est lu à part (`Event.current.type == EventType.MouseDown`, `button == 1`, souris sur la pastille), puis `Event.current.Use()`, pour qu'il n'atteigne pas la carte du jeu derrière.
- Priorités manuelles inactives : badge coche ; clic = activé (3) / désactivé (0) pour tous ; mixte devient activé.
- Les membres incapables sont sautés. Sons vanilla : `DragSlider` en manuel, `Checkbox_TurnedOn/Off` sinon.
- Comme la case vanilla : quand un type passe de 0 à une priorité chez un membre dont l'idéoligion juge ce travail dangereux (`Ideo.IsWorkTypeConsideredDangerous`), message `MessageIdeoOpposedWorkTypeSelected` (`CautionInput`), une fois par clic.
- Bascule « Manuelles : oui/non » sur la ligne de titre TÂCHES, dans ce mode seulement : réglage de **toute la colonie**, dit dans l'infobulle. Changer la bascule appelle `Notify_UseWorkPrioritiesChanged` sur tous les pions du joueur, comme l'onglet Travail vanilla.

### Bas de l'onglet

- « Copier les priorités du chef » : pour chaque autre membre vivant, chaque type que **le chef et ce membre** peuvent faire reçoit la priorité du chef. Un type que le chef ne peut pas faire n'est pas copié : sa priorité 0 éteindrait ce travail chez les autres.
- « Copier l'emploi du temps du chef » : les 24 heures de `timetable` du chef recopiées sur chaque autre membre vivant qui a un emploi du temps.
- Grisés sans autre membre ; message avec le nombre de membres changés.

## 5. Raccourcis

Aucun raccourci neuf (choix de conception : ces ordres se donnent rarement d'urgence ; les dix raccourcis actuels couvrent le combat).

## 6. Code

| Fichier | Rôle |
|---|---|
| `SquadWork.cs` (nouveau) | Pauses, recherche de tâche par type, priorités, copies, chef. |
| `SquadOrderCard.cs` | Troisième onglet, grille avec défilement, bascule de mode, instantané étendu (comptes de pause, badge par type, priorités communes). |

Le mode (Maintenant / Priorités) est retenu pour la session, comme l'onglet.

## 7. Textes

- Clés `RCB_Tab_Work`, `RCB_Work_*` (titres, quatre pauses et leurs infobulles et messages, deux modes, bascule des priorités manuelles, comptes de l'infobulle, deux copies, messages).
- Repris de vanilla : `labelShort`, `description` et `gerundLabel` des `WorkTypeDef`.
- Documentation : README section « Travail », CHANGELOG « 1.4.0 — non publiée », ROADMAP (4b), `About.xml` et `docs/WORKSHOP.md` à la publication.

## 8. Hors périmètre

- Travail « actif » ou « forcé » réappliqué en continu (Colony Groups) : il faudrait sauvegarder un état et tourner en tâche de fond.
- Boutons vers l'écran de recherche et le menu caravane : déjà dans le jeu et dans la barre.
- Édition heure par heure de l'emploi du temps.
- Discuter (`SocialRelax`) comme pause à part : `JobGiver_GetJoy` le propose déjà parmi les détentes.

## 9. Vérification

Build sans nouvel avertissement, clés sans doublon, `check_translations.py`. Contrôles en jeu :

1. Onglet Travail : la carte garde sa hauteur ; la grille défile.
2. Maintenant → Couper (plantes) avec trois membres et des arbres désignés : trois membres sur trois arbres différents ; badge 3 ; message avec le bon compte.
3. Un membre incapable de ce travail, et un membre capable mais à priorité 0 : l'infobulle les nomme, ils sont sautés et comptés à part.
3 bis. Un membre limité à une zone : la tâche trouvée est dans sa zone.
4. Rien à faire de ce type : message « sans tâche trouvée », aucun membre ne bouge.
5. Membres mobilisés : démobilisés avant d'aller travailler ; assaut terminé.
6. Priorités manuelles actives : clic gauche puis droit sur une pastille, valeurs de tous les membres changées comme dans l'onglet Travail vanilla ; « Mixte » quand un membre diffère.
7. Priorités manuelles inactives : la pastille coche ou décoche pour tous.
8. Bascule des priorités manuelles : l'onglet Travail vanilla suit.
9. Manger, Dormir, Détente : membres concernés partis ; un membre rassasié n'est pas compté dans Manger, un membre reposé pas dans Dormir.
9 bis. Soigner (onglet Combat), puis Reprendre : le patient qui attend son soigneur n'est pas dérangé.
9 ter. Molette sur la grille : la grille défile, la caméra ne zoome pas.
9 quater. Un membre mobilisé, aucune nourriture accessible, Manger : il reste mobilisé, le message le compte « sans nourriture ».
10. Reprendre : un membre en détente revient au travail ; un membre qui mange continue.
11. Copier priorités et emploi du temps du chef : identiques chez tous ensuite, membre en caravane compris ; un type que le chef ne peut pas faire garde la priorité de chacun.
12. Clic droit sur une pastille en mode Priorités : la priorité change, rien ne se passe sur la carte derrière.
