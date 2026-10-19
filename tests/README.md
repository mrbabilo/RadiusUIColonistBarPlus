# Tests en jeu

Deux mods d'aide, jamais publiés : `deploy.sh` exclut `tests/`.

| Dossier | packageId | Rôle |
|---|---|---|
| `ColonistBarPlusTests/` | `mrbabilo.RadiusUI.ColonistBarPlus.Tests` | Contrôles automatiques, pilotage de l'interface, sondes de performance |
| `Scene/` | `mrbabilo.RadiusUI.ColonistBarPlus.Scene` | Scènes de mesure : colons, pillards, vitesse. Aucun patch Harmony, aucune référence au mod mesuré : la scène tourne aussi avec la barre vanilla. |

Les mods ne font rien sans leur argument de lancement :

| Argument | Effet |
|---|---|
| `-rcbtest=<fichier>` | Contrôles automatiques. Le mod écrit une ligne PASS ou FAIL par contrôle, puis quitte le jeu. |
| `-rcbui=<dossier>` | Pilotage de l'interface par fichier de commandes (carte d'ordres, onglets, clics injectés, captures). |
| `-rcbscene=<dossier>` | Même serveur de commandes, sans aucun patch ni scène propre. La scène vient de `Scene/`. |
| `-rcbperf=<fichier>` | Sondes Stopwatch, l'ancienne méthode. Les mesures se font maintenant avec DPA. |

Pendant les contrôles, les scènes et les mesures, le journal de debug et la notice du framework Radius ne s'ouvrent pas (`QuietStartup.cs`) ; les erreurs restent comptées par les contrôles.

Les scripts lancent une partie séparée, avec son propre dossier de sauvegarde (`-savedatafolder`), dans `$RCBTEST_WORK` (par défaut `$TMPDIR/rcbtest`). Ils touchent quand même aux préférences Unity du joueur (taille de fenêtre, plein écran). Pour cela :
- ils les sauvegardent une fois, dans le dossier de travail ;
- ils les remettent après chaque lancement.

Les mods d'aide ne sont copiés dans `Mods` (`RCBPlus-*`) que le temps du lancement.

## Contrôles

Publier d'abord Colonist Bar Plus (`./deploy.sh`) : les contrôles portent sur le mod installé.

```sh
tests/run-checks.sh
```

Le script n'affiche que les lignes qui ne sont pas PASS, et rend le code 1 si un contrôle a échoué. Les résultats complets sont dans `$RCBTEST_WORK/results.txt`, et le journal du jeu dans `checks.log`.

## Pilotage de l'interface

```sh
tests/run-ui.sh               # bloque jusqu'à la fermeture du jeu ; lancer dans un autre terminal
tests/cmd.sh opencard 150 120
tests/cmd.sh settab 2
tests/cmd.sh shot carte.png
tests/cmd.sh quit
```

Les verbes sont listés en tête de `cmd.sh`, et l'état du jeu est écrit dans `state.json`. Le clic droit et la molette injectés n'atteignent pas toutes les fenêtres du jeu : ces deux gestes se vérifient à la vraie souris.

## Mesures DPA

DPA (Workshop 2038874626) doit être installé. Ses réglages pour le bac à sable viennent de `tests/dpa/`. Les réglages DPA du joueur ne sont pas modifiés.

```sh
tests/run-scene.sh            # plein écran ; bloque jusqu'à la fermeture du jeu
tests/scene.sh colonists 20
tests/cmd.sh squadall
tests/cmd.sh opencard 150 120
tests/scene.sh raid 6
tests/scene.sh quit
```

Le panneau DPA s'ouvre par la loupe de la barre du bas, catégories Custom Update et Custom Tick.

Pour une méthode récursive, ne la mesurer que depuis le site d'appel de son appelant, jamais avec un second profileur DPA sur elle.

Rapports de mesure : `docs/perf/`.
