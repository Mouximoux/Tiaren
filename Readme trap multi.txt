Le networkManager ne peut pas �tre le game si on veut pouvoir utiliser isServer et ne pas laisser la logique du jeu exister cot� client par exemple
NetworkServer n'existe que cot� server; Si on en a besoin pour retrieve un objet utiliser plutot ClientScene
Pour spawn un machin il faut l'ajouter au networkmanager pour qu'il puisse �tre r�pliqu� cot� client et appeller une m�thode statique NetworkServer.Spawn apr�s avoir instanci� l'objet
Ne pas oublier de mettre un network transform. Il n'est pas forc�ment important de transf�rer beaucoup d'info au client,
n'envoyons que ce qui est n�cessaire pour calculer le graphique. Exemple: la couleur des blocs.
A l'inverse ne pas faire de calculs inutiles au serveur? =>Jump.
Les personnages devraient avoir une autorit� locale pour pouvoir dire a