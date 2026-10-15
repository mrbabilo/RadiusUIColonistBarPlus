# Journal des modifications

## 1.0.0 — non publiée

- Fork de Radius UI - Colonist Bar (astryl). Corrections de décompilation pour compiler les sources, dont les cibles de trois correctifs Harmony perdues par ILSpy (taille de la barre, espacement des pions, largeur maximale) : sans elles, la barre gardait les dimensions du jeu et les pions débordaient de leur fond.
- **Ordres d'escouade** par clic droit sur l'onglet d'une escouade ou depuis le module Escouades : mobiliser ou démobiliser, tir à volonté, regroupement sur le chef, assaut (les plus forts d'abord, les plus faibles d'abord, poursuite des fuyards).
- Carte d'ordres aux couleurs de Radius UI à la place du menu déroulant : état de l'escouade, tous les ordres sans sous-menu, mode d'assaut en cours surligné (un clic dessus arrête l'assaut).
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
