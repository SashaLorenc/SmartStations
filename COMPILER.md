# Compiler et tester Smart Stations sur ton PC

Même principe que Smart Chest : mêmes outils (SDK .NET, r2modman, Jötunn), même `Environment.props`.

## 0. Récupérer le code (une seule fois)

Dans PowerShell, dans le dossier où tu ranges tes projets (par ex. là où il y a déjà `SmartChest`) :

```
git clone https://github.com/SashaLorenc/SmartStations.git
cd SmartStations
```

Puis copie ton `Environment.props` de Smart Chest à la racine de `SmartStations` (il est identique : mêmes chemins Valheim, BepInEx et Jötunn).

## À chaque modification

```
git pull
dotnet build SmartStations/SmartStations.csproj -c Release
```

La DLL est copiée automatiquement dans `BepInEx/plugins/SmartStations/` de ton profil r2modman.

## Tester la version 0.1.0

Au lancement, `BepInEx/LogOutput.log` doit afficher `Smart Stations 0.1.0 chargé.`

1. **Fonderie** : approche-toi, le survol affiche « [E] Ouvrir » et « [Maj + E] » + l'action d'origine. Appuie sur **E** : ton inventaire s'ouvre avec la fenêtre de la station au-dessus.
2. **Ajouter** : clique sur du minerai de cuivre dans ton inventaire, puis clique sur la zone **Entrée** : il y entre (jusqu'à 10). Essaie aussi le **clic droit** sur du charbon dans l'inventaire : il va dans **Combustible**. Boutons **Tout ajouter** et **Remplir**.
3. **Reprendre** : clic sur la case du minerai = tout revient dans l'inventaire ; clic droit = un seul.
4. **Produits** : laisse fondre, les lingots apparaissent dans **Produits** au lieu de tomber par terre. Clic dessus ou **Tout prendre**.
5. **Remplissage auto** : mets du minerai et du charbon dans un coffre à moins de 10 m, clique **Remplissage auto : NON** pour passer à OUI, ferme la fenêtre : en 5 s la fonderie se remplit depuis le coffre.
6. **Four à charbon, moulin à vent, rouet, raffinerie d'eitr** : même fenêtre, sans case Combustible quand la station n'en utilise pas.
7. **Fermentateur** : une seule entrée ; pendant la fermentation la ligne d'état affiche le pourcentage et le temps restant, puis les hydromels apparaissent dans **Produits**.
8. **Détruire** une fonderie qui contient des produits : tout tombe par terre, rien n'est perdu.

## Réglages

Dans `BepInEx/config/sashalorenc.SmartStations.cfg` (créé au premier lancement) :

- `[Stations]` : `Fours`, `Fermentateur` (activer la fenêtre), `DistanceMax`.
- `[Produits]` : `GarderDansLaStation` (true/false), `Capacite` (100 par défaut ; au-delà ça tombe par terre).
- `[RemplissageAuto]` : `Autorise` (interrupteur général), `ActifParDefaut`, `Rayon` (10 m), `Intervalle` (5 s).

## Bon à savoir

- Le remplissage auto ne puise que dans les coffres posés par un joueur, auxquels tu as accès, que personne n'a ouverts, et dont ta partie est « propriétaire » réseau (en solo : tous). C'est ce qui empêche toute duplication sur serveur.
- Si tu désinstalles le mod, les produits encore gardés dans une station sont perdus : vide-les avant.
- Pas encore géré : manette, stations de cuisson (grill, four à pain), ruche, collecteur de sève.

## En cas d'erreur

Copie-colle l'erreur complète (terminal ou `LogOutput.log`) dans le fil du projet.
