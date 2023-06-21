using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor.AI;

public class SimulationManager : MonoBehaviour
{
    //The physical area this simulation will run in
    [Header("Landscape")]
    [SerializeField] private GameObject floorArea;
    private MeshRenderer floorAreaMeshRenderer;
    private int floorMinX;
    private int floorMaxX;
    private int floorMinZ;
    private int floorMaxZ;
    private float floorHeight;

    //information about the caves
    [Header("")]
    [SerializeField] private GameObject caveObject;
    [SerializeField] private GameObject caveHousing;
    [SerializeField] [Range(3, 15)] private int numberOfCaves = 10;
    private int caveWidth = 2;
    private int caveLength = 2;
    private float caveSpawnHeight = 1.125f;
    private Vector2 caveOffset = new Vector2(0.5f, 0.5f);
    private bool generatedCaves = false;

    //information about the water
    [Header("")]
    [SerializeField] private GameObject waterObject;
    [SerializeField] private GameObject waterHousing;
    [SerializeField] [Range(1, 95)] private int waterCoverage = 20;
    private float waterSurfaceHeight = 0.5f;
    private float[,] perlinNoiseHeights;
    private bool generatedWaterMap = false;

    //information about miscellaneous obstacles (trees, rocks etc)
    [Header("")]
    [SerializeField] private List<GameObject> miscellaneousObjects;
    [SerializeField] private GameObject miscellaneousHousing;
    [SerializeField] [Range(0, 100)] private int numberOfMiscellaneousObjects = 40;
    [SerializeField] private List<Vector3> miscellaneousXZScaleDimensions;
    private float miscellaneousSpawnHeight = 1.25f;
    private bool generatedMiscellaneousObjects = false;


    private bool finishedSetup = false;
    private bool allLandscapeSpawned = false;
    private bool allAnimalsSpawned = false;
    private bool baseFoodSpawned = false;
    private List<Vector2> occupiedSpawnPositions = new List<Vector2>();


    [Header("\n\nAnimals")]
    //information about top predator
    [SerializeField] public GameObject cubedTopPredatorPrefab;
    [SerializeField] private GameObject optimisedTopPredatorPrefab;
    [SerializeField] private GameObject topPredatorHousing;
    [SerializeField] private int maxStartNumOfTopPredators = 3;
    private int numOfTopPredators;

    [Header("")]
    //information about middle predator
    [SerializeField] public GameObject cubedMiddlePredatorPrefab;
    [SerializeField] private GameObject optimisedMiddlePredatorPrefab;
    [SerializeField] private GameObject middlePredatorHousing;
    [SerializeField] private int maxStartNumOfMiddlePredators = 9;
    private int numOfMiddlePredators;

    [Header("")]
    //information about prey
    [SerializeField] public GameObject cubedPreyPrefab;
    [SerializeField] private GameObject optimisedPreyPrefab;
    [SerializeField] private GameObject preyHousing;
    [SerializeField] private int maxStartNumOfPrey = 40;
    private int numOfPrey;

    [Header("")]
    //hurt effect prefabs
    [SerializeField] public GameObject smallAnimalHurtEffectPrefab;
    [SerializeField] public GameObject largeAnimalHurtEffectPrefab;
    
    [Header("\n\nFood")]
    //information about food
    [SerializeField] private GameObject[] foodPrefabs;
    [SerializeField] private GameObject foodHousing;
    [SerializeField] [Range(25, 200)] private int numberOfBaseFood = 100;
    private List<GameObject> meatFoods = new List<GameObject>();
    private List<GameObject> vegatarianFoods = new List<GameObject>();
    private List<Vector2> baseFoodLocations = new List<Vector2>();

    //lists of object positions
    public List<GameObject> baseFood = new List<GameObject>();
    public List<GameObject> spawnedMeat = new List<GameObject>();
    public List<AnimalInfo> animalsStatus = new List<AnimalInfo>();
    public List<CaveInfo> caveMap = new List<CaveInfo>();
    public List<Vector2> waterMap = new List<Vector2>();

    //vitals stats and names
    public Vitals vitals = new Vitals();

    //pausing the simulation
    [SerializeField] private bool pause = false;
    private bool currentPause = false;

    private void Awake()
    {
        //set target framerate
        Application.targetFrameRate = 144;
        //get the floor object's mesh renderer
        floorAreaMeshRenderer = floorArea.GetComponent<MeshRenderer>();
        //clear the navigation mesh
        UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
        //initialize the vitals information
        vitals.Initialize();
    }

    private void Start()
    {
        //get world space area the simulation will generate and run in
        floorMinX = 1 + Mathf.CeilToInt(floorArea.transform.position.x - floorAreaMeshRenderer.bounds.extents.x);
        floorMaxX = Mathf.FloorToInt(floorArea.transform.position.x + floorAreaMeshRenderer.bounds.extents.x);
        floorMinZ = 1 + Mathf.CeilToInt(floorArea.transform.position.z - floorAreaMeshRenderer.bounds.extents.z);
        floorMaxZ = Mathf.FloorToInt(floorArea.transform.position.z + floorAreaMeshRenderer.bounds.extents.z);

        floorHeight = floorArea.transform.position.y + floorAreaMeshRenderer.bounds.extents.y;

        foreach (GameObject obj in foodPrefabs)
        {
            //add food prefab objects to correct list
            Food.FoodTypes foodType = obj.GetComponent<Food>().GetFoodType();
            if (foodType == Food.FoodTypes.Meat) meatFoods.Add(obj);
            else if (foodType == Food.FoodTypes.Vegetarian) vegatarianFoods.Add(obj);
        }
    }

    private void Update()
    {
        //scene setup
        if (!finishedSetup)
        {
            Debug.Log("running setup");
            SetupScene();
            return;
        }

        //check if we should pause the simulation
        if (pause != currentPause)
        {
            currentPause = pause;
            //pause day night cycle
            FindObjectOfType<UIInfo>().stopDayNightCycle = pause;
            //pause all animal's update cycle
            foreach (AnimalInfo info in animalsStatus)
            {
                info.gameObj.GetComponent<Animal>().SetUpdateAnimal(!pause);
            }
        }


        Debug.Log("running AI code");
    }

    public void RemoveAnimal(int index)
    {
        //remove animal from animal list at index
        animalsStatus.RemoveAt(index);

        //for every animal at and after the removed animal's index
        for (int i = index; i < animalsStatus.Count; ++i)
        {
            //set new index in this list
            animalsStatus[i].animalIndex = i;
            //update the animal's index in animal class
            animalsStatus[i].gameObj.GetComponent<Animal>().SetAnimalIndex(i);
        }
    }

    private void SetupScene()
    {
        //each function that runs to setup the scene
        if (!allLandscapeSpawned) SpawnLandscape();
        else if (!allAnimalsSpawned) SpawnAnimals();
        else if (!baseFoodSpawned) SpawnFood();
        else finishedSetup = true;

        if (finishedSetup)
        {
            Debug.Log("all animals spawned! (1/2) (1/2)");
            Debug.Log("all food spawned! (1/2) (2/2)");
            Debug.Log("Scene is setup! (2/2)");
        }
    }

    private void SpawnLandscape()
    {
        //each function that creates landscape objects
        if (!generatedWaterMap) CreateWater();
        if (!generatedCaves) CreateCaves();
        if (!generatedMiscellaneousObjects) CreateMiscellaneousObjects();

        //clear existing navmesh
        UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
        //build new navmesh with the new scene layout
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();

        allLandscapeSpawned = true;
        Debug.Log("all landscape created! (1/2)");
    }

    private void CreateWater()
    {
        //water can spawn anywhere on the map
        int boundarySeperation = 0;
        //get x and z length of the potential spawning area
        int xLength = Mathf.Abs(floorMinX + boundarySeperation) + floorMaxX - boundarySeperation;
        int zLength = Mathf.Abs(floorMinZ + boundarySeperation) + floorMaxZ - boundarySeperation;
        //empty list of perlin noise values using x and z length
        perlinNoiseHeights = new float[xLength, zLength];
        //add randomness to xoffset using UTC seconds and hours
        int xOffset = Random.Range(1, 100 * System.DateTime.UtcNow.Second * System.DateTime.UtcNow.Hour);
        //add randomness to yoffset using UTC minutes and milliseconds
        int yOffset = Random.Range(1, 100 * System.DateTime.UtcNow.Minute * System.DateTime.UtcNow.Millisecond);

        List<float> ordererdHeights = new List<float>();

        for (int i = 0; i < xLength; ++i)
        {
            for (int j = 0; j < zLength; ++j)
            {
                //get x position using area position and offset
                float xPos = (float)i / (float)floorMaxX + xOffset;
                //get y position using area position and offset
                float yPos = (float)j / (float)floorMaxZ + yOffset;
                //get perlin noise map for simulation world space area
                float weight = Mathf.PerlinNoise(xPos, yPos);

                //get weight at this coordinate
                perlinNoiseHeights[i, j] = weight;

                //place this weight in the correct place in ordered list
                if (ordererdHeights.Count == 0) ordererdHeights.Add(weight);
                else if (ordererdHeights[ordererdHeights.Count - 1] > weight) ordererdHeights.Insert(ordererdHeights.Count - 1, weight);
                else
                {
                    for (int k = 0; k < ordererdHeights.Count; ++k)
                    {
                        if (weight > ordererdHeights[k])
                        {
                            ordererdHeights.Insert(k, weight);
                            break;
                        }
                    }
                }
            }
        }

        List<Vector2> waterLocations = new List<Vector2>();
        //get required height value required to create water at this position
        float requiredWeight = ordererdHeights[Mathf.FloorToInt((float)ordererdHeights.Count * ((float)waterCoverage / 100))];
        for (int i = 0; i < xLength; ++i)
        {
            for (int j = 0; j < zLength; ++j)
            {
                //if this height value is greater than the required value
                if (perlinNoiseHeights[i, j] > requiredWeight)
                {
                    //get the x position for the list
                    float xPos = i + floorMinX + boundarySeperation;
                    //get the z position for the list
                    float zPos = j + floorMinZ + boundarySeperation;
                    //create vector2 coordinate using x and z position
                    Vector2 v2NewPos = new Vector2(xPos, zPos);

                    //add position to occupied list
                    if (!occupiedSpawnPositions.Contains(v2NewPos)) AddOccupiedPosition(v2NewPos);
                    //add position to water map
                    if (!waterLocations.Contains(v2NewPos)) waterLocations.Add(v2NewPos);

                    //instantiate the water prefab
                    GameObject obj = Instantiate(waterObject, waterHousing.transform);
                    //set the position of the object to the correct place in world space
                    obj.transform.position = new Vector3(xPos, waterSurfaceHeight, zPos);
                }
            }
        }

        foreach (Vector2 v2Pos in waterLocations)
        {
            bool add = false;
            //only add water position to list if it's on the outside of the body of water, as animals will only need to search for water at the edge of the group of water objects
            if (!waterLocations.Contains(new Vector2(v2Pos.x + 1, v2Pos.y))) add = true;
            else if (!waterLocations.Contains(new Vector2(v2Pos.x, v2Pos.y + 1))) add = true;
            else if (!waterLocations.Contains(new Vector2(v2Pos.x - 1, v2Pos.y))) add = true;
            else if (!waterLocations.Contains(new Vector2(v2Pos.x, v2Pos.y - 1))) add = true;
            //add to water map list
            if (add) waterMap.Add(v2Pos);
        }

        Debug.Log("Created water! (0/2) (1/3)");
        generatedWaterMap = true;
    }

    private void CreateCaves()
    {
        for (int i = 0; i < numberOfCaves; ++i)
        {
            //get x and z position to try and create a cave
            float xPos = Mathf.Floor(Random.Range(floorMinX + caveWidth, floorMaxX - caveWidth));
            float zPos = Mathf.Floor(Random.Range(floorMinZ + caveLength, floorMaxZ - caveLength));
            //values close to the centre of the map, where the cave should not spawn
            float centralExclusionDistance = Mathf.Floor((Mathf.Abs(floorMinX) + floorMaxX) / 6) + caveWidth;
            //if the selected position is too close to the centre of the map
            if (Mathf.Abs(xPos) < centralExclusionDistance && Mathf.Abs(zPos) < centralExclusionDistance)
            {
                //decrement the incrementor, and start over
                --i;
                continue;
            }
            else if (occupiedSpawnPositions.Contains(new Vector2(xPos, zPos)) ||
                occupiedSpawnPositions.Contains(new Vector2(xPos + 1, zPos)) ||
                occupiedSpawnPositions.Contains(new Vector2(xPos + 1, zPos + 1)) ||
                occupiedSpawnPositions.Contains(new Vector2(xPos, zPos + 1)) ||

                occupiedSpawnPositions.Contains(new Vector2(xPos + 2, zPos)) ||
                occupiedSpawnPositions.Contains(new Vector2(xPos + 2, zPos + 1)) ||
                occupiedSpawnPositions.Contains(new Vector2(xPos + 1, zPos + 2)) ||
                occupiedSpawnPositions.Contains(new Vector2(xPos, zPos + 2)) ||

                occupiedSpawnPositions.Contains(new Vector2(xPos + 1, zPos - 1)) ||
                occupiedSpawnPositions.Contains(new Vector2(xPos, zPos - 1)) ||
                occupiedSpawnPositions.Contains(new Vector2(xPos - 1, zPos)) ||
                occupiedSpawnPositions.Contains(new Vector2(xPos - 1, zPos + 1)))
            {
                //if there is already a cave occupying the new position for this cave
                //decrement the incrementor, and start over
                --i;
                continue;
            }

            //middle 4 map coordinates of the new cave
            occupiedSpawnPositions.Add(new Vector2(xPos, zPos));
            occupiedSpawnPositions.Add(new Vector2(xPos + 1, zPos));
            occupiedSpawnPositions.Add(new Vector2(xPos + 1, zPos + 1));
            occupiedSpawnPositions.Add(new Vector2(xPos, zPos + 1));
            //coordinates above and to the right of the cave
            occupiedSpawnPositions.Add(new Vector2(xPos + 2, zPos));
            occupiedSpawnPositions.Add(new Vector2(xPos + 2, zPos + 1));
            occupiedSpawnPositions.Add(new Vector2(xPos + 1, zPos + 2));
            occupiedSpawnPositions.Add(new Vector2(xPos, zPos + 2));
            //coordinates below and to the left of the cave
            occupiedSpawnPositions.Add(new Vector2(xPos + 1, zPos - 1));
            occupiedSpawnPositions.Add(new Vector2(xPos, zPos - 1));
            occupiedSpawnPositions.Add(new Vector2(xPos - 1, zPos));
            occupiedSpawnPositions.Add(new Vector2(xPos - 1, zPos + 1));

            float rotation = 0.0f;
            //use x and z position to calculate the rotation the cave should be such that the entrance faces the centre of the map
            if (Mathf.Abs(xPos) >= Mathf.Abs(zPos))
            {
                if (xPos >= 0 && zPos >= 0) rotation = 270.0f;
                else if (xPos >= 0 && zPos < 0) rotation = 270.0f;
                else if (xPos < 0 && zPos >= 0) rotation = 90.0f;
                else if (xPos < 0 && zPos < 0) rotation = 90.0f;
            }
            else
            {
                if (xPos >= 0 && zPos >= 0) rotation = 180.0f;
                else if (xPos >= 0 && zPos < 0) rotation = 0.0f;
                else if (xPos < 0 && zPos >= 0) rotation = 180.0f;
                else if (xPos < 0 && zPos < 0) rotation = 0.0f;
            }

            //instantiate the cave object
            GameObject obj = Instantiate(caveObject, caveHousing.transform);
            //set the position
            obj.transform.position = new Vector3(xPos + caveOffset.x, caveSpawnHeight, zPos + caveOffset.y);
            //set the rotation
            obj.transform.eulerAngles = new Vector3(0, rotation, 0);

            //add the new cave to the cave map list
            caveMap.Add(CaveInfo.AddCaveInfo(obj, caveMap.Count, 0, 10));
        }

        Debug.Log("Created Caves! (0/2) (2/3)");
        generatedCaves = true;
    }

    private void CreateMiscellaneousObjects()
    {
        for (int i = 0; i < numberOfMiscellaneousObjects; ++i)
        {
            //get x and z position to try and create an object
            float xPos = Mathf.Floor(Random.Range(floorMinX + 2, floorMaxX - 2));
            float zPos = Mathf.Floor(Random.Range(floorMinZ + 2, floorMaxZ - 2));
            //create vector2 using generated x and z position
            Vector2 v2NewPos = new Vector2(xPos, zPos);

            bool taken = false;
            //check if that position is already occupied
            if (occupiedSpawnPositions.Contains(v2NewPos)) taken = true;

            //get random number to pick an object from the miscellaneous object list
            int objectIndex = Random.Range(0, miscellaneousObjects.Count);

            //if the picked object has a scale of 2 (taking up more space)
            if (miscellaneousXZScaleDimensions[objectIndex].z == 2)
            {
                //also check if each additional space is already occupied
                v2NewPos.x += 1;
                if (occupiedSpawnPositions.Contains(v2NewPos)) taken = true;
                v2NewPos.y += 1;
                if (occupiedSpawnPositions.Contains(v2NewPos)) taken = true;
                v2NewPos.x -= 1;
                if (occupiedSpawnPositions.Contains(v2NewPos)) taken = true;
                v2NewPos.y -= 1;
            }

            //if the position is occupied
            if (taken)
            {
                //decrement incrementor and continue onto next object
                --i;
                continue;
            }

            //add the position to the occupied positions list
            occupiedSpawnPositions.Add(v2NewPos);
            //if the picked object has a scale of 2 (taking up more space)
            if (miscellaneousXZScaleDimensions[objectIndex].z == 2)
            {
                //add each additional position to the occupied positions list
                v2NewPos.x += 1;
                occupiedSpawnPositions.Add(v2NewPos);
                v2NewPos.y += 1;
                occupiedSpawnPositions.Add(v2NewPos);
                v2NewPos.x -= 1;
                occupiedSpawnPositions.Add(v2NewPos);
                v2NewPos.y -= 1;
            }

            //calculate the world position to instantiate the object at
            Vector3 newPosition = Vector3.zero;
            newPosition.x = v2NewPos.x + miscellaneousXZScaleDimensions[objectIndex].x;
            newPosition.y = miscellaneousSpawnHeight;
            newPosition.z = v2NewPos.y + miscellaneousXZScaleDimensions[objectIndex].y;
            //get a random rotation to instantiate the object with
            Quaternion newRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            
            //instantiate the miscellaneous object prefab using the new position and new rotation
            Instantiate(miscellaneousObjects[objectIndex], newPosition, newRotation, miscellaneousHousing.transform);
        }

        Debug.Log("Created Miscellaneous Objects! (0/2) (3/3)");
        generatedMiscellaneousObjects = true;
    }

    private void SpawnAnimals()
    {
        //get x and z position to try and create an object
        float xPos = Mathf.Floor(Random.Range(floorMinX, floorMaxX));
        float zPos = Mathf.Floor(Random.Range(floorMinZ, floorMaxZ));
        //get the y position the animal will be at
        float yPos = floorHeight + optimisedPreyPrefab.GetComponent<NavMeshAgent>().height;

        //create vector3 using the x y and z positions
        Vector3 newPos = new Vector3(xPos, yPos, zPos);

        if (numOfTopPredators < maxStartNumOfTopPredators)
        {
            //create a top predator animal using its prefab, its animal type and new position
            if (CreateAnimal(optimisedTopPredatorPrefab, topPredatorHousing, Animal.AnimalTypes.Wolf, newPos)) numOfTopPredators++;
        }
        else if (numOfMiddlePredators < maxStartNumOfMiddlePredators)
        {
            //create a middle predator animal using its prefab, its animal type and new position
            if (CreateAnimal(optimisedMiddlePredatorPrefab, middlePredatorHousing, Animal.AnimalTypes.Fox, newPos)) numOfMiddlePredators++;
        }
        else if (numOfPrey < maxStartNumOfPrey)
        {
            //create a prey animal using its prefab, its animal type and new position
            if (CreateAnimal(optimisedPreyPrefab, preyHousing, Animal.AnimalTypes.Rabbit, newPos)) numOfPrey++;
        }
        else
        {
            //set flag to true as all top, middle and prey animals have been created
            allAnimalsSpawned = true;
        }
    }

    private void SpawnFood()
    {
        //create required amount of vegaterian food 
        CreateFood(Food.FoodTypes.Vegetarian, numberOfBaseFood, Vector3.zero, Vector3.zero);
        baseFoodSpawned = true;
    }

    private void AddOccupiedPosition(Vector2 position)
    {
        //add middle position to occupied positions list
        occupiedSpawnPositions.Add(position);

        //add top and bottom position to occupied positions list
        position.x += 1;
        if (!occupiedSpawnPositions.Contains(position)) occupiedSpawnPositions.Add(position);
        position.x -= 2;
        if (!occupiedSpawnPositions.Contains(position)) occupiedSpawnPositions.Add(position);
        position.x += 1;

        //add left and right position to occupied positions list
        position.y += 1;
        if (!occupiedSpawnPositions.Contains(position)) occupiedSpawnPositions.Add(position);
        position.y -= 2;
        if (!occupiedSpawnPositions.Contains(position)) occupiedSpawnPositions.Add(position);
        position.y += 1;
    }

    public void CreateFood(Food.FoodTypes foodType, int foodAmount, Vector3 location, Vector3 force)
    {
        List<Vector3> usedSpawnPositions = new List<Vector3>();

        for (int i = 0; i < foodAmount; ++i)
        {
            if (foodType == Food.FoodTypes.Meat)
            {
                //create a random meat type food object
                GameObject obj = Instantiate(meatFoods[Random.Range(0, meatFoods.Count)]);
                //set the object tag
                obj.tag = "Food";
                //add a rigidbody component
                Rigidbody rb = obj.AddComponent<Rigidbody>();
                //add the physics force
                rb.AddForce(force);
                //set mass, drag and angular drag
                rb.mass = 15.0f;
                rb.drag = 5.0f;
                rb.angularDrag = 5.0f;

                //flags and values
                bool taken = true;
                int basicInfiniteLoopCheck = 50;
                int currentIter = 0;
                Vector3 newPosition = Vector3.zero;

                while (taken)
                {
                    //get random new position within this tile
                    newPosition = new Vector3(Random.Range(-4, 4) * 0.25f, 0.75f, Random.Range(-4, 4) * 0.25f);
                    //if new position is unoccupied, set taken flag to false
                    if (!usedSpawnPositions.Contains(newPosition)) taken = false;
                    //increment current iteration
                    currentIter++;
                    if (currentIter >= basicInfiniteLoopCheck)
                    {
                        //set taken flag to false and continue onto next food object to create
                        taken = false;
                        continue;
                    }
                }

                //add position to used spawn positions list
                usedSpawnPositions.Add(newPosition);

                //get world space position
                newPosition += location;
                //set position
                obj.transform.position = newPosition;
                //get random rotation on y axis
                Vector3 newRotation = new Vector3(0, Random.Range(0, 360), 0);
                //set new rotation
                obj.transform.eulerAngles = newRotation;
                //add object to list of spawned meat food types
                spawnedMeat.Add(obj);
            }
            else if (foodType == Food.FoodTypes.Vegetarian)
            {
                Vector3 newPosition = location;

                if (location == Vector3.zero)
                {
                    //get new position to create food type at
                    float xPos = Mathf.Floor(Random.Range(floorMinX + 1, floorMaxX - 2));
                    float zPos = Mathf.Floor(Random.Range(floorMinZ + 1, floorMaxZ - 2));
                    float yPos = 1;

                    //if the new position is occupied
                    if (occupiedSpawnPositions.Contains(new Vector2(xPos, zPos)))
                    {
                        //decrement the incrementor and continue onto next food object to create
                        --i;
                        continue;
                    }

                    //get vector3 of new position using x y and z position
                    newPosition = new Vector3(xPos, yPos, zPos);
                }
                else if (occupiedSpawnPositions.Contains(new Vector2(location.x, location.z)))
                {
                    //decrement the incrementor and continue onto next food object to create as position is occupied
                    --i;
                    continue;
                }

                //add randomness to the position this food object will spawn within this tile
                newPosition.x += Random.Range(-1, 2) * 0.3f;
                newPosition.z += Random.Range(-1, 2) * 0.3f;

                //if this position is occupied
                if (baseFoodLocations.Contains(new Vector2(newPosition.x, newPosition.z)))
                {
                    //decrement incrementor and continue onto next food object to create
                    --i;
                    continue;
                }

                //add the new position to the list of base food positions
                baseFoodLocations.Add(new Vector2(newPosition.x, newPosition.z));

                //get a random y axis rotation
                Quaternion newRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                //instantiate the food type object prefab using the new position and new rotation
                GameObject obj = Instantiate(vegatarianFoods[Random.Range(0, vegatarianFoods.Count)], newPosition, newRotation, foodHousing.transform);
                //set the object tag
                obj.tag = "Food";
                //add food to the list of base food objects
                baseFood.Add(obj);
            }
        }
    }

    public void CreateAnimal(GameObject animalPrefab, GameObject animalHousing, Animal.AnimalTypes animalType)
    {
        //get new position to instantiate the animal at
        float xPos = Mathf.Floor(Random.Range(floorMinX, floorMaxX));
        float zPos = Mathf.Floor(Random.Range(floorMinZ, floorMaxZ));
        float yPos = floorHeight + optimisedPreyPrefab.GetComponent<NavMeshAgent>().height;

        //get vector3 position using x y and z position
        Vector3 newPosition = new Vector3(xPos, yPos, zPos);
        //get a random y axis rotation
        Quaternion newRotation = Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0));
        //instantiate the animal prefab using the new position and new rotation
        GameObject obj = Instantiate(animalPrefab, newPosition, newRotation, animalHousing.transform);
        //add the new animal to the list of animals
        animalsStatus.Add(AnimalInfo.AddAnimalInfo(obj, animalsStatus.Count, animalType));
        //set the index of the animal within the simulation
        obj.GetComponent<Animal>().SetAnimalIndex(animalsStatus.Count - 1);

        //call setup on the animal type
        if ((int)animalType == 1) obj.GetComponent<Wolf>().Setup();
        else if ((int)animalType == 2) obj.GetComponent<Fox>().Setup();
        else if ((int)animalType == 3) obj.GetComponent<Rabbit>().Setup();

    }

    public bool CreateAnimal(GameObject animalPrefab, GameObject animalHousing, Animal.AnimalTypes animalType, Vector3 position)
    {
        //get vector3 position of the requested location to instantiate the animal at
        Vector2 vec2Position = new Vector2(position.x, position.z);

        //if the requested position is already occupied, return false
        if (occupiedSpawnPositions.Contains(vec2Position)) return false;

        //add the new position to the list of occupied positions
        AddOccupiedPosition(vec2Position);

        //get a random y axis rotation
        Quaternion newRotation = Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0));
        //instantiate the animal prefab using the new position and new rotation
        GameObject obj = Instantiate(animalPrefab, position, newRotation, animalHousing.transform);

        //add the new animal to the list of animals
        animalsStatus.Add(AnimalInfo.AddAnimalInfo(obj, animalsStatus.Count, animalType));
        //set the index of the animal within the simulation
        obj.GetComponent<Animal>().SetAnimalIndex(animalsStatus.Count - 1);

        //call setup on the animal type
        if ((int)animalType == 1) obj.GetComponent<Wolf>().Setup();
        else if ((int)animalType == 2) obj.GetComponent<Fox>().Setup();
        else if ((int)animalType == 3) obj.GetComponent<Rabbit>().Setup();

        //return true as animal was created and setup successfully
        return true;
    }

    public void EvictCave(int caveID)
    {
        foreach (CaveInfo cave in caveMap)
        {
            if (cave.caveIndex == caveID)
            {
                foreach (AnimalInfo animal in animalsStatus)
                {
                    if (cave.animalsInside.Contains(animal.animalIndex))
                    {
                        //get the animal inside this cave to leave
                        animal.gameObj.GetComponent<Animal>().ExitCave();
                        //remvoe the animals ID from the list of animal IDs inside this cave
                        cave.animalsInside.Remove(animal.animalIndex);
                    }
                }
                
                return;
            }
        }
    }

    public void CreateAnimalAsChild(Animal.AnimalTypes animalType, Vector3 position, Quaternion rotation, int parentID)
    {
        GameObject obj = null;
        //instantiate animal using the animal type, position and rotation
        if ((int)animalType == 1) obj = Instantiate(optimisedTopPredatorPrefab, position, rotation, topPredatorHousing.transform);
        else if ((int)animalType == 2) obj = Instantiate(optimisedMiddlePredatorPrefab, position, rotation, middlePredatorHousing.transform);
        else if ((int)animalType == 3) obj = Instantiate(optimisedPreyPrefab, position, rotation, preyHousing.transform);

        //add the new animal to the list of animals
        animalsStatus.Add(AnimalInfo.AddAnimalInfo(obj, animalsStatus.Count, animalType));
        //set the index of the animal within the simulation
        obj.GetComponent<Animal>().SetAnimalIndex(animalsStatus.Count - 1);

        //get the parent's relevant values for use in genetics algorithm
        int parentBaseAmount = animalsStatus[parentID].gameObj.GetComponent<Animal>().ATTRIBUTESAMOUNT;
        int parentMaxBaseAmount = animalsStatus[parentID].gameObj.GetComponent<Animal>().MAXBASEATTRIBUTEAMOUNT;
        //get the range of attribute points the child can have
        int range = parentBaseAmount / 4;

        List<int> triangularDistribution = new List<int>();
        //for each point in the distribution range
        for (int i = 0; i < range; ++i)
        {
            //for the density value at this point
            for (int j = i; j < range; ++j)
            {
                //add an item to the list above and below the centre point
                if (i + parentBaseAmount <= parentMaxBaseAmount) triangularDistribution.Add(i + parentBaseAmount);
                if (i != 0) triangularDistribution.Add(-i + parentBaseAmount);
            }
        }

        //get a random index to select the attributes amount
        int newValueIndex = Random.Range(0, triangularDistribution.Count);
        //get the attributes amount using the generated index
        int attributeAmount = triangularDistribution[newValueIndex];

        //how many changes we have to make to the attributes
        int changedAmount = attributeAmount - parentBaseAmount;
        //the maximum value a single attribute can have for this animal type
        int maxAttributeValue = animalsStatus[parentID].gameObj.GetComponent<Animal>().MAXATTRIBUTEVALUE;

        //get the attribute values of the parent and set them as the childs
        int strength = animalsStatus[parentID].gameObj.GetComponent<Animal>().STRENGTH;
        int vitality = animalsStatus[parentID].gameObj.GetComponent<Animal>().VITALITY;
        int speed = animalsStatus[parentID].gameObj.GetComponent<Animal>().SPEED;
        int eyeStrength = animalsStatus[parentID].gameObj.GetComponent<Animal>().EYESTRENGTH;
        int nightSurvivability = animalsStatus[parentID].gameObj.GetComponent<Animal>().NIGHTSURVIVABILITY;

        while (changedAmount != 0)
        {
            //get changed amount; +1 or -1 to an attribute
            int changeValue = changedAmount / Mathf.Abs(changedAmount);
            //get a random attribute to change
            int newIndex = Random.Range(0, 5);
            //apply change to counter
            changedAmount -= changeValue;

            //apply attribute change to selected attribute
            switch (newIndex)
            {
                case 0:
                    strength += changeValue;
                    if (strength > maxAttributeValue)
                    {
                        strength--;
                        changedAmount -= changeValue;
                    }
                    else if (strength == 0)
                    {
                        strength++;
                        changedAmount += changeValue;
                    }
                    break;

                case 1:
                    vitality += changeValue;
                    if (vitality > maxAttributeValue)
                    {
                        vitality--;
                        changedAmount -= changeValue;
                    }
                    else if (vitality == 0)
                    {
                        vitality++;
                        changedAmount += changeValue;
                    }
                    break;

                case 2:
                    speed += changeValue;
                    if (speed > maxAttributeValue)
                    {
                        speed--;
                        changedAmount -= changeValue;
                    }
                    else if (speed == 0)
                    {
                        speed++;
                        changedAmount += changeValue;
                    }
                    break;

                case 3:
                    eyeStrength += changeValue;
                    if (eyeStrength > maxAttributeValue)
                    {
                        eyeStrength--;
                        changedAmount -= changeValue;
                    }
                    else if (eyeStrength == 0)
                    {
                        eyeStrength++;
                        changedAmount += changeValue;
                    }
                    break;

                case 4:
                    nightSurvivability += changeValue;
                    if (nightSurvivability > maxAttributeValue)
                    {
                        nightSurvivability--;
                        changedAmount -= changeValue;
                    }
                    else if (nightSurvivability == 0)
                    {
                        nightSurvivability++;
                        changedAmount += changeValue;
                    }
                    break;
            }
        }

        //set the new attribute values
        obj.GetComponent<Animal>().STRENGTH = strength;
        obj.GetComponent<Animal>().VITALITY = vitality;
        obj.GetComponent<Animal>().SPEED = speed;
        obj.GetComponent<Animal>().EYESTRENGTH = eyeStrength;
        obj.GetComponent<Animal>().NIGHTSURVIVABILITY = nightSurvivability;

        //call setup on the animal type
        if ((int)animalType == 1) obj.GetComponent<Wolf>().Setup();
        else if ((int)animalType == 2) obj.GetComponent<Fox>().Setup();
        else if ((int)animalType == 3) obj.GetComponent<Rabbit>().Setup();
    }

    public void DayEnding()
    {
        foreach (AnimalInfo info in animalsStatus)
        {
            //tell each animal that the day is ending
            info.gameObj.GetComponent<Animal>().dayEnding = true;
        }
    }

    public void DayStarting()
    {
        //flags and counters
        bool spawnWolves = true;
        int wolfCount = 0;
        bool spawnFoxes = true;
        int foxCount = 0;
        bool spawnRabbits = true;
        int rabbitCount = 0;

        //check if there are not enough animals of any type in the simulation at the start of this day
        foreach (AnimalInfo info in animalsStatus.ToArray())
        {
            if (!spawnWolves && !spawnFoxes && !spawnRabbits) break;

            if (spawnWolves)
            {
                if (info.type == Animal.AnimalTypes.Wolf)
                {
                    wolfCount++;
                    if (wolfCount > 2) spawnWolves = false;
                }
            }
            if (spawnFoxes)
            {
                if (info.type == Animal.AnimalTypes.Fox)
                {
                    foxCount++;
                    if (foxCount > 3) spawnFoxes = false;
                }
            }
            if (spawnRabbits)
            {
                if (info.type == Animal.AnimalTypes.Rabbit)
                {
                    rabbitCount++;
                    if (rabbitCount > 8) spawnRabbits = false;
                }
            }
        }

        //if there are not enough wolves, create 3
        if (spawnWolves)
        {
            for (int i = 0; i < 2; ++i) CreateAnimal(optimisedTopPredatorPrefab, topPredatorHousing, Animal.AnimalTypes.Wolf);
        }
        //if there are not enough foxes, create 6
        if (spawnFoxes)
        {
            for (int i = 0; i < 6; ++i) CreateAnimal(optimisedMiddlePredatorPrefab, middlePredatorHousing, Animal.AnimalTypes.Fox);
        }
        //if there are not enough rabbits, create 15
        if (spawnRabbits)
        {
            for (int i = 0; i < 15; ++i) CreateAnimal(optimisedPreyPrefab, preyHousing, Animal.AnimalTypes.Rabbit);
        }

        foreach (AnimalInfo info in animalsStatus)
        {
            //tell each animal the day is starting
            info.gameObj.GetComponent<Animal>().dayStarting = true;
        }

        //create a random amount of base vegetarian within a range, that will last for this day
        CreateFood(Food.FoodTypes.Vegetarian, Random.Range(65, 85), Vector3.zero, Vector3.zero);
    }

    public void DayOver()
    {
        foreach (AnimalInfo info in animalsStatus)
        {
            //tell each animal the day is over
            info.gameObj.GetComponent<Animal>().dayOver = true;
        }
    }

    public void CleanUpDay()
    {
        //destroy all remaining base food objects
        foreach (GameObject obj in baseFood) Destroy(obj);
        //clear the relevant lists
        baseFood.Clear();
        baseFoodLocations.Clear();

        //destroy all remaining meat food type objects
        foreach (GameObject obj in spawnedMeat) Destroy(obj);
        //clear the list of spawned meat
        spawnedMeat.Clear();
    }
}

public class AnimalInfo
{
    public GameObject gameObj;
    public int animalIndex;
    public Animal.AnimalTypes type;

    public static AnimalInfo AddAnimalInfo(GameObject gameObj, int animalIndex, Animal.AnimalTypes type)
    {
        AnimalInfo info = new AnimalInfo();
        info.gameObj = gameObj;
        info.animalIndex = animalIndex;
        info.type = type;

        return info;
    }
}

public class CaveInfo
{
    public GameObject gameObj;
    public int caveIndex;
    public GameObject entrance;
    public GameObject exit;
    public Vector2 vec2Position;
    public int takenSpace;
    public int maxTakenSpace;
    public Animal.AnimalTypes takenAnimalType;
    public List<int> animalsInside;

    public static CaveInfo AddCaveInfo(GameObject gameObj, int caveIndex, int takenSpace, int maxTakenSpace)
    {
        CaveInfo info = new CaveInfo();
        info.gameObj = gameObj;
        info.caveIndex = caveIndex;

        foreach (Transform transform in info.gameObj.GetComponentsInChildren<Transform>())
        {
            if (info.entrance != null && info.exit != null) break;

            if (transform.CompareTag("Cave Entrance")) info.entrance = transform.gameObject;
            else if (transform.CompareTag("Cave Exit")) info.exit = transform.gameObject;
        }

        info.vec2Position = new Vector2(gameObj.transform.position.x, gameObj.transform.position.z);
        info.takenSpace = takenSpace;
        info.maxTakenSpace = maxTakenSpace;
        info.takenAnimalType = Animal.AnimalTypes.Empty;
        info.animalsInside = new List<int>();

        return info;
    }
}


public class Vitals
{
    public struct HealthNames
    {
        public string healthyText;
        public int healthyValue;
        public string woundedText;
        public int woundedValue;
        public string badlyWoundedText;
        public int badlyWoundedValue;
        public string mortallyWoundedText;
        public int mortallyWoundedValue;
        public string deadText;
        public int deadValue;
    }

    public struct HungerNames
    {
        public string fullText;
        public int fullValue;
        public string satisfiedText;
        public int satisfiedValue;
        public string hungryText;
        public int hungryValue;
        public string veryHungryText;
        public int veryHungryValue;
        public string starvingText;
        public int starvingValue;
    }

    public struct ThirstNames
    {
        public string fullText;
        public int fullValue;
        public string satisfiedText;
        public int satisfiedValue;
        public string thirstyText;
        public int thirstyValue;
        public string veryThirstyText;
        public int veryThirstyValue;
        public string severelyDehydratedText;
        public int severelyDehdratedValue;

    }

    public HealthNames healthNames;
    public HungerNames hungerNames;
    public ThirstNames thirstNames;

    public HealthNames HealthStats() { return healthNames; }
    public HungerNames HungerStats() { return hungerNames; }
    public ThirstNames ThirstStats() { return thirstNames; }


    public void Initialize()
    {
        healthNames.healthyText = "Healthy";
        healthNames.healthyValue = 80;
        healthNames.woundedText = "Wounded";
        healthNames.woundedValue = 60;
        healthNames.badlyWoundedText = "Badly Wounded";
        healthNames.badlyWoundedValue = 40;
        healthNames.mortallyWoundedText = "Mortally Wounded";
        healthNames.mortallyWoundedValue = 20;
        healthNames.deadText = "Dead";
        healthNames.deadValue = 0;

        hungerNames.fullText = "Full";
        hungerNames.fullValue = 80;
        hungerNames.satisfiedText = "Satisfied";
        hungerNames.satisfiedValue = 60;
        hungerNames.hungryText = "Hungry";
        hungerNames.hungryValue = 40;
        hungerNames.veryHungryText = "Very Hungry";
        hungerNames.veryHungryValue = 20;
        hungerNames.starvingText = "Starving";
        hungerNames.starvingValue = 0;

        thirstNames.fullText = "Full";
        thirstNames.fullValue = 80;
        thirstNames.satisfiedText = "Satisfied";
        thirstNames.satisfiedValue = 60;
        thirstNames.thirstyText = "Thirsty";
        thirstNames.thirstyValue = 40;
        thirstNames.veryThirstyText = "Very Thirsty";
        thirstNames.veryThirstyValue = 20;
        thirstNames.severelyDehydratedText = "Severely Dehydrated";
        thirstNames.severelyDehdratedValue = 0;
    }
}
