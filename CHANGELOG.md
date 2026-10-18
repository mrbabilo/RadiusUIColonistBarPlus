# Journal des modifications

## 1.4.0 — 2026-10-18

- **Onglet Travail** dans la carte d'ordres : pauses (manger, dormir, détente, reprendre), tâches d'un type de travail données à toute l'escouade, priorités de travail de l'escouade, copie des priorités et des horaires du chef.
- Mode Maintenant : chaque membre affecté part sur une tâche différente, selon la recherche de travail du jeu ; un membre à priorité 0 n'est pas envoyé. Badge et message comptent le résultat.
- Mode Priorités : les vraies priorités des membres, communes ou mixtes, clic gauche et droit comme dans l'onglet Travail ; bascule des priorités manuelles.
- Rien n'est sauvegardé, aucun travail n'est réappliqué en continu.
- Onglet Règles : la ligne Soins et le bouton « Modifier… » tiennent dans leur case en français.

## 1.3.0 — 2026-10-18

- **Onglets** dans la carte d'ordres : Combat et Règles. La section Membres reste sous les deux, la carte garde sa hauteur, l'onglet ouvert est retenu pour la session.
- **Règles d'escouade** : tenue, nourriture, drogues, soins médicaux, zone et réaction aux menaces réglées en une fois pour toute l'escouade, avec la valeur commune ou « Mixte » et la valeur de chacun en infobulle. Zone : membres de la carte affichée seulement. « Modifier… » ouvre la gestion des règles du jeu ; « Libérer » retire les vêtements forcés.
- Un pion incapable de violence ne reçoit jamais « Attaquer », comme dans l'onglet Affectation.
- Menu « Règles » du portrait : textes traduits (ils étaient écrits en dur en anglais).

## 1.2.0 — 2026-10-18

- **Soins** dans la carte d'ordres : Secourir porte les pions à terre au lit, Soigner envoie les membres qui savent soigner vers les blessés, Au lit met les membres blessés au repos médical jusqu'à guérison. Patients : colons et esclaves de la carte, hémorragies mortelles d'abord, puis membres de l'escouade ; un patient par membre, le plus proche.
- Les boutons comptent les pions concernés, les nomment dans l'infobulle et les entourent sur la carte au survol.
- Trois raccourcis sans touche par défaut : secourir, soigner, envoyer au lit.
- La ligne d'état de la carte compte les membres à terre et à soigner ; Secourir et Soigner sont soulignés tant que des membres secourent ou soignent.
- L'infobulle d'un onglet d'escouade commence par cette ligne d'état : plus besoin d'ouvrir la carte pour voir où en est l'escouade.
- Quand tous les membres présents sont à terre ou en crise, la carte le dit au lieu d'annoncer « aucun membre sur cette carte ».
- Une carte d'ordres déplacée se rouvre à la même place (pour la session ; carte de la barre et carte du Centre de commandement séparément).
- Carte d'ordres plus légère : l'état et les listes de membres sont relus toutes les 6 images, ou aussitôt après un ordre, au lieu de chaque image.

## 1.1.0 — 2026-10-17

- **Postes de combat** dans la carte d'ordres : quatre postes par escouade. Enregistrer prend la position actuelle des membres présents ; Aller au poste mobilise l'escouade, arrête son assaut et envoie chacun en sprint à sa case ; Effacer demande confirmation. Le survol montre les cases sur la carte.
- Raccourci « aller au poste de combat actif », sans touche par défaut.
- Les postes appartiennent au pion et à la carte où ils ont été enregistrés ; ils sont sauvegardés avec les escouades.
- **Affectation rapide** : la carte d'ordres a une section « Membres » avec « Ajouter la sélection » et « Retirer la sélection » (colons et esclaves sélectionnés ; un pion d'une autre escouade change d'escouade, l'infobulle le compte). « Sélectionner » sélectionne les membres présents, y compris ceux à terre. Dans le module Escouades, « Nouvelle escouade » prend la sélection quand il y en a une.
- Carte d'ordres : Mobiliser et Tir à volonté sont surlignés quand toute l'escouade est dans cet état, soulignés quand seule une partie l'est. Un bouton grisé dit pourquoi dans son infobulle. Les infobulles indiquent la touche des raccourcis qui en ont une.
- Dissoudre une escouade demande confirmation.
- Infobulle de l'onglet d'escouade : le clic droit ouvre les ordres (et non plus « afficher tout le monde »).
- Fiche de survol : délai avant ouverture (0,25 s par défaut, réglable dans l'onglet Panneau de survol), sans délai pour passer d'un portrait au voisin ; ligne d'aide sur les clics droits du portrait.

## 1.0.0 — 2026-10-15

- Fork de Radius UI - Colonist Bar (astryl). Corrections de décompilation pour compiler les sources, dont les cibles de trois correctifs Harmony perdues par ILSpy (taille de la barre, espacement des pions, largeur maximale) : sans elles, la barre gardait les dimensions du jeu et les pions débordaient de leur fond.
- **Ordres d'escouade** par clic droit sur l'onglet d'une escouade ou depuis le module Escouades : mobiliser ou démobiliser, tir à volonté, regroupement sur le chef, assaut (les plus forts d'abord, les plus faibles d'abord, poursuite des fuyards).
- Carte d'ordres aux couleurs de Radius UI à la place du menu déroulant : état de l'escouade, tous les ordres sans sous-menu, mode d'assaut en cours surligné (un clic dessus arrête l'assaut). La carte se déplace à la souris et se ferme par son carré en haut à droite.
- Module Escouades : la même carte, avec Renommer, Monter, Descendre et Dissoudre.
- Onglet d'escouade : point quand des membres sont mobilisés, épée pendant un assaut.
- Épée sur le portrait des pions qui suivent un assaut.
- Six raccourcis clavier sans touche par défaut (catégorie « Radius UI - Escouades ») pour l'escouade des pions sélectionnés.
- L'assaut garde le comportement vanilla de couverture et de corps à corps, attaque n'importe quel ennemi à portée quand aucun ne correspond au mode, et se réveille seul quand de nouveaux ennemis arrivent.
- Un ordre donné à un pion, sa démobilisation ou « Cesser le feu » met fin à son assaut. L'ordre est exécuté du premier coup, y compris un ordre mis en file avec Maj. Couper le tir à volonté d'un seul pion, par son propre bouton, l'arrête aussi.
- « Cesser le feu » et le regroupement interrompent l'attaque déjà en cours au lieu de la laisser finir. Un ordre du joueur ou un job qui ne peut pas être interrompu n'est jamais coupé.
- Un pion en repos mortel (sanguophage) n'est pas pris dans les ordres d'escouade : mobiliser l'escouade ne le réveille pas.
- Multijoueur non pris en charge.
- Infobulle des sous-menus traduite. Boutons de la carte de survol traduits (Mobiliser, Centrer, Santé, Bio, Social, Matériel, Renommer, Règles, Défendre), avec les traductions du jeu quand elles existent.
- Carte de survol : un libellé trop long pour son bouton passe en petite police au lieu de passer à la ligne (« Démobiliser » était coupé en « Démobili… » sur deux lignes).
