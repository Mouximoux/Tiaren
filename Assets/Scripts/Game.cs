﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Game : MonoBehaviour {
    private int numberTurn=0;

    private Placeable shotPlaceable;
    public GameObject[] prefabPersos;
    public GameObject[] prefabArmes;

    public GameObject joueur1; //Should be Object
    
    public GameObject joueur2; //Should be Object
    public GameObject[] prefabMonstres;

    private List<GameObject> listeMonstresNeutres=new List<GameObject>(); 

    private List<Effect> listeEffectsDebutTour=new List<Effect>(); 
    private List<Effect> listeEffectsFinTour=new List<Effect>();
    private int capacityinUse=0;
    private GameEffectManager gameEffectManager;
    private Grille grilleJeu;
    private Timer clock;
    private Joueur winner;
    private Vector3Int placeToGo;
    /// <summary>
    /// Permet de savoir si le jeu est fini
    /// </summary>
    /// <returns></returns>

    /// <summary>
    /// indique le numéro du tour actuel
    /// </summary>
    /// 
    public int NumberTurn
    {
        get
        {
            return numberTurn;
        }

        set
        {
            numberTurn = value;
        }
    }

    /// <summary>
    /// Indique le joueur en train de jouer
    /// </summary>
   

    public Grille GrilleJeu
    {
        get
        {
            return grilleJeu;
        }

        set
        {
            grilleJeu = value;
        }
    }

    public Joueur Winner
    {
        get
        {
            return winner;
        }

        set
        {
            winner = value;
        }
    }

    public Placeable ShotPlaceable
    {
        get
        {
            return shotPlaceable;
        }

        set
        {
            shotPlaceable = value;
        }
    }

    public GameEffectManager GameEffectManager
    {
        get
        {
            return gameEffectManager;
        }

        set
        {
            gameEffectManager = value;
        }
    }

    public Vector3Int PlaceToGo
    {
        get
        {
            return placeToGo;
        }

        set
        {
            placeToGo = value;
        }
    }

    private void Awake()

    {
        //    DontDestroyOnLoad(gameObject);

        this.GameEffectManager = gameObject.GetComponent<GameEffectManager>();
        this.clock = gameObject.GetComponent<Timer>();
        this.grilleJeu = gameObject.GetComponent<Grille>();
        grilleJeu.GameManager = this;
        grilleJeu.CreateRandomGrid();
      


        //  grilleJeu.ActualisePosition();
        this.PlaceToGo = new Vector3Int(-1, -1, -1);


    }
    public void ChangeCapacityOnUse(int i)
    {
        this.capacityinUse = i;   
    }

    /** Déroulement 
1)On reset pm et le bool de tir/compétences
On applique les effets qui prennent effet en début de tour
On soustrait un tour aux effets persistants attachés

On laisse la main à l'utilisateur, qui peut utiliser une compétence et ses pm
A chaque fois qu'un effet se déclenche, il est ajouté au game manager
Le game manager applique vérifie que l'effet est activable /: pas contré/ autre	puis le use()
On continue jusqu'à la fin des 30s /
**/


    // Use this for initialization
    IEnumerator Start()
    {
        //quand on aura la serialization bien:
        //on lit la position et le numero dans le fichier et non dans le numero prefab joueur on instancie.
        //Dans le awake de perso on lit les bonnes valeurs également.

        for (int i = 0; i < joueur1.GetComponent<Joueur>().NumeroPrefab.Count ; i++)
        {
            GameObject pers = Instantiate(prefabPersos[joueur1.GetComponent<Joueur>().NumeroPrefab[i]], new Vector3(0, 3.5f, 0),Quaternion.identity);
            


            Personnage pers1 = pers.GetComponent<Personnage>();
            pers1.GameManager = this;
            joueur1.GetComponent<Joueur>().Personnages.Add(pers);
            pers1.Joueur = joueur1.GetComponent<Joueur>();
            pers1.Position = new Vector3Int(0, 4, 0);
            Vector3Int posPers = pers1.Position;
            this.grilleJeu.Grid[posPers.x, posPers.y, posPers.z] = pers1;
            pers1.Armes.Add(Instantiate(prefabArmes[0], pers.transform)); // a changer selon l'arme de départ
            pers1.EquipedArm = pers1.Armes[0].GetComponent<Arme>();
            
        }

        for (int i = 0; i < joueur2.GetComponent<Joueur>().NumeroPrefab.Count; i++)
        {
            GameObject pers = Instantiate(prefabPersos[joueur2.GetComponent<Joueur>().NumeroPrefab[i]], new Vector3(0, 3.5f, 10), Quaternion.identity);

            Personnage pers1 = pers.GetComponent<Personnage>();
            pers1.GameManager = this;

            joueur2.GetComponent<Joueur>().Personnages.Add(pers);
            pers1.Joueur = joueur2.GetComponent<Joueur>();
            pers1.Position = new Vector3Int(0, 4, 10);
            Vector3Int posPers = pers1.Position;
            this.grilleJeu.Grid[posPers.x, posPers.y, posPers.z] = pers1;
            pers1.Armes.Add(Instantiate(prefabArmes[0], pers.transform)); // a changer selon l'arme de départ
            pers1.EquipedArm = pers1.Armes[0].GetComponent<Arme>();

        }

        //pas vérifié
        foreach (GameObject monstre in listeMonstresNeutres)
        {
            LivingPlaceable monstre1 = monstre.GetComponent<Personnage>();
            monstre1.GameManager = this;
            Vector3Int posPers = monstre1.Position;
            this.grilleJeu.Grid[posPers.x, posPers.y, posPers.z] = monstre1;
            monstre1.Armes.Add(Instantiate(prefabArmes[0], monstre.transform)); // a changer selon l'arme de départ
            monstre1.EquipedArm = monstre1.Armes[0].GetComponent<Arme>();
        }
        grilleJeu.Gravite();
        //grilleJeu.SaveGridFile();
        grilleJeu.InitialiseExplored(false);
        
        Debug.Log(grilleJeu.IsGridAllExplored());
        grilleJeu.Explore(0, 0, 0);
        Debug.Log(grilleJeu.IsGridAllExplored());

       //La speed de turn est déterminée par l'élément le plus lent


        while (Winner == null)
        {
            this.GameEffectManager.ToBeTreated.AddRange(this.listeEffectsDebutTour);
            this.GameEffectManager.Solve();

            List<LivingPlaceable> liste = CreateTurnOrder();
            if(liste.Count != 0)
            { 
            foreach (LivingPlaceable placeable in liste)
            {
                    
                placeable.PmActuels = placeable.PmMax;
                placeable.NbFoisFiredThisTurn = 0;
                this.GameEffectManager.ToBeTreated.AddRange(placeable.OnDebutTour);
                this.GameEffectManager.Solve();
                    //Ici l'utilisateur a la main, et 30 secondes.

                    Color transp = new Color(0, 0, 0, 0);

                    if (placeable.Joueur != null)
                {
                        if (!placeable.EstMort)
                        {
                            clock.IsFinished = false;
                            clock.StartTimer(30f);


                            bool endPhase = false;
                            Vector3Int positiongo = new Vector3Int(placeable.Position.x, placeable.Position.y - 1, placeable.Position.z);

                            DistanceAndParent[,,] inPlace = grilleJeu.CanGo(placeable, placeable.PmMax, positiongo);
                            Debug.Log("C'est le debut lol!");
                            Vector3Int vecTest = new Vector3Int(-1, -1, -1);
                            while (placeable.NbFoisFiredThisTurn < 1 && placeable.PmActuels > 0 && !endPhase && !clock.IsFinished)
                            {
                                if (shotPlaceable != null && capacityinUse == 0 && shotPlaceable != placeable && placeable.CanHit(shotPlaceable).Count > 0)// si il se tire pas dessus et qu'il a bien sélectionné quelqu'un
                                {
                                    //piew piew
                                    Debug.Log("Piew piew");
                                    Vector3 thePlaceToShoot = placeable.ShootDamage(shotPlaceable); // pour les animations
                                }
                                else if (shotPlaceable != null && capacityinUse !=0 &&  placeable.Competences[capacityinUse].TourCooldownLeft == 0 &&
                                    placeable.Competences[capacityinUse].CompetenceType==CompetenceType.ONECLICKLIVING
                                    && placeable.Joueur.Ressource >= placeable.Competences[capacityinUse].Cost
                                    && shotPlaceable.GetType()==typeof(LivingPlaceable)
                                    && placeable.Competences[capacityinUse].condition())
                                {
                                    placeable.Competences[capacityinUse].Use();
                                    placeable.Joueur.Ressource -= placeable.Competences[capacityinUse].Cost;
                                }
                                else if (shotPlaceable ==placeable && capacityinUse != 0 && placeable.Competences[capacityinUse].TourCooldownLeft == 0 &&
                                    placeable.Competences[capacityinUse].CompetenceType == CompetenceType.SELFCOMPETENCE
                                    && placeable.Joueur.Ressource >= placeable.Competences[capacityinUse].Cost
                                    && placeable.Competences[capacityinUse].condition())
                                {
                                    placeable.Competences[capacityinUse].Use();
                                    placeable.Joueur.Ressource -= placeable.Competences[capacityinUse].Cost;
                                }
                                else if (shotPlaceable != null && capacityinUse != 0 && placeable.Competences[capacityinUse].TourCooldownLeft == 0 &&
                                  placeable.Competences[capacityinUse].CompetenceType == CompetenceType.ONECLICKPLACEABLE
                                  && placeable.Joueur.Ressource >= placeable.Competences[capacityinUse].Cost
                                  && shotPlaceable.GetType() == typeof(Placeable)
                                  && placeable.Competences[capacityinUse].condition())
                                {
                                    placeable.Competences[capacityinUse].Use();
                                    placeable.Joueur.Ressource -= placeable.Competences[capacityinUse].Cost;
                                }


                                else if(placeToGo != vecTest && inPlace[placeToGo.x, placeToGo.y, placeToGo.z].GetDistance()>0 && inPlace[placeToGo.x,placeToGo.y,placeToGo.z].GetDistance()<=placeable.PmActuels)
                                {
                                    List<Vector3> listAnimator = new List<Vector3>();
                                    DistanceAndParent parent=inPlace[placeToGo.x, placeToGo.y, placeToGo.z];
                                    listAnimator.Add(parent.Pos);
                                    while (parent.GetDistance()>1)
                                    {
                                        
                                        parent = parent.GetParent();
                                        listAnimator.Add(parent.Pos);
                                    }
                                    listAnimator.Add(placeable.Position-new Vector3Int(0,1,0)); //on veut l'emplacement dessous en fait
                                    StartCoroutine(ApplyMove(listAnimator,placeable));
                                    placeable.PmActuels -= listAnimator.Count - 1;

                                    //on actualise les positions
                                    grilleJeu.Grid[PlaceToGo.x, PlaceToGo.y+1, PlaceToGo.z] = placeable;   
                                    grilleJeu.Grid[placeable.Position.x, placeable.Position.y, placeable.Position.z] = null; // on est plus a l'emplacement précédent
                                    placeable.Position = PlaceToGo+new Vector3Int(0,1,0);

                                    for (int x = 0; x < inPlace.GetLength(0); x++)
                                    {
                                        for (int y = 0; y < inPlace.GetLength(1); y++)
                                        {
                                            for (int z = 0; z < inPlace.GetLength(2); z++)
                                            {

                                                if (inPlace[x, y, z].Color != transp)
                                                {
                                                    GrilleJeu.Grid[x, y, z].gameObject.GetComponent<Renderer>().material.color = inPlace[x, y, z].Color;
                                                }
                                            }
                                        }
                                    }
                                    inPlace = grilleJeu.CanGo(placeable, placeable.PmMax, placeToGo);
                                    this.PlaceToGo = vecTest;
                                    Debug.Log("runny run");
                                    
                                }
                                yield return null;

                            }
                            //on diminue de 1 le cooldown de la competence
                            foreach (Competence comp in placeable.Competences)
                            {
                                if(comp.TourCooldownLeft > 0)
                                {
                                    comp.TourCooldownLeft--;
                                }
                            }
                            shotPlaceable = null;
                            this.PlaceToGo = vecTest;

                         
                            for (int x = 0; x < inPlace.GetLength(0); x++)
                            {
                                for (int y = 0; y < inPlace.GetLength(1); y++)
                                {
                                    for (int z = 0; z < inPlace.GetLength(2); z++)
                                    {
                                        
                                        if (inPlace[x,y,z].Color != transp)
                                        {
                                            GrilleJeu.Grid[x, y, z].gameObject.GetComponent<Renderer>().material.color= inPlace[x, y, z].Color;
                                        }
                                    }
                                }
                            }
                                        Debug.Log("C'est la fin lol!");
                            //On applique les changements
                            //StopCoroutine(routine);
                        }
                        else
                        {
                            placeable.TourRestantsCimetiere--;
                        }

                    }

                    this.GameEffectManager.ToBeTreated.AddRange(this.listeEffectsFinTour);
                this.GameEffectManager.Solve();
            }
            }
            else
            {
                yield return null;
            }


        }
    }
    public IEnumerator ApplyMove(List<Vector3> path,Placeable placeable)
    {
        Vector3 delta =   placeable.transform.position - path[path.Count - 1]  ;
        bool isMoving = true;
        float speed = 1;
        // distance parcourue depuis le point de départ
        float travelDistance = 0;

        // position de départ
        Vector3 startPosition = path[path.Count-1] + delta;
        
        Vector3 targetDir = path[path.Count - 2] - placeable.transform.position;
        targetDir.y = 0;
        // boucle sur tous les point du chemin (c'est toujours mieux de stocker le total dans une variable locale)
        for (int i = path.Count - 2, count = path.Count, lastIndex = 0; i >= 0; i--)
        {
             //targetDir = path[i] - placeable.transform.position;
            // distance entre le point de départ et le point d'arrivée (node actuel, node suivant)
            float distance = Vector3.Distance(startPosition, path[i] + delta);

            // vecteur directeur entre ces deux points
            Vector3 direction = (path[i] + delta - startPosition).normalized;
           
            // boucle tant qu'on a pas encore dépassé la position du node suivant
            while (travelDistance < distance)
            {

                // on avance en fonction de la vitesse de déplacement et du temps écoulé
                travelDistance += (speed * Time.deltaTime);
               
                // si on a dépassé ou atteint la position du node d'arrivée
                if (travelDistance >= distance)
                {
                   
                    // si on est encore en chemin, 
                    if (i > lastIndex)
                    {
                        targetDir = path[i - 1] - placeable.transform.position;
                        targetDir.y = 0;
                        // on se positionne un peu plus loin sur le chemin 
                        // entre les deux nodes suivants, selon la distance parcourue au delà du node d'arrivée actuel
                        float distanceNext = Vector3.Distance(path[i - 1], path[i]);
                        
                        float ratio = (travelDistance - distance) / distanceNext;

                        // si le ratio est supérieur à 1, c'est que la distance parcourue
                        // est aussi supérieur à la distance entre les deux node suivants (donc on bouge vite)
                        // cette boucle va sauter tous les nodes qu'on est censé avoir parcourus en se déplaçant
                        // à grande vitesse
                        while (ratio > 1)
                        {
                            i--;
                            if (i == lastIndex)
                            {
                                // on est arrivé au dernier node du chemin
                                placeable.transform.position = path[i] + delta;
                                // sortie de la boucle
                                break;
                            }
                            else
                            {
                                travelDistance -= distance;
                                distance = distanceNext;
                                distanceNext = Vector3.Distance(path[i - 1], path[i]);
                                ratio = (travelDistance - distance) / distanceNext;
                            }
                        }

                        if (i == lastIndex)
                        {
                            // on est arrivé au dernier node du chemin dans le while précédent
                            break;
                        }
                        else
                        {
                            
                            transform.position = Vector3.Lerp(path[i], path[i - 1], ratio);
                        }

                    }
                    else
                    {
                        // on est arrivé au dernier node du chemin
                        placeable.transform.position = path[i] + delta ;
                        
                        break;
                    }
                }
                else
                {
                    // sinon on avance en direction du point d'arrivée
                    placeable.transform.position += direction * (speed * Time.deltaTime) ;
                }

                Vector3 vectorRotation = Vector3.RotateTowards(placeable.transform.forward, targetDir, 0.2f, 0);
                placeable.transform.rotation = Quaternion.LookRotation(vectorRotation);

                yield return null;
            }

            // on retire la distance qu'il y avait à parcourir entre les deux nodes précédents
            travelDistance -= distance;

            // mise à jour de la position de départ pour l'itération suivante
            startPosition = path[i] + delta;
        }
        isMoving = false;
    }

    public  List<LivingPlaceable> CreateTurnOrder(){
        int maxSpeedStack = 0;
        List<LivingPlaceable> liste = new List<LivingPlaceable>();

        foreach (GameObject pers1b in joueur1.GetComponent<Joueur>().Personnages)
        {
            Personnage pers1 = pers1b.GetComponent<Personnage>();
            pers1.SpeedStack += 1 / pers1.Speed;

            liste.Add(pers1);
            if (maxSpeedStack< pers1.SpeedStack)
            {
                maxSpeedStack = pers1.SpeedStack;
            }

        }
        foreach (GameObject pers2b in joueur2.GetComponent<Joueur>().Personnages)
        {
            Personnage pers2 = pers2b.GetComponent<Personnage>();
            liste.Add(pers2);
            if (maxSpeedStack < pers2.SpeedStack)
            {
                maxSpeedStack = pers2.SpeedStack;
            }

        }
        foreach (GameObject monstre2 in listeMonstresNeutres)
        {
            LivingPlaceable monstre = monstre2.GetComponent<LivingPlaceable>();

            monstre.SpeedStack += 1 / monstre.Speed;
            liste.Add(monstre);
            if (maxSpeedStack < monstre.SpeedStack)
            {
                maxSpeedStack = monstre.SpeedStack;
            }

        }

        foreach (GameObject pers1b in joueur1.GetComponent<Joueur>().Personnages)
        {
            Personnage pers1 = pers1b.GetComponent<Personnage>();

            if (pers1.SpeedStack<maxSpeedStack + 1/pers1.Speed) // alors on peut le rerajouter
            {
                pers1.SpeedStack += 1 / pers1.Speed;
                liste.Add(pers1);
            }

        }

        foreach (GameObject pers2b in joueur2.GetComponent<Joueur>().Personnages)
        {
            Personnage pers2 = pers2b.GetComponent<Personnage>();
            if (pers2.SpeedStack < maxSpeedStack + 1 / pers2.Speed) // alors on peut le rerajouter
            {
                pers2.SpeedStack += 1 / pers2.Speed;
                liste.Add(pers2);
            }
        }
        foreach (GameObject monstre2 in listeMonstresNeutres)
        {
            LivingPlaceable monstre = monstre2.GetComponent<LivingPlaceable>();
            if (monstre.SpeedStack < maxSpeedStack + 1 / monstre.Speed) // alors on peut le rerajouter
            {
                monstre.SpeedStack += 1 / monstre.Speed;
                liste.Add(monstre);
            }
        }


        liste.Sort((x, y) => x.SpeedStack - y.SpeedStack);

        return liste;
     }
    public void Shootable(LivingPlaceable shooter)
    {
        Vector3 shootpos = shooter.position + shooter.ShootPosition;
        foreach (GameObject pers1b in joueur1.GetComponent<Joueur>().Personnages)
        {

            Personnage pers1 = pers1b.GetComponent<Personnage>();

           if(shooter.CanHit(pers1).Count>0)
            {
                
                //typiquement, on change sa couleur
            }

        }

        foreach (GameObject pers2b in joueur2.GetComponent<Joueur>().Personnages)
        {
            Personnage pers2 = pers2b.GetComponent<Personnage>();

            if (shooter.CanHit(pers2).Count > 0)
            {

                //typiquement, on change sa couleur
            }


        }
        foreach (GameObject monstre2 in listeMonstresNeutres)
        {
            LivingPlaceable monstre = monstre2.GetComponent<LivingPlaceable>();


            if (shooter.CanHit(monstre).Count > 0)
            {

                //typiquement, on change sa couleur
            }

        }
        }
    

            /*
            private int MinSpeedPersos()
            {
               int minSpeed = 1000000000;
                foreach (Personnage pers1 in joueur1.Personnages)
                {
                    if(minSpeed>pers1.Speed)
                    {
                        minSpeed = pers1.Speed;
                    }

                }
                foreach (Personnage pers2 in joueur2.Personnages)
                {
                    if (minSpeed > pers2.Speed)
                    {
                        minSpeed = pers2.Speed;
                    }

                }
                return minSpeed;
            }
            */
            // Update is called once per frame
            void Update () {
        

    }
}
