Le networkManager ne peut pas �tre le game si on veut pouvoir utiliser isServer et ne pas laisser la logique du jeu exister cot� client par exemple
NetworkServer n'existe que cot� server; Si on en a besoin pour retrieve un objet utiliser plutot ClientScene
Pour spawn un machin il faut l'ajouter au networkmanager pour qu'il puisse �tre r�pliqu� cot� client et appeller une m�thode statique NetworkServer.Spawn apr�s avoir instanci� l'objet
Ne pas oublier de mettre un network transform. Il n'est pas forc�ment important de transf�rer beaucoup d'info au client,
n'envoyons que ce qui est n�cessaire pour calculer le graphique. Exemple: la couleur des blocs.
A l'inverse ne pas faire de calculs inutiles au serveur? =>Jump.
Les personnages devraient avoir une autorit� locale pour pouvoir dire...
Lorsqu'on utilise joueur.Rpc, seul le player concern� recoit l'info, mais sur tous les clients.
Si on envoie un rpc sur un objet dupliqu� par tout le monde, ex: GameManager tous les clients font l'action.
Les joueurs adverses sont �galement pr�sent sur les deux clients, il est donc de bon ton de v�rifier quand on le veut qu'il s'agit bien du local player
Pour certaine Reasons (Tm) envoy� des commandes depuis les GUI marche mal, passer par une fonction locale interm�diaire au niveau du player script(required? dunno) marche bien 