# Guide de Console Mode

Les détails qui ne tiennent pas dans le [README](../README.fr-FR.md).

[English](GUIDE.md) · [Português (Brasil)](GUIDE.pt-BR.md) · [Español](GUIDE.es-ES.md) · [Deutsch](GUIDE.de-DE.md) · Français · [Русский](GUIDE.ru-RU.md)

## Modes et restauration

| Mode | À la sortie |
|------|---------|
| **Steam Big Picture** | Restauration automatique (l'application reste dans la zone de notification) |
| **Playnite en plein écran** | Restauration automatique (l'application reste dans la zone de notification) |
| **Mode Xbox** | Manuelle : utilisez *Restaurer maintenant*, le menu de la zone de notification, ou rouvrez la fenêtre |
| **Application personnalisée** | Restauration automatique quand l'application (ou le processus choisi à surveiller) se ferme |

Vous pouvez aussi restaurer à tout moment depuis la zone de notification (*Restaurer la configuration* / *Afficher la fenêtre*). Avec les écrans noirs, **Échap** retire les rideaux.

Le menu de session propose deux sorties. **Retour au PC** termine la session et remet les écrans, la résolution, le HDR, le son et la limite de FPS comme avant ; l'application reste dans la zone de notification. **Quitter Console Mode** fait la même chose puis ferme l'application : les raccourcis manette ne fonctionnent plus jusqu'à ce que vous la rouvriez. À la première ouverture, le menu propose une visite qui explique chaque option ; pour la revoir plus tard, appuyez sur **Y** (△ sur PlayStation) ou **F1** dans le menu, ou utilisez **Paramètres → Visite du menu de session**.

## Paramètres en mode console

Dans **Système → Manette**, choisissez la disposition des boutons : **Automatique**, **Xbox** ou **PlayStation**. L'aperçu et les libellés des raccourcis suivent ce choix, qui est enregistré à la fermeture de l'application. Seuls les symboles affichés changent ; les entrées de la manette gardent leur correspondance actuelle.

L'onglet **Système** offre les mêmes paramètres que le mode Bureau, y compris les raccourcis manette, Playnite, Android TV, les FPS personnalisés et le dossier de données. Les deux interfaces partagent les valeurs enregistrées, qui sont conservées après un redémarrage de l'application.

Ouvrez l'action de raccourci à configurer, choisissez **Définir** ou **Modifier**, maintenez la combinaison voulue sur une manette, puis relâchez les boutons. **Supprimer** désactive ce raccourci. Pendant la capture, les boutons de la manette ne naviguent pas dans l'interface ; utilisez **Annuler** ou **Échap** pour arrêter la capture. Hors capture, **B** quitte l'éditeur. Les champs d'adresse et de FPS personnalisés acceptent la saisie au clavier.

Au retour de Big Picture, **Réduire Steam dans la zone de notification au retour au PC** ferme la fenêtre du client tout en laissant Steam en marche. Si l'option est désactivée, le client de bureau reste ouvert. Les fenêtres de Steam ne sont pas touchées quand un jeu est en cours ou quand son état ne peut pas être vérifié.

Changer d'écran principal peut amener Windows à mélanger les icônes du bureau. **Remettre les icônes du bureau à leur place** (activé par défaut) enregistre leur position avant la session et les remet en place une fois le bureau restauré. Sans effet si **Réorganiser automatiquement les icônes** est activé sur le bureau, car Windows les place alors lui-même.

**Réveiller le PC avec la manette** permet à l'adaptateur sans fil Xbox, au Bluetooth ou à une manette filaire de sortir le PC de la veille avec le bouton central de la manette. **Activer** demande l'autorisation d'administrateur à Windows et coche *Autoriser ce périphérique à sortir l'ordinateur du mode veille* sur ces périphériques. Un PC éteint ou en veille prolongée ne se réveille pas, et certains PC coupent le Bluetooth pendant la veille. Avec **Entrer à la connexion d'une manette**, le PC se réveille directement en mode console.

Les **profils par jeu** donnent à chaque jeu Steam sa propre résolution, son HDR, sa limite de FPS et sa sortie audio. Pendant que le jeu tourne, ouvrez le menu de la session et choisissez **Profil du jeu → Enregistrer les réglages actuels pour…** : ce qu'utilise l'écran de jeu à ce moment-là est enregistré. La prochaine fois que ce jeu démarre pendant une session, Console Mode applique le profil et, quand le jeu se ferme, l'écran revient à son état précédent. Le jeu est détecté via Steam, qu'il soit lancé depuis Big Picture, Playnite ou l'application Xbox. Les profils enregistrés sont listés dans Paramètres → **Profils par jeu**, où vous pouvez les supprimer (dans l'interface Console : Système → À l'entrée en mode console).

## API de contrôle local

Les liens `consolemode://` s'envoient sans attendre de réponse. Les outils qui ont besoin d'une réponse (un agent de contrôle à distance qui tourne comme service Windows, un plugin Stream Deck qui indique si le mode console est actif) peuvent utiliser le canal nommé `\\.\pipe\ConsoleMode.Control` tant que l'application tourne : envoyez une ligne JSON, recevez-en une en retour.

```
→ {"cmd":"status"}          // or "start", "stop", "show"
← {"ok":true,"active":true,"restoring":false,"mode":"xboxMode","version":"1.5.0"}
```

`start`, `stop` et `show` font exactement ce que fait le lien `consolemode://` correspondant, puis répondent une fois l'application stabilisée (`ok:false` avec un `error` si le mode console n'a pas démarré ou si la restauration n'a pas abouti). `status` se contente de lire. Seuls l'utilisateur connecté et LocalSystem peuvent se connecter ; rien n'est exposé sur le réseau.

## Contrôle de la TV

Paramètres → **TV** peut allumer la TV et la basculer sur l'entrée HDMI du PC au démarrage du mode console, et en option la remettre en veille une fois le bureau restauré. Une TV qui ne répond pas ne bloque jamais le mode console : l'application le note dans le journal et attend l'écran de jeu comme d'habitude.

Avec la valeur par défaut, **Ne pas la contrôler**, rien n'est envoyé et rien ne s'ajoute au démarrage ni à la restauration. Avec une méthode choisie, l'étape TV s'exécute en premier au démarrage du mode console et dure au plus **30 secondes** (le pire cas, quand la TV ne répond pas et que le Wake-on-LAN est tenté) ; la mise en veille facultative à la restauration dure au plus **15 secondes**. Une TV déjà allumée répond en général en une ou deux secondes.

La plupart des cartes graphiques de PC ne savent pas envoyer de HDMI-CEC, donc l'application parle à la TV par le réseau :

### Google TV / Android TV

TCL, Sony, Hisense, Philips et les autres TV sous Google TV ou Android TV, via ADB (le protocole de débogage d'Android). Rien à installer sur le PC.

1. Sur la TV : **Paramètres → Système → À propos** (Settings → System → About), appuyez 7 fois sur **Build de l'OS Android TV** (Android TV OS build) pour débloquer les options pour les développeurs.
2. **Paramètres → Système → Options pour les développeurs** (Developer options) : activez le **Débogage USB** (USB debugging) (sur certaines TV, **Débogage réseau** / **ADB via le réseau**).
3. Dans Console Mode, choisissez *Google TV / Android TV (réseau)*, saisissez l'adresse IP de la TV (Paramètres → Réseau sur la TV ; réservez-la dans votre box ou routeur) et l'entrée HDMI utilisée par le PC.
4. Appuyez sur **Tester maintenant**. La TV demande « Autoriser le débogage depuis cet ordinateur ? » (Allow debugging from this computer?) : cochez **Toujours autoriser** (Always allow) et appuyez sur **Autoriser** (Allow).

La clé ADB du PC est stockée chiffrée pour votre utilisateur Windows (DPAPI), donc la TV ne pose la question qu'une fois. Si le dossier de données est copié vers un autre utilisateur Windows ou un autre PC, la clé n'y est pas lisible : l'application en crée une nouvelle et la TV redemande « Autoriser le débogage ? ».

Le réveil utilise la touche de réveil d'Android, puis la touche **HDMI 1-4**. Si votre TV ignore cette touche, renseignez dans **Commande d'entrée (avancé)** n'importe quelle commande shell Android qui ouvre l'entrée du PC. Si la TV disparaît du réseau en veille, indiquez son **Adresse MAC** pour que l'application envoie d'abord un Wake-on-LAN (l'option « Réveil par le réseau » / « Réveil par Wi-Fi » de la TV doit être activée).

Le « Débogage sans fil » (Wireless debugging) avec code d'association (téléphones Android 11+) est un autre protocole, encapsulé en TLS, et n'est pas pris en charge : utilisez le débogage USB / réseau.

## Extras facultatifs

### HDR

Active le HDR sur l'écran de jeu tant que le mode console est actif. Il est désactivé (ou remis dans son état précédent) à la sortie.

### VRR

Console Mode peut activer ou désactiver le paramètre Windows de fréquence d'actualisation variable (VRR). Pour de meilleurs résultats, activez aussi VRR / G-SYNC / FreeSync dans le **panneau de configuration du GPU** (NVIDIA ou AMD).

### Limite de FPS (RTSS)

Plafonne la fréquence d'images globale pendant le mode console (utile sur une TV 60 Hz). Nécessite RTSS installé et lancé. La limite précédente est rétablie à la sortie.

## Limitations

- Les configurations multi-écrans varient ; sur certaines, la restauration peut demander un second essai depuis la zone de notification
- Le mode Xbox ne détecte pas la fin du plein écran : restaurez manuellement
- La limite de FPS est globale (limitation de RTSS), pas par écran
- La version WinUI 3 nécessite actuellement Windows pour être compilée (`net8.0-windows`)

## Dépannage

### La disposition du bureau n'a pas été restaurée

Ouvrez le menu de la zone de notification et choisissez **Restaurer la configuration**. Si la disposition semble encore incorrecte, choisissez à nouveau **Restaurer la configuration** une fois que Windows a fini d'appliquer le changement d'écrans. Vous pouvez aussi rouvrir la fenêtre depuis la zone de notification et restaurer manuellement.

### Le son est resté sur la sortie précédente

Vérifiez que la sortie cible est branchée et disponible dans Windows avant de lancer le mode console. Pour les sorties HDMI/TV, il peut être nécessaire de rebrancher le câble et de relancer le mode.

### La manette est détectée mais ne fait rien (DualSense / DualShock)

Ouvrez **Paramètres → Tester la manette** : la lecture en direct montre ce que Windows reçoit de chaque manette. Si elle reste vide quand vous appuyez sur les boutons, c'est très probablement Steam qui capture la manette (Steam lancé avec la prise en charge PlayStation activée dans Steam Input la transforme en clavier/souris sur le bureau). Fermez Steam, ou désactivez la prise en charge PlayStation dans Steam Input, puis testez à nouveau. Si la lecture n'affiche toujours rien, utilisez **Copier le diagnostic** et collez-le dans le formulaire de commentaires.

### Le HDR ou le VRR n'a pas changé

Vérifiez que l'écran de jeu prend en charge la fonction et que le HDR est activé dans Windows. Pour le VRR, activez aussi G-SYNC ou FreeSync dans le panneau de configuration du GPU si besoin.

## Compiler depuis les sources (Windows)

Voir la section du guide en anglais : [GUIDE.md#build-from-source-windows](GUIDE.md#build-from-source-windows).

*Cette page est une traduction du guide en anglais. En cas de différence, c'est la version anglaise qui fait foi.*
