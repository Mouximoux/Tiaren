﻿using System.Collections;
using System.Collections.Generic;
using System.IO;

using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Classe représentant une grille de jeux pour une partie
/// </summary>

public class Grille : MonoBehaviour
{
    private Game gameManager;
    //  50 x 50 x 5 = 12 500 blocs
    public int sizeX = 50;
    public int sizeY = 6;
    public int sizeZ = 50;
    public DistanceAndParent[,,] gridBool;


    /// <summary>
    /// Représente la grille de jeux
    /// </summary>

    private Placeable[,,] grid;
    /// <summary>
    /// Paramètre de génération de la grille
    /// </summary>
    public int randomParameter = 90;
    /// <summary>
    /// on mettra tous les floaties dedans pour ne pas avoir a les chercher lors de la gravite connexe
    /// </summary>
    public List<Placeable> floaties;

    public GameObject[] prefabsList;


    public Placeable[,,] Grid
    {
        get
        {
            return grid;
        }

        set
        {
            grid = value;
        }
    }

    public Game GameManager
    {
        get
        {
            return gameManager;
        }

        set
        {
            gameManager = value;
        }
    }



    /// <summary>
    /// Crée une grille aléatoire a partir de randomParameter
    /// </summary>
    public void CreateRandomGrid(GameObject parent)
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    if (Random.Range(0, 100) < randomParameter && y < sizeY - 2) // la deuxième condition pourrait simplement etre dans le for du Y
                    {
                        GameObject obj = Instantiate(prefabsList[0], new Vector3(x, y, z), Quaternion.identity, parent.transform);
                        obj.GetComponent<Placeable>().Position = new Vector3Int(x, y, z);
                        // Debug.Log(obj.GetComponent<Placeable>().Position);
                        obj.GetComponent<Placeable>().GameManager = this.gameManager;
                        Grid[x, y, z] = obj.GetComponent<Placeable>();
                        NetworkServer.Spawn(obj);



                    }
                }
            }
        }

    }


    /// <summary>
    /// Fonction permettant de trouver et afficher un chemin d'un point A à un point B
    /// Seuls les cases sur lesquelles on est sûrs de pouvoir aller sont ajoutées
    /// <param name="deplacement">Valeur de déplacement d'un personnage</param>
    /// <param name="saut">Valeur de saut d'un personnage</param>
    /// <param name="positionBloc">Position du bloc de départ, c'est à dire celui SOUS le personnage</param>
    /// <returns>Retourne une grille qui permet d'avoir les positions de chacun des blocs du chemin, c'est à dire les blocs appartenant au SOL. Le personnage se déplacera une case au dessus</returns>
    /// </summary>
    public DistanceAndParent[,,] CanGo(LivingPlaceable livingPlaceable, int saut, Vector3Int positionBloc)
    {

        int deplacement = livingPlaceable.PmActuels;



        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    gridBool[x, y, z] = new DistanceAndParent(x, y, z);
                }
            }
        }

        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        queue.Enqueue(positionBloc);
        DistanceAndParent parent = null;
        gridBool[positionBloc.x, positionBloc.y, positionBloc.z].SetDistance(0);
        while (parent == null || ((parent.GetDistance() <= deplacement) && queue.Count != 0))
        {
            //On mets ceux à coté, ceux au dessus accessibles ou ceux en dessous accessibles
            //coté

            Vector3Int posBlocActuel = queue.Dequeue();


            parent = gridBool[posBlocActuel.x, posBlocActuel.y, posBlocActuel.z];
            if (parent.GetDistance() <= deplacement)
            {

                Placeable testa = Grid[posBlocActuel.x, posBlocActuel.y, posBlocActuel.z];
                if (testa.gameObject.GetComponent<Renderer>() != null)
                {
                   //On envoie plutot l'id parce que rien n'assure que les blocs soient a la bonne position coté client
                    gameManager.PlayingPlaceable.Joueur.RpcMakeCubeBlue(testa.netId);
                  
                }
                //là mettre l'affichage du bloc en bleu
                for (int yactuel = -saut + 1; yactuel < saut; yactuel++)
                {

                    if (posBlocActuel.y + yactuel >= 0 && posBlocActuel.y + yactuel < sizeY && posBlocActuel.x < sizeX - 1 && //au dessus de 0, en dessous du max, dans le terrain en x
                        gridBool[posBlocActuel.x + 1, posBlocActuel.y + yactuel, posBlocActuel.z].GetDistance() == -1 && //si on l'a pas dejà vu
                        Grid[posBlocActuel.x + 1, posBlocActuel.y + yactuel, posBlocActuel.z] != null &&        // et si le bloc existe
                        (Grid[posBlocActuel.x + 1, posBlocActuel.y + yactuel + 1, posBlocActuel.z] == null  //si le bloc au dessus est vide
                        || Grid[posBlocActuel.x + 1, posBlocActuel.y + yactuel + 1, posBlocActuel.z].TraversableChar == TraversableType.ALLTHROUGH //ou traversable en général
                        || Grid[posBlocActuel.x + 1, posBlocActuel.y + yactuel + 1, posBlocActuel.z].TraversableChar == TraversableType.ALLIESTHROUGH && //ou seulement par un allié
                        Grid[posBlocActuel.x + 1, posBlocActuel.y + yactuel + 1, posBlocActuel.z].Joueur == livingPlaceable.Joueur) //si on est l'allié
                        && Grid[posBlocActuel.x + 1, posBlocActuel.y + yactuel, posBlocActuel.z].Walkable
                        )
                    {//si le sommet n'est pas marqué et qu'il existe, et qu'il n'y a rien au dessus ou que celui du dessus est traversable, que le bloc est walkable

                        queue.Enqueue(new Vector3Int(posBlocActuel.x + 1, posBlocActuel.y + yactuel, posBlocActuel.z));
                        gridBool[posBlocActuel.x + 1, posBlocActuel.y + yactuel, posBlocActuel.z].SetDistance(
                            parent.GetDistance() + 1);
                        gridBool[posBlocActuel.x + 1, posBlocActuel.y + yactuel, posBlocActuel.z].SetParent(parent);
                    }

                    if (posBlocActuel.y + yactuel >= 0 && posBlocActuel.y + yactuel < sizeY && posBlocActuel.x > 0 &&
                        gridBool[posBlocActuel.x - 1, posBlocActuel.y + yactuel, posBlocActuel.z].GetDistance() == -1 &&
                        Grid[posBlocActuel.x - 1, posBlocActuel.y + yactuel, posBlocActuel.z] != null &&
                        (Grid[posBlocActuel.x - 1, posBlocActuel.y + yactuel + 1, posBlocActuel.z] == null
                        || Grid[posBlocActuel.x - 1, posBlocActuel.y + yactuel + 1, posBlocActuel.z].TraversableChar == TraversableType.ALLTHROUGH
                        || Grid[posBlocActuel.x - 1, posBlocActuel.y + yactuel + 1, posBlocActuel.z].TraversableChar == TraversableType.ALLIESTHROUGH &&
                        Grid[posBlocActuel.x - 1, posBlocActuel.y + yactuel + 1, posBlocActuel.z].Joueur == livingPlaceable.Joueur)
                        && Grid[posBlocActuel.x - 1, posBlocActuel.y + yactuel, posBlocActuel.z].Walkable)
                    {//si le sommet n'est pas marqué et qu'il existe, et qu'il n'y a rien au dessus

                        queue.Enqueue(new Vector3Int(posBlocActuel.x - 1, posBlocActuel.y + yactuel, posBlocActuel.z));
                        gridBool[posBlocActuel.x - 1, posBlocActuel.y + yactuel, posBlocActuel.z].SetDistance(
                            parent.GetDistance() + 1);
                        gridBool[posBlocActuel.x - 1, posBlocActuel.y + yactuel, posBlocActuel.z].SetParent(parent);


                    }
                    if (posBlocActuel.y + yactuel >= 0 && posBlocActuel.y + yactuel < sizeY && posBlocActuel.z < sizeZ - 1 &&
                        gridBool[posBlocActuel.x, posBlocActuel.y + yactuel, posBlocActuel.z + 1].GetDistance() == -1 &&
                        Grid[posBlocActuel.x, posBlocActuel.y + yactuel, posBlocActuel.z + 1] != null &&
                        (Grid[posBlocActuel.x, posBlocActuel.y + yactuel + 1, posBlocActuel.z + 1] == null
                        || Grid[posBlocActuel.x, posBlocActuel.y + yactuel + 1, posBlocActuel.z + 1].TraversableChar == TraversableType.ALLTHROUGH
                        || Grid[posBlocActuel.x, posBlocActuel.y + yactuel + 1, posBlocActuel.z + 1].TraversableChar == TraversableType.ALLIESTHROUGH &&
                        Grid[posBlocActuel.x, posBlocActuel.y + yactuel + 1, posBlocActuel.z + 1].Joueur == livingPlaceable.Joueur)
                        && Grid[posBlocActuel.x, posBlocActuel.y + yactuel, posBlocActuel.z + 1].Walkable)

                    {//si le sommet n'est pas marqué et qu'il existe et qu'il n'y a rien au dessus
                        queue.Enqueue(new Vector3Int(posBlocActuel.x, posBlocActuel.y + yactuel, posBlocActuel.z + 1));
                        gridBool[posBlocActuel.x, posBlocActuel.y + yactuel, posBlocActuel.z + 1].SetDistance(
                            parent.GetDistance() + 1);
                        gridBool[posBlocActuel.x, posBlocActuel.y + yactuel, posBlocActuel.z + 1].SetParent(parent);

                    }
                    if (posBlocActuel.y + yactuel >= 0 && posBlocActuel.y + yactuel < sizeY && posBlocActuel.z > 0 &&
                        gridBool[posBlocActuel.x, posBlocActuel.y + yactuel, posBlocActuel.z - 1].GetDistance() == -1 &&
                        Grid[posBlocActuel.x, posBlocActuel.y + yactuel, posBlocActuel.z - 1] != null &&
                        (Grid[posBlocActuel.x, posBlocActuel.y + yactuel + 1, posBlocActuel.z - 1] == null
                        || Grid[posBlocActuel.x, posBlocActuel.y + yactuel + 1, posBlocActuel.z - 1].TraversableChar == TraversableType.ALLTHROUGH
                        || Grid[posBlocActuel.x, posBlocActuel.y + yactuel + 1, posBlocActuel.z - 1].TraversableChar == TraversableType.ALLIESTHROUGH &&
                        Grid[posBlocActuel.x, posBlocActuel.y + yactuel + 1, posBlocActuel.z - 1].Joueur == livingPlaceable.Joueur)
                        && Grid[posBlocActuel.x, posBlocActuel.y + yactuel, posBlocActuel.z - 1].Walkable)
                    {//si le sommet n'est pas marqué et qu'il existe, et qu'il n'y a rien au dessus
                        queue.Enqueue(new Vector3Int(posBlocActuel.x, posBlocActuel.y + yactuel, posBlocActuel.z - 1));
                        gridBool[posBlocActuel.x, posBlocActuel.y + yactuel, posBlocActuel.z - 1].SetDistance(
                            parent.GetDistance() + 1);
                        gridBool[posBlocActuel.x, posBlocActuel.y + yactuel, posBlocActuel.z - 1].SetParent(parent);
                    }
                }
            }

        }

        return gridBool;
    }
    /// <summary>
    /// Actualise la position de chaque bloc de la grille
    /// </summary>
    public void ActualisePosition()
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    if (Grid[x, y, z] != null)
                    {
                        Grid[x, y, z].gameObject.transform.position = new Vector3(x, y, z);

                    }
                }
            }
        }
    }

    /// <summary>
    /// Constructeur standard
    /// </summary>
    public Grille()
    {

    }

    /// <summary>
    /// Fonction qui intitalise la valeur du booléen explored des blocs de la grille
    /// </summary>
    public void InitialiseExplored(bool value)
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    if (Grid[x, y, z] != null)
                    {
                        Grid[x, y, z].Explored = value;
                    }
                }
            }
        }

    }
    /// <summary>
    /// Parcours de recherche en profondeur pour la gravite connexe
    /// ça les explore ok, mais il manque la partie chute si non gravité
    /// </summary>
    /// <param name="positionSommetX"></param>
    /// <param name="positionSommetY"></param>
    /// <param name="positionSommetZ"></param>
	public void Explore(int positionSommetX, int positionSommetY, int positionSommetZ)
    {
        if (!Grid[positionSommetX, positionSommetY, positionSommetZ].Explored)
        {
            Grid[positionSommetX, positionSommetY, positionSommetZ].Explored = true;
            //Debug.Log("x" + positionSommetX + "y" + positionSommetY + "z" + positionSommetZ + "bool" + Grid[positionSommetX, positionSommetY, positionSommetZ].Explored);

            //on considère les 4 à cotés et ceux au dess(o)us ... donc les 6 cotés ?
            //si les voisins sont  non nuls et non explorés et toujours dans la map
            if (positionSommetX > 0 && Grid[positionSommetX - 1, positionSommetY, positionSommetZ] != null &&
                !Grid[positionSommetX - 1, positionSommetY, positionSommetZ].Explored)
            {
                Explore(positionSommetX - 1, positionSommetY, positionSommetZ);
            }
            if (positionSommetX < sizeX - 1 && Grid[positionSommetX + 1, positionSommetY, positionSommetZ] != null &&
                !Grid[positionSommetX + 1, positionSommetY, positionSommetZ].Explored)
            {
                Explore(positionSommetX + 1, positionSommetY, positionSommetZ);
            }
            if (positionSommetY > 0 && Grid[positionSommetX, positionSommetY - 1, positionSommetZ] != null &&
                !Grid[positionSommetX, positionSommetY - 1, positionSommetZ].Explored)
            {
                Explore(positionSommetX, positionSommetY - 1, positionSommetZ);
            }
            if (positionSommetY < sizeY - 1 && Grid[positionSommetX, positionSommetY + 1, positionSommetZ] != null &&
                !Grid[positionSommetX, positionSommetY + 1, positionSommetZ].Explored)
            {
                Explore(positionSommetX, positionSommetY + 1, positionSommetZ);
            }
            if (positionSommetZ > 0 && Grid[positionSommetX, positionSommetY, positionSommetZ - 1] != null &&
                !Grid[positionSommetX, positionSommetY, positionSommetZ - 1].Explored)
            {
                Explore(positionSommetX, positionSommetY, positionSommetZ - 1);
            }
            if (positionSommetZ < sizeZ - 1 && Grid[positionSommetX, positionSommetY, positionSommetZ + 1] != null &&
                !Grid[positionSommetX, positionSommetY, positionSommetZ + 1].Explored)
            {
                Explore(positionSommetX, positionSommetY, positionSommetZ + 1);
            }
        }
    }

    /// <summary>
    /// Vérifie que toute la grille a été explorée
    /// </summary>
    /// <returns></returns>
    public bool IsGridAllExplored()
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    if (Grid[x, y, z] != null && !Grid[x, y, z].Explored)
                    // si on a des trucs non explorés, c'est à dire qu'ils doivent tomber donc faux
                    {
                        Debug.Log("x" + x + "y" + y + "z" + z);
                        return false;

                    }
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Seule fonction qui sera appelée pour gérer toute la gravité.
    /// </summary>
    public void Gravite()
    {
        GraviteSimple();

        //VERIFIER que a la creation explored = false. Tant que tous ne sont pas explorés
        while (!IsGridAllExplored())
        {
            //  On commence sur tous ceux d'en bas qui ne sont pas marqués, on expand,
            //  puis on passe aux floaties, on expand. Tous les autres tomberont. 
            //  0 = pas vu rien a faire; 1 vu
            InitialiseExplored(false);

            //On commence sur tous ceux d'en bas qui ne sont pas marqués, on expand , puis on passe aux floaties, on expand. Tous les autres tomberont. 

            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        if (Grid[x, y, z] != null && !Grid[x, y, z].Explored && (y == 0 || Grid[x, y, z].GravityType == GravityType.GRAVITE_NULLE))
                        //On lance le dfs sur les non vides qui ne sont pas explorés, au sol ou sans gravité
                        {
                            Explore(x, y, z);

                        }


                    }
                }
            }
            //Ensuite tous les blocs non nuls avec explored à false tombent de 1
            TombeConnexe();

        }
    }
    /// <summary>
    /// Fait tomber les blocs qui sont en gravite connexe. Il s'agit de tous les non nuls non explorés
    /// On tombe tant qu'il n'y a rien en dessous ou une une une une ?
    /// </summary>
    public void TombeConnexe()
    {

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    if (Grid[x, y, z] != null && !Grid[x, y, z].Explored) // si le bloc est non nul, que le bloc était sensé tombé
                                                                          //Alors on tombe.
                    {

                        GereEcrasageBloc(x, y, z, 1); // On peut descendre de 1 il faut juste appliquer le bon ecrasage si besoin
                    }
                }
            }
        }

    }
    public void DeplaceBloc(Placeable bloc, Vector3Int positionVoulue)
    {
        if (bloc != null && positionVoulue.x >= 0 && positionVoulue.x < sizeX
           && positionVoulue.y >= 0 && positionVoulue.y < sizeY
           && positionVoulue.z >= 0 && positionVoulue.z < sizeZ &&
           Grid[positionVoulue.x, positionVoulue.y, positionVoulue.y] == null
         || (Grid[positionVoulue.x, positionVoulue.y, positionVoulue.y] != null
         && Grid[positionVoulue.x, positionVoulue.y, positionVoulue.y].Destroyable))
        {
            Grid[positionVoulue.x, positionVoulue.y, positionVoulue.z] = Grid[bloc.Position.x, bloc.Position.y, bloc.Position.z];//met un lien
            Grid[positionVoulue.x, positionVoulue.y, positionVoulue.z].gameObject.transform.position += (positionVoulue - bloc.Position);//on decale le modèle
            Grid[bloc.Position.x, bloc.Position.y, bloc.Position.z] = null;//on met a zero l'ancien endroit
            Grid[positionVoulue.x, positionVoulue.y, positionVoulue.z].Position = positionVoulue;//on change la Position

        }
    }
    /// <summary>
    /// Fonction qui va s'occuper de gérer les colisions verticale d'un bloc par un autre
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="ydescente"></param>
    public void GereEcrasageBloc(int x, int y, int z, int ydescente)
    {
        if (Grid[x, y - ydescente, z] == null)// On copie et on détruit
        {
            DeplaceBloc(Grid[x, y, z], new Vector3Int(x, y - ydescente, z));
        }

        else if (Grid[x, y - ydescente, z].Ecrasable == EcraseType.ECRASEDESTROYBLOC)// On détruit le bloc et on trigger ses effets
        {
            Grid[x, y, z].Detruire();
            Grid[x, y, z] = null;

        }
        else if (Grid[x, y - ydescente, z].Ecrasable == EcraseType.ECRASELIFT)// On copie et on détruit
        {
            int ymontee = y;
            while (ymontee < sizeY && Grid[x, ymontee, z] != null) // la verification devrait être non nécessaire en y mais bon -_-
            {
                ymontee++;
            }

            Grid[x, ymontee, z] = Grid[x, y - ydescente, z].Cloner();
            Grid[x, ymontee, z].Position.Set(x, ymontee, z);


            Grid[x, y - ydescente, z] = Grid[x, y, z].Cloner();
            Grid[x, y - ydescente, z].Position.Set(x, y - ydescente, z);
            Grid[x, y, z] = null;
        }
        else if (Grid[x, y - ydescente, z].Ecrasable == EcraseType.ECRASEDEATH)
        {
            Grid[x, y - ydescente, z].Detruire();
            Grid[x, y - ydescente, z] = Grid[x, y, z].Cloner();
            Grid[x, y - ydescente, z].Position.Set(x, y - ydescente, z);
            Grid[x, y, z] = null;

        }



        //Soit c'était un écrasable de type ecrasestay, soit c'était du vide

    }
    /// <summary>
    /// Gère la gravite des objets de type gravite simple, c'est à dire s'il n'y a rien en dessous on tombe
    /// </summary>
    public void GraviteSimple()
    {
        int y = 0;
        while (y < sizeY)
        {
            for (int x = 0; x < sizeX; x++)
            {

                for (int z = 0; z < sizeZ; z++)
                {
                    if (Grid[x, y, z] != null && Grid[x, y, z].GravityType == GravityType.GRAVITE_SIMPLE)
                    {
                        int ydescente = 0;

                        while (y - ydescente > 0 && (Grid[x, y - ydescente - 1, z] == null
                            || (Grid[x, y - ydescente - 1, z].Ecrasable != EcraseType.ECRASESTAY)))

                        {
                            ydescente++;
                        }
                        if (ydescente > 0)
                        {
                            GereEcrasageBloc(x, y, z, ydescente);




                        }


                    }
                }
            }
            y++;
        }
    }
    /// <summary>
    /// Sauvegarde la grille dans un fichier json en utilisant un jaggedarray
    /// </summary>
    public void SaveGridFile()
    {
        JaggedGrid jagged = new JaggedGrid();
        jagged.ToJagged(this);
        jagged.Save();

    }



    void Awake()
    {
        gridBool = new DistanceAndParent[sizeX, sizeY, sizeZ];
        grid = new Placeable[sizeX, sizeY, sizeZ];



    }

    /// <summary>
    /// Precondition: grille vide jamais remplie
    /// </summary>
    /// <param name="grid"></param>
    public void FillThisGrilleAndSpawn(GameObject parent)
    {
        JaggedGrid jagged = JaggedGrid.FillGridFromJSON();

        for (int y = 0; y < sizeY; y++)
        {
            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    if (jagged.grille[y * sizeZ * sizeX + z * sizeX + x] != 0) //On suppose que la grille était vide et jamais remplie!
                    {

                        GameObject obj = Instantiate(gameManager.networkManager.spawnPrefabs[jagged.grille[y * sizeZ * sizeX + z * sizeX + x] - 1],
                            new Vector3(x, y, z), Quaternion.identity, parent.transform);
                        obj.GetComponent<Placeable>().Position = new Vector3Int(x, y, z);

                        // Debug.Log(obj.GetComponent<Placeable>().Position);
                        obj.GetComponent<Placeable>().GameManager = gameManager;


                        Grid[x, y, z] = obj.GetComponent<Placeable>(); //ce qui nous intéresse c'est pas le game object
                        NetworkServer.Spawn(obj);


                    }

                }
            }
        }
    }

}
