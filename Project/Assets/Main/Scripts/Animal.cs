using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class Animal : MonoBehaviour
{
    //overview
    [SerializeField] public bool kill;
    [HideInInspector] public bool killed;
    [HideInInspector] public GameObject housing;
    [HideInInspector] public float attackRange;
    [HideInInspector] public int foodValue;
    [HideInInspector] public SimulationManager simulationManager;
    [HideInInspector] public NavMeshAgent navMeshAgent;
    [HideInInspector] public bool pauseAnimal = false;

    //attributes info
    [HideInInspector] public int BASEATTRIBUTESAMOUNT;
    [HideInInspector] public int BASEATTRIBUTESRANGE;
    [HideInInspector] public int MAXBASEATTRIBUTEAMOUNT;
    [HideInInspector] public int MAXATTRIBUTEVALUE;
    [HideInInspector] public int ATTRIBUTESAMOUNT;

    //attribute values
    [HideInInspector] public int STRENGTH;
    [HideInInspector] public int VITALITY;
    [HideInInspector] public int SPEED;
    [HideInInspector] public int EYESTRENGTH;
    [HideInInspector] public int NIGHTSURVIVABILITY;

    //vitals
    [SerializeField] public int HEALTH;
    [SerializeField] public int HUNGER;
    [SerializeField] public int THIRST;

    //wait time at start of simulation
    [HideInInspector] public float startWaitTime = 2.5f;
    [HideInInspector] public bool startWaiting = true;

    //this animal's index within the scene
    private int animalIndex;
    public void SetAnimalIndex(int index) { animalIndex = index; } //set the animal index
    public int GetAnimalIndex() { return animalIndex; } //get the animal index

    //dot product values for vision
    [SerializeField] public float dotValue = 0;
    [SerializeField] public GameObject dotTarget = null;
    private float defaultDotVisionValue = 0.6f;

    //allow / disallowed each behaviour to be performed
    [SerializeField] public bool shouldChase = false;
    [SerializeField] public bool shouldFlee = false;
    [SerializeField] public bool shouldAttack = false;
    [SerializeField] public bool goToFood = false;
    [SerializeField] public bool goToWater = false;
    [SerializeField] public bool goToShelter = false;
    //the goto target item for the above behaviours
    private AnimalInfo chaseTarget;
    private AnimalInfo fleeTarget;
    private GameObject foodTarget;
    private Vector3 waterTarget;
    private CaveInfo caveTarget;

    //death animation
    private bool playDeathAnimation = false;
    private float deathAnimationTimer;
    private float deathAnimationWaitTime = 5.0f;
    private float deathAnimationTotalTime = 7.0f;

    //hurt / attack animation
    private bool playHurtAnimation = false;
    private float hurtAnimationTotalTime = 6.0f;

    //vitals
    public bool eaten = false;
    public bool eating = false;
    public bool drank = false;
    public bool drinking = false;
    public int maxHealth;

    //day & night cycle of the simulation
    public bool dayStarting = false;
    public bool dayEnding = false;
    public bool dayOver = false;
    public bool seekShelter = false;
    public bool dayCycleOver = false;
    public bool isInsideShelter = false;

    //when to start seeking shelter
    private bool seekShelterToggled = false;
    private bool shelterCostActive = false;
    private void EnableSeekShelter() { seekShelter = true; } //start seeking shelter

    //kill during / at night
    public bool inShelter = false;
    public bool killOvernight = false;

    //enum of animals within the simulation
    public enum AnimalTypes
    {
        Empty,
        Wolf,
        Fox,
        Rabbit
    }

    public void SetUpdateAnimal(bool update)
    {
        //stops the animal from moving and updating
        pauseAnimal = !update;

        if (pauseAnimal) navMeshAgent.isStopped = true;
        else navMeshAgent.isStopped = false;
    }

    public void UpdateAnimalAnimations()
    {
        ManageAnimations();
    }

    private void ManageAnimations()
    {
        //play each animation if needed
        if (playDeathAnimation) AnimateDeath();
        if (playHurtAnimation) AnimateHurt();
    }

    public bool CheckDeath()
    {
        HEALTH = Mathf.Clamp(HEALTH, 0, maxHealth);

        //check if the animal should be dead
        if (HEALTH <= 0) kill = true;

        if (kill && !killed) return true;

        return false;
    }

    public void Kill(AnimalTypes myType)
    {
        navMeshAgent.enabled = true;
        navMeshAgent.isStopped = true;

        killed = true;

        //tell simulation manager to remove this animal from list of alive animals
        simulationManager.RemoveAnimal(animalIndex);

        //get position & rotation
        Vector3 position = gameObject.transform.GetChild(0).position;
        Quaternion rotation = Quaternion.Euler(gameObject.transform.GetChild(0).eulerAngles);

        foreach (Transform child in transform.transform)
        {
            //destroy all children in optimized version of the animal
            Destroy(child.gameObject);
        }

        //instantiate the fully cubed version of the animal
        if (myType == AnimalTypes.Wolf) housing = Instantiate(simulationManager.cubedTopPredatorPrefab, position, rotation, gameObject.transform);
        else if (myType == AnimalTypes.Fox) housing = Instantiate(simulationManager.cubedMiddlePredatorPrefab, position, rotation, gameObject.transform);
        else if (myType == AnimalTypes.Rabbit) housing = Instantiate(simulationManager.cubedPreyPrefab, position, rotation, gameObject.transform);

        Vector3 force;

        //calculate the force to apply on the animal's body
        if (fleeTarget == null) force = new Vector3(0.0f, -0.5f, 0.0f);
        else if (fleeTarget.gameObj == null) force = new Vector3(0.0f, -0.5f, 0.0f);
        else
        {
            //applied force will move the cubes in the direction away from the attacker
            force = transform.position - fleeTarget.gameObj.transform.position;
            //downward force will always be constant no matter where the attacker is
            force.y = -0.5f;

            //get absolute value of x and z
            float xAbs = Mathf.Abs(force.x);
            float zAbs = Mathf.Abs(force.z);

            float multi;
            //calculate the multiplier
            if (xAbs > zAbs) multi = 3 / xAbs;
            else multi = 3 / zAbs;

            //apply multiplier such that the larger force will be scaled down to a constant value, and the smaller force will be scaled down at the same rate
            force.x *= multi;
            force.z *= multi;
        }

        foreach (Transform child in housing.transform)
        {
            //enable the collider on the cube
            child.GetComponent<BoxCollider>().enabled = true;
            //add a rigidbody to the cube
            Rigidbody rb = child.gameObject.AddComponent<Rigidbody>();
            //add the calculated force onto the cube on impulse mode
            rb.AddForce(force, ForceMode.Impulse);
        }

        //create the food prefabs
        simulationManager.CreateFood(Food.FoodTypes.Meat, foodValue, transform.position, force);

        //disable the navmesh agent
        navMeshAgent.enabled = false;

        //start the death animation
        Invoke("PrepareAnimateDeath", deathAnimationWaitTime);
        //destroy this game object when the animation has finished
        Destroy(gameObject, deathAnimationWaitTime + deathAnimationTotalTime);
    }

    public void OvernightDeath()
    {
        killed = true;
        //tell simulation manager to remove this animal from list of alive animals
        simulationManager.RemoveAnimal(animalIndex);

        foreach (Transform child in transform.transform)
        {
            //destroy all children in optimized version of child
            Destroy(child.gameObject);
        }

        //disable the navmesh agent
        navMeshAgent.enabled = false;
        //destroy this game object now
        Destroy(gameObject);
    }

    private void AnimateDeath()
    {
        //destroy this game object or update the animation timer if it isnt finished
        if (deathAnimationTimer < deathAnimationTotalTime) deathAnimationTimer += Time.deltaTime;
        else Destroy(gameObject);

        //get the new alpha value for each cube for this frame
        float newAlphaValue = 1 - deathAnimationTimer / deathAnimationTotalTime;
        if (newAlphaValue > 1) return;
        else if (newAlphaValue < 0) newAlphaValue = 0;

        //get desired (transparent) render queue
        int transparencyRenderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        foreach (Transform child in housing.transform)
        {
            //get the mesh renderer component
            MeshRenderer renderer = child.GetComponent<MeshRenderer>();
            if (renderer) //if it exists
            {
                //if the render queue is not the desired render queue (transparent)
                if (renderer.material.renderQueue != transparencyRenderQueue)
                {
                    //apply the required changes to make the material support transparency
                    child.GetComponent<MeshRenderer>().material = MakeMaterialTransparent(renderer.material);
                }

                //get current colour
                Color newColour = renderer.material.color;
                //set new alpha value
                newColour.a = newAlphaValue;
                //apply new colour
                child.GetComponent<MeshRenderer>().material.color = newColour;
            }
        }
    }

    private void AnimateHurt()
    {
        //play play animations if the animal is dead
        if (killed) return;

        //if animation should play
        if (playHurtAnimation)
        {
            Vector3 force;
            //checks to see if animation shouldnt be playing
            if (fleeTarget == null)
            {
                playHurtAnimation = false;
                return;
            }
            else if (fleeTarget.gameObj == null)
            {
                playHurtAnimation = false;
                return;
            }
            else
            {
                //applied force will move the cubes in the direction away from the attacker
                force = transform.position - fleeTarget.gameObj.transform.position;
                //downward force will always be constant no matter where the attacker is
                force.y = -0.5f;

                //get the absolute value of x and z
                float xAbs = Mathf.Abs(force.x);
                float zAbs = Mathf.Abs(force.z);

                float multi;
                //calculate the force multiplier
                if (xAbs > zAbs) multi = 3 / xAbs;
                else multi = 3 / zAbs;

                //apply multiplier such that the larger force will be scaled down to a constant value, and the smaller force will be scaled down at the same rate
                force.x *= multi;
                force.z *= multi;
            }

            //get the correct prefab for this animal
            GameObject prefab = simulationManager.smallAnimalHurtEffectPrefab;
            if (GetComponent<Rabbit>() == null) prefab = simulationManager.largeAnimalHurtEffectPrefab;

            //get the position to instantiate the prefab at
            Vector3 newPosition = transform.position;
            newPosition.y += 0.25f;

            //instantiate the prefab
            GameObject obj = Instantiate(prefab, newPosition, Quaternion.identity);

            foreach (Transform child in obj.transform)
            {
                //enable the collider
                child.GetComponent<BoxCollider>().enabled = true;
                //add a rigidbody to the cube
                Rigidbody rb = child.gameObject.AddComponent<Rigidbody>();
                //add the calculated force onto the cube on impulse mode
                rb.AddForce(force, ForceMode.Impulse);
            }

            //destroy theprefab after a set time
            Destroy(obj, hurtAnimationTotalTime);
            //reset flag
            playHurtAnimation = false;
        }
    }

    private void PrepareAnimateDeath()
    {
        foreach (Transform child in housing.transform)
        {
            //destroy the rigidbody
            if (child.GetComponent<Rigidbody>()) Destroy(child.GetComponent<Rigidbody>());
            //set the glossiness (smoothness) to 1
            if (child.GetComponent<MeshRenderer>()) child.GetComponent<MeshRenderer>().material.SetFloat("_Glossiness", 1.0f);
        }

        //update flag to perform animation
        playDeathAnimation = true;
    }

    private Material MakeMaterialTransparent(Material material)
    {
        //make required changes to change the material to transparent
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0.0f);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        int minRenderQueue = (int)UnityEngine.Rendering.RenderQueue.GeometryLast + 1;
        int maxRenderQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay - 1;
        int targetRenderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        if (material.renderQueue < minRenderQueue || material.renderQueue > maxRenderQueue)
        {
            //set the render queue
            material.renderQueue = targetRenderQueue;
        }

        return material;
    }

    private Vector3 GetMoveDestination(int visionZone, float rawMoveDistance, float sourceDirection)
    {
        //get a move distance
        float randomMoveDistance = Random.Range(rawMoveDistance / 2, (float)(rawMoveDistance * 4 / 5));
        //get a move direction
        float randomMoveDirection = sourceDirection + Random.Range(-visionZone / 2, (float)(visionZone / 2));

        //remember the current rotation
        Vector3 oldRotation = transform.eulerAngles;

        //set the new rotation
        transform.eulerAngles = new Vector3(0, randomMoveDirection, 0);
        //calculate the new destination using forward transform and the randomized distance
        Vector3 targetDestination = transform.position + transform.forward * randomMoveDistance;

        bool hasHit = false;
        while (!hasHit)
        {
            hasHit = false;

            //raycast all from target position downwards
            RaycastHit[] hits = Physics.RaycastAll(targetDestination, Vector3.down, 5.0f);
            foreach (RaycastHit hit in hits)
            {
                //if the raycast hit the floor object
                if (hit.collider.gameObject.CompareTag("Floor")) hasHit = true;
            }

            if (!hasHit)
            {
                //restore the rotation
                transform.eulerAngles = oldRotation;

                //get a new move distance
                randomMoveDistance = Random.Range(rawMoveDistance / 2, (float)(rawMoveDistance * 4 / 5));
                //get a new move direction using a wider range
                randomMoveDirection = Random.Range(visionZone, (float)(visionZone += 15));
                //get random number to decide if we should flip the sign or not
                if (Random.Range(0, 2) == 0) randomMoveDirection *= -1;
                randomMoveDirection += transform.eulerAngles.y;

                //set the new rotation
                transform.eulerAngles = new Vector3(0, randomMoveDirection, 0);
                //calculate the new destination using forward transform and the randomized distance
                targetDestination = transform.position + transform.forward * randomMoveDistance;
            }
        }

        NavMeshHit navHit;
        if (NavMesh.SamplePosition(targetDestination, out navHit, 3, NavMesh.AllAreas))
        {
            //set new destination to the position on navmesh nearest to the calculated destination
            targetDestination = navHit.position;

            float backupAmount = 0.5f;
            //move target destination slightly closer to the centre
            if (targetDestination.x > 0) targetDestination.x -= backupAmount;
            else targetDestination.x += backupAmount;
            if (targetDestination.z > 0) targetDestination.z -= backupAmount;
            else targetDestination.z += backupAmount;

            if (NavMesh.SamplePosition(targetDestination, out navHit, 1, NavMesh.AllAreas))
            {
                //set new destination to the position on navmesh nearest to the calculated destination
                targetDestination = navHit.position;
            }
        }

        //restore the rotation
        transform.eulerAngles = oldRotation;

        return targetDestination;
    }

    private AnimalInfo GetTarget(List<AnimalInfo> validTargets, AnimalTypes myType)
    {
        int size = validTargets.Count;

        Vector2 myPos = new Vector2(transform.position.x, transform.position.z);

        List<AnimalInfo> orderedTargets = new List<AnimalInfo>();
        List<float> orderedDistances = new List<float>();

        for (int i = 0; i < size; ++i)
        {
            //get current target
            AnimalInfo target = validTargets[i];
            //get position of the current target
            Vector2 targetPos = new Vector2(validTargets[i].gameObj.transform.position.x, validTargets[i].gameObj.transform.position.z);
            //calculate distance from this animal to the target animal
            float targetDistance = Vector2.Distance(targetPos, myPos);
            if (targetDistance >= EYESTRENGTH)
            {
                //if the target animal is beyond this animal's eye strength, remove it from the list
                validTargets.RemoveAt(i);
                //decrement incrementor as the irrelevant animal has been removed from list
                i--;
                size--;
            }
            else
            {
                //if list is empty or target animal is further away than the furthest away animal
                if (orderedDistances.Count == 0 || orderedDistances[orderedDistances.Count - 1] < targetDistance)
                {
                    //add the animal into the ordered list
                    orderedDistances.Add(targetDistance);
                    orderedTargets.Add(target);
                }
                else
                {
                    for (int j = 0; j < orderedDistances.Count; ++j)
                    {
                        //if the targets' distance from this animal is nearer than the one at this index
                        if (targetDistance < orderedDistances[j])
                        {
                            //insert the target animal into the correct place in the list
                            orderedDistances.Insert(j, targetDistance);
                            orderedTargets.Insert(j, target);
                            break;
                        }
                    }
                }
            }
        }

        //if there are no animals in this animals vision, return
        if (orderedTargets.Count == 0) return null;

        AnimalInfo newTarget = null;

        foreach (AnimalInfo info in orderedTargets)
        {
            //get the source position
            Vector3 source = transform.position;
            source.y += 0.25f;
            //get the desination position
            Vector3 destination = info.gameObj.transform.position;
            destination.y += 0.25f;

            //get vector2 from the vector3
            Vector2 v2Source = new Vector2(source.x, source.z);
            Vector2 v2Destination = new Vector2(destination.x, destination.z);

            //get the vector from source to destination
            Vector2 newDestination = v2Destination - v2Source;
            //normalize the value
            newDestination.Normalize();

            dotValue = 0;
            dotTarget = null;

            //if the distance is not within nearby radii
            if (Vector2.Distance(v2Source, v2Destination) > 2.5F)
            {
                dotTarget = info.gameObj;
                //calculate the dot product of forward transform and destination vector
                dotValue = Vector2.Dot(transform.forward, newDestination);
                
                //default angle
                float eyeStrengthBonus = 0.05f;
                //max benefit from eye strength at 11 points, and apply to vision radius
                if (EYESTRENGTH > 11) eyeStrengthBonus *= 11;
                else eyeStrengthBonus *= EYESTRENGTH;

                //if target animal is not visible to this animal, continue onto the next target animal
                if (dotValue < defaultDotVisionValue - eyeStrengthBonus) continue;
            }
            else
            {
                //target is within nearby radii, therefore this animal is aware of them even without seeing them
                newTarget = info;
                return newTarget;
            }

            List<Vector3> destinations = new List<Vector3>();

            foreach (Transform child in info.gameObj.transform)
            {
                //if the child is the housing object of raycast reference points
                if (child.CompareTag("Reference Points"))
                {
                    foreach(Transform referencePoint in child.GetComponentsInChildren<Transform>())
                    {
                        //add this point to the list to raycast to
                        destinations.Add(referencePoint.position);
                    }

                    //break to exit loop early as we've already added all reference points to list
                    break;
                }
            }

            //if any of our rays hit the target animal without being obstructed by another object
            if (FireRays(source, transform.forward, destinations))
            {
                //set the new target
                newTarget = info;
                return newTarget;
            }
        }

        //return null if there is no target
        if (newTarget == null) return null;

        return newTarget;
    }

    public void LookForTargets(AnimalTypes myType)
    {
        List<AnimalInfo> validTargets = new List<AnimalInfo>();

        foreach (AnimalInfo info in simulationManager.animalsStatus)
        {
            //empty type
            if ((int)info.type == 0) continue;

            //get & calculate some info
            Animal infoAnimal = info.gameObj.GetComponent<Animal>();
            float infoHealth = (float)infoAnimal.HEALTH / (float)infoAnimal.maxHealth * 100;
            float myHealth = (float)HEALTH / (float)(maxHealth) * 100;

            //inside a cave
            if (info.gameObj.GetComponent<Animal>().isInsideShelter) continue;

            //self
            if (infoAnimal.animalIndex == animalIndex) continue;

            //wolf
            if ((int)myType == 1)
            {
                //only hunt other wolves if this animal is very hungry, not badly wounded, and the target animal is badly wounded
                if ((int)info.type == 1)
                {
                    if (HUNGER > simulationManager.vitals.HungerStats().veryHungryValue) continue;
                    if (infoHealth > simulationManager.vitals.HealthStats().badlyWoundedValue && myHealth < simulationManager.vitals.HealthStats().woundedValue) continue;
                }
                //always hunt foxes
                //only hunt rabbits if this animal is very hungry
                if ((int)info.type == 3)
                {
                    if (HUNGER > simulationManager.vitals.HungerStats().veryHungryValue) continue;
                }
            }

            //fox
            if ((int)myType == 2)
            {
                //never hunt wolves
                if ((int)info.type == 1) continue;
                //only hunt other foxes if this animal is very hungry, not badly wounded, and the target animal is badly wounded
                if ((int)info.type == 2)
                {
                    if (HUNGER > simulationManager.vitals.HungerStats().veryHungryValue) continue;
                    if (infoHealth > simulationManager.vitals.HealthStats().badlyWoundedValue && myHealth < simulationManager.vitals.HealthStats().healthyValue) continue;
                }
            }

            //rabbits never hunt any animals
            if ((int)myType == 3) continue;

            //add the valid target to list
            validTargets.Add(info);
        }

        //return if there are no potential targets
        if (validTargets.Count == 0)
        {
            shouldChase = false;
            return;
        }

        //see if an animal in the list of valid animals is a potential target
        chaseTarget = GetTarget(validTargets, myType);

        //check if we can chase a target or not
        if (chaseTarget == null) shouldChase = false;
        else shouldChase = true;

        return;
    }

    public void LookForAttackers(AnimalTypes myType)
    {
        List<AnimalInfo> validTargets = new List<AnimalInfo>();

        foreach (AnimalInfo info in simulationManager.animalsStatus)
        {
            //empty
            if ((int)info.type == 0) continue;

            //inside a cave
            if (info.gameObj.GetComponent<Animal>().isInsideShelter) continue;

            //get & calculate some information
            Animal infoAnimal = info.gameObj.GetComponent<Animal>();
            float infoHealth = (float)infoAnimal.HEALTH / (float)infoAnimal.maxHealth * 100;
            float myHealth = (float)HEALTH / (float)(maxHealth) * 100;

            //self
            if (infoAnimal.animalIndex == animalIndex) continue;

            //wolf
            if ((int)myType == 1)
            {
                //only run from other wolves if this animal is very hungry and badly wounded, and the target animal is not badly wounded
                if ((int)info.type == 1)
                {
                    if (infoAnimal.HUNGER > simulationManager.vitals.HungerStats().veryHungryValue) continue;
                    if (infoHealth < simulationManager.vitals.HealthStats().badlyWoundedValue && myHealth > simulationManager.vitals.HealthStats().woundedValue) continue;
                }
                //never run from foxes
                if ((int)info.type == 2) continue;
                //never run from rabbits
                if ((int)info.type == 3) continue;
            }

            //fox
            if ((int)myType == 2)
            {
                //always run from wolves
                //only run from other foxes if this animal is very hungry and badly wounded, and the target animal is not badly wounded
                if ((int)info.type == 2)
                {
                    if (HUNGER > simulationManager.vitals.HungerStats().veryHungryValue) continue;
                    if (infoHealth < simulationManager.vitals.HealthStats().badlyWoundedValue && myHealth > simulationManager.vitals.HealthStats().healthyValue) continue;
                }
                //never run from rabbits
                if ((int)info.type == 3) continue;
            }

            //rabbit
            if ((int)myType == 3)
            {
                //always run from wolves
                //always run from foxes
                //never run from rabbits
                if ((int)info.type == 3) continue;
            }

            //add the valid target to list
            validTargets.Add(info);
        }

        //return if there are no potential targets
        if (validTargets.Count == 0)
        {
            shouldFlee = false;
            return;
        }

        //see if an animal in the list of valid animals is a potential target
        fleeTarget = GetTarget(validTargets, myType);

        //check if we can run from a target or not
        if (fleeTarget == null) shouldFlee = false;
        else shouldFlee = true;

        return;
    }

    public void LookForFood(Food.FoodTypes foodType)
    {
        List<GameObject> validTargets;

        //get the relevant list of food on the map
        if (foodType == Food.FoodTypes.Vegetarian) validTargets = new List<GameObject>(simulationManager.baseFood);
        else if (foodType == Food.FoodTypes.Meat) validTargets = new List<GameObject>(simulationManager.spawnedMeat);
        else
        {
            goToFood = false;
            foodTarget = null;
            return;
        }

        int size = validTargets.Count;

        //return if there is no food
        if (size == 0)
        {
            goToFood = false;
            return;
        }

        Vector2 myPos = new Vector2(transform.position.x, transform.position.z);

        List<GameObject> orderedTargets = new List<GameObject>();
        List<float> orderedDistances = new List<float>();

        for (int i = 0; i < size; ++i)
        {
            //get current target
            GameObject target = validTargets[i];
            //get position of current target
            Vector2 targetPos = new Vector2(validTargets[i].transform.position.x, validTargets[i].transform.position.z);
            //calculate distance from this animal to the food target
            float targetDistance = Vector2.Distance(targetPos, myPos);
            if (targetDistance > 2.0f && targetDistance >= EYESTRENGTH)
            {
                //if the food target is beyond this animal's eye strength, remove it from the list
                validTargets.RemoveAt(i);
                //decrement incrementor as the irrelevant food target has been removed from list
                i--;
                size--;
            }
            else
            {
                //if list is empty or the food target is further away than the furthest away food target
                if (orderedDistances.Count == 0 || orderedDistances[orderedDistances.Count - 1] < targetDistance)
                {
                    //add the food target to the ordered list
                    orderedDistances.Add(targetDistance);
                    orderedTargets.Add(target);
                }
                else
                {
                    for (int j = 0; j < orderedDistances.Count; ++j)
                    {
                        //if the food targets' distance from this animal is nearer than the one at this index
                        if (targetDistance < orderedDistances[j])
                        {
                            //insert the food target into the correct place in the list
                            orderedDistances.Insert(j, targetDistance);
                            orderedTargets.Insert(j, target);
                            break;
                        }
                    }
                }
            }
        }

        //if there are no animals in this animals vision, return
        if (orderedTargets.Count == 0)
        {
            goToFood = false;
            return;
        }
        
        GameObject newTarget = null;

        foreach (GameObject obj in orderedTargets)
        {
            //get the source position
            Vector3 source = transform.position;
            source.y += 0.5f;
            //get the destination position
            Vector3 destination = obj.transform.position;
            destination.y += 0.25f;

            //get the vector2 from the vector3
            Vector2 v2Source = new Vector2(source.x, source.z);
            Vector2 v2Destination = new Vector2(destination.x, destination.z);

            //get the vector from source to destination
            Vector2 newDestination = v2Destination - v2Source;
            //normalize the vector
            newDestination.Normalize();

            //if the distance is not within nearby radii
            if (Vector2.Distance(v2Source, v2Destination) > 2.5F)
            {
                //calculate the dot product of forward transform and destination vector
                float foodDotValue = Vector2.Dot(transform.forward, newDestination);

                //default angle
                float eyeStrentghBonus = 0.05f;
                //max benefit from eye strength at 11 points, and apply to vision radius
                if (EYESTRENGTH > 11) eyeStrentghBonus *= 11;
                else eyeStrentghBonus *= EYESTRENGTH;

                //if target animal is not visible to this animal, continue onto the next target animal
                if (foodDotValue < defaultDotVisionValue - eyeStrentghBonus) continue;
            }
            else
            {
                //food target is within nearby radii, therefore this animal is aware of it even without seeing them
                goToFood = true;
                foodTarget = obj;
                return;
            }

            bool canSee = true;
            RaycastHit[] hits;
            hits = Physics.RaycastAll(transform.position, destination - transform.forward, Vector3.Distance(destination, transform.position));

            //if the raycast hits are obstructed by another object, set canSee to false
            foreach (RaycastHit hit in hits)
            {
                if (!hit.collider.GetComponent<NavMeshAgent>()) canSee = false;
                if (hit.collider.transform.parent != null)
                {
                    if (hit.collider.transform.parent.parent != null)
                    {
                        if (!hit.collider.transform.parent.parent.GetComponent<NavMeshAgent>()) canSee = false;
                    }
                }

                //if we cannot see the target, break out of the loop early
                if (!canSee) break;
            }

            //if we cant see the target, continue onto next potential target
            if (!canSee) continue;

            newTarget = obj;
            break;
        }

        //if there is no food target
        if (newTarget == null)
        {
            //set flag to false and return null
            goToFood = false;
            return;
        }

        //set flag to true and set new food target
        goToFood = true;
        foodTarget = newTarget;
    }

    public void LookForWater()
    {
        List<Vector2> validTargets = new List<Vector2>(simulationManager.waterMap);

        int size = validTargets.Count;

        //get this animals position as a vector2
        Vector2 myPos = new Vector2(transform.position.x, transform.position.z);

        List<Vector2> orderedTargets = new List<Vector2>();
        List<float> orderedDistances = new List<float>();

        for (int i = 0; i < size; ++i)
        {
            //get distance from this animal to the target
            float targetDistance = Vector2.Distance(validTargets[i], myPos);
            if (targetDistance > 2.0f && targetDistance >= EYESTRENGTH)
            {
                //if the distance is beyond this animals eye strength, remove it from the list
                validTargets.RemoveAt(i);
                //decrement incrementor as the irrelevant water target has been removed from list
                i--;
                size--;
            }
            else
            {
                //if list is empty or the water target is further away than the furthest away food target
                if (orderedDistances.Count == 0 || orderedDistances[orderedDistances.Count - 1] < targetDistance)
                {
                    //add the water target to the ordered list
                    orderedDistances.Add(targetDistance);
                    orderedTargets.Add(validTargets[i]);
                }
                else
                {
                    for (int j = 0; j < orderedDistances.Count; ++j)
                    {
                        //if the water targets' distance from this animal is nearer than the one at this index
                        if (targetDistance < orderedDistances[j])
                        {
                            //insert water target into correct place in list
                            orderedDistances.Insert(j, targetDistance);
                            orderedTargets.Insert(j, validTargets[i]);
                            break;
                        }
                    }
                }
            }
        }

        //if there are no potential targets, set flag to false and return
        if (orderedTargets.Count == 0)
        {
            goToWater = false;
            return;
        }

        Vector3 newTarget = Vector2.zero;

        foreach (Vector2 target in orderedTargets)
        {
            //get source position
            Vector3 source = transform.position;
            source.y += 0.5f;
            //get destination position
            Vector3 destination = new Vector3(target.x, 0, target.y);
            destination.y += 0.75f;

            //get vector2 from the vector3
            Vector2 v2Source = new Vector2(source.x, source.z);

            //get the destination vector from source to destination
            Vector2 newDestination = target - v2Source;
            //normalize the vector
            newDestination.Normalize();

            //if the distance is not within nearby radii
            if (Vector2.Distance(v2Source, target) > 2.5F)
            {
                //calculate the dot product of forward transform and destination vector
                float waterDotValue = Vector2.Dot(transform.forward, newDestination);

                //max benefit from eye strength at 11 points, and apply to vision radius
                float eyeStrentghBonus = 0.05f;
                if (EYESTRENGTH > 11) eyeStrentghBonus *= 11;
                else eyeStrentghBonus *= EYESTRENGTH;

                //if water target is not visible to this animal, continue onto the next water target
                if (waterDotValue < defaultDotVisionValue - eyeStrentghBonus) continue;
            }
            else
            {
                //water target is within nearby radii, therefore this animal is aware of it even without seeing it
                goToWater = true;
                waterTarget = destination;
                //add some randomness to the destination to the x and z position of the water tile
                waterTarget.x += Random.Range(-0.4f, 0.4f);
                waterTarget.z += Random.Range(-0.4f, 0.4f);
                return;
            }

            bool canSee = true;
            RaycastHit[] hits;
            hits = Physics.RaycastAll(transform.position, destination - transform.forward, Vector3.Distance(destination, transform.position));

            //if the raycast hits are obstructed by another object, set canSee to false
            foreach (RaycastHit hit in hits)
            {
                if (!hit.collider.GetComponent<NavMeshAgent>()) canSee = false;
                if (hit.collider.transform.parent != null)
                {
                    if (hit.collider.transform.parent.parent != null)
                    {
                        if (!hit.collider.transform.parent.parent.GetComponent<NavMeshAgent>()) canSee = false;
                    }
                }
                
                //if we cannot see the target, break out of the loop early
                if (!canSee) break;
            }

            //if we cant see the target, continue onto next potential target
            if (!canSee) continue;

            //we can go to water target, therefore set it as the destination and add some randomness to the x and z position
            newTarget = destination;
            newTarget.x += Random.Range(-0.4f, 0.4f);
            newTarget.z += Random.Range(-0.4f, 0.4f);
            break;
        }

        //if there is no water target
        if (newTarget == Vector3.zero)
        {
            //set flag to false and return
            goToWater = false;
            return;
        }

        //set flag to true and set new food target
        goToWater = true;
        waterTarget = newTarget;
    }

    public void GoToFood()
    {
        //guard clauses
        if (!navMeshAgent.isActiveAndEnabled) return;
        if (!navMeshAgent.isOnNavMesh) return;
        if (foodTarget == null) return;

        //set destination
        navMeshAgent.destination = foodTarget.transform.position;
        //try to get the food
        EatFood(foodTarget);
    }

    public void GoToWater()
    {
        //guard clauses
        if (!navMeshAgent.isActiveAndEnabled) return;
        if (!navMeshAgent.isOnNavMesh) return;
        if (waterTarget == Vector3.zero) return;

        //set destination
        navMeshAgent.destination = waterTarget;
        //try to drink the water
        DrinkWater(waterTarget);
    }

    private void EatFood(GameObject food)
    {
        //if the animal is close enough to the food target to eat it
        if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(food.transform.position.x, food.transform.position.z)) < 1.0f)
        {
            //set flags
            goToFood = false;
            eaten = true;
            //remove food from relevant list in simulation manager
            if (simulationManager.baseFood.Contains(food)) simulationManager.baseFood.Remove(food);
            else if (simulationManager.spawnedMeat.Contains(food)) simulationManager.spawnedMeat.Remove(food);
            //destroy the food object after a short delay
            Destroy(food, 2.0f);
        }
    }

    private void DrinkWater(Vector3 water)
    {
        //if the animal is close enough to the water target to drink
        if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(water.x, water.z)) < 1.0f)
        {
            //set flags
            goToWater = false;
            drank = true;
        }
    }

    public void CheckCanAttack()
    {
        shouldAttack = false;

        //guard clauses
        if (chaseTarget == null) return;
        if (chaseTarget.gameObj == null) return;

        //check if the target animal is within this animal's attack range and set flags accordingly
        if (Vector3.Distance(chaseTarget.gameObj.transform.position, transform.position) > attackRange) shouldAttack = false;
        else shouldAttack = true;
    }

    public void Attack()
    {
        //attack the target animal
        chaseTarget.gameObj.GetComponent<Animal>().ReceiveDamage(STRENGTH);
    }

    private void ReceiveDamage(int damage)
    {
        //reduce this animal's health by the attackers damage
        HEALTH -= damage;
        HEALTH = Mathf.Clamp(HEALTH, 0, maxHealth);

        //set flag to play the hurt animation
        playHurtAnimation = true;
    }

    public void RandomizeAttributeAmount(int baseValue, int range)
    {
        List<int> triangularDistribution = new List<int>();
        //for each point in the distribution range
        for (int i = 0; i < range; ++i)
        {
            //for the density value at this point
            for (int j = i; j < range; ++j)
            {
                //add an item to the list above and below the centre point
                triangularDistribution.Add(i + baseValue);
                if (i != 0) triangularDistribution.Add(-i + baseValue);
            }
        }

        //get a random number to pick a index in the distribution list
        int newValueIndex = Random.Range(0, triangularDistribution.Count);
        //get the value at that point
        ATTRIBUTESAMOUNT = triangularDistribution[newValueIndex];
    }

    public void RandomizeAttributes(AnimalTypes myType)
    {
        //give animal one starting point in each attribute
        STRENGTH = 1;
        VITALITY = 1;
        SPEED = 1;
        EYESTRENGTH = 1;
        NIGHTSURVIVABILITY = 1;

        int assignedValues = 5;

        //while not all attributes have been assigned
        while (assignedValues < ATTRIBUTESAMOUNT)
        {
            assignedValues++;
            //get a random index to spend the attribute point in
            int newIndex = Random.Range(0, 5);
            //add the attribute point if possible
            switch (newIndex)
            {
                case 0:
                    if (STRENGTH < MAXATTRIBUTEVALUE) STRENGTH++;
                    else assignedValues--;
                    break;

                case 1:
                    if (VITALITY < MAXATTRIBUTEVALUE) VITALITY++;
                    else assignedValues--;
                    break;

                case 2:
                    if (SPEED < MAXATTRIBUTEVALUE) SPEED++;
                    else assignedValues--;
                    break;

                case 3:
                    if (EYESTRENGTH < MAXATTRIBUTEVALUE) EYESTRENGTH++;
                    else assignedValues--;
                    break;

                case 4:
                    if (NIGHTSURVIVABILITY < MAXATTRIBUTEVALUE) NIGHTSURVIVABILITY++;
                    else assignedValues--;
                    break;
            }
        }
    }

    private bool FireRays(Vector3 origin, Vector3 forward, List<Vector3> destinations)
    {
        for (int i = 0; i < destinations.Count; ++i)
        {
            bool canSeePoint = true;
            RaycastHit[] hits;
            //raycastall from source to destination point for this reference point
            hits = Physics.RaycastAll(origin, destinations[i] - forward, Vector3.Distance(destinations[i], origin));

            foreach (RaycastHit hit in hits)
            {
                //perform checks to see if we can see through this object or not - e.g. can see through other animals but cannot see through rocks
                if (!hit.collider.transform.GetComponent<NavMeshAgent>())
                {
                    if (hit.collider.transform.GetComponent<MeshRenderer>()) canSeePoint = false;
                    else if (hit.collider.transform.childCount > 0)
                    {
                        if (hit.collider.transform.GetChild(0).CompareTag("Cave Housing")) canSeePoint = false;
                    }
                }
                if (hit.collider.transform.parent != null)
                {
                    if (hit.collider.transform.parent.parent != null)
                    {
                        if (!hit.collider.transform.parent.parent.GetComponent<NavMeshAgent>()) canSeePoint = false;
                    }
                }

                //if the view is obstructed, break to continue onto next reference point
                if (!canSeePoint) break;
            }

            //if we can see any reference point, return as other points do not need to be checked
            if (canSeePoint) return true;
        }

        //all reference points were blocked, therefore return false as we cannot see the target
        return false;
    }

    public void CheckCycleStage()
    {
        if (dayOver && !dayStarting)
        {
            //if the animal isnt inside the shelter when midnight hits, kill it
            if (!isInsideShelter) kill = true;
            //set flags
            dayEnding = false;
            dayOver = false;
        }
        else if (dayEnding && !seekShelter)
        {
            if (!dayOver)
            {
                if (!seekShelterToggled)
                {
                    seekShelterToggled = true;

                    //set flag to enable action cost calculations on wolf
                    if (GetComponent<Wolf>() != null) shelterCostActive = true;
                    //delay the call for rabbits and foxes to seek shelter, reduce the delay based on night survivability attribute
                    else Invoke("EnableSeekShelter", 5.0f - (float)NIGHTSURVIVABILITY / 2.0f);
                }
            }
        }
        else if (dayStarting)
        {
            //reset flags
            dayStarting = false;
            dayEnding = false;
            dayOver = false;
            seekShelter = false;
            seekShelterToggled = false;
            //next day is ready to start
            dayCycleOver = true;
        }
    }

    public void LookForShelter(AnimalTypes myType)
    {
        if (isInsideShelter) return;

        List<CaveInfo> validTargets = new List<CaveInfo>(simulationManager.caveMap);

        int size = validTargets.Count;

        if (size == 0)
        {
            goToShelter = false;
            return;
        }

        //get this animals position as a vector2
        Vector2 myPos = new Vector2(transform.position.x, transform.position.z);

        List<CaveInfo> orderedTargets = new List<CaveInfo>();
        List<float> orderedDistances = new List<float>();

        for (int i = 0; i < size; ++i)
        {
            CaveInfo target = validTargets[i];
            //get distance from this animal to the target cave
            float targetDistance = Vector2.Distance(target.vec2Position, myPos);
            if (targetDistance >= EYESTRENGTH + 4)
            {
                //if the distance is beyond this animals eye strength, remove it from the list
                validTargets.RemoveAt(i);
                //decrement incrementor as the irrelevant cave target has been removed from list
                i--;
                size--;
            }
            else
            {
                //if list is empty or the cave target is further away than the furthest away food target
                if (orderedDistances.Count == 0 || orderedDistances[orderedDistances.Count - 1] < targetDistance)
                {
                    orderedDistances.Add(targetDistance);
                    orderedTargets.Add(target);
                }
                else
                {
                    for (int j = 0; j < orderedDistances.Count; ++j)
                    {
                        //add the cave target to the ordered list
                        if (targetDistance < orderedDistances[j])
                        {
                            //insert cave target into correct place in list
                            orderedDistances.Insert(j, targetDistance);
                            orderedTargets.Insert(j, target);
                            break;
                        }
                    }
                }
            }
        }

        //if there are no potential targets, set flag to false and return
        if (orderedTargets.Count == 0)
        {
            goToShelter = false;
            return;
        }

        size = validTargets.Count;

        //check for space for this animal to enter in each cave
        for (int i = 0; i < size; ++i)
        {
            //if cave is empty, keep it in list
            if ((int)validTargets[i].takenAnimalType == 0) continue;
            //if animals inside this cave are further down the food chain than this animal, keep it in list
            if ((int)validTargets[i].takenAnimalType > (int)myType) continue;
            //if this animal is the same type as those inside the cave
            if ((int)validTargets[i].takenAnimalType == (int)myType)
            {
                //wolves require 3 empty spaces
                if ((int)myType == 1 && (validTargets[i].maxTakenSpace - validTargets[i].takenSpace >= 3)) continue;
                //foxes require 2 empty spaces
                if ((int)myType == 2 && (validTargets[i].maxTakenSpace - validTargets[i].takenSpace >= 2)) continue;
                //rabbits require 1 empty space
                if ((int)myType == 3 && (validTargets[i].maxTakenSpace - validTargets[i].takenSpace >= 1)) continue;
            }

            //animal cannot enter the cave, therefore remove this cave target from the list
            validTargets.RemoveAt(i);
            orderedDistances.RemoveAt(i);
            orderedTargets.RemoveAt(i);
            //decrement incrementors
            i--;
            size--;
        }

        CaveInfo newCaveTarget = null;

        //check if cave is in line of sight
        foreach (CaveInfo target in orderedTargets)
        {
            //get source position
            Vector3 source = transform.position;
            source.y += 0.5f;
            //get destination position
            Vector3 destination = new Vector3(target.vec2Position.x, target.gameObj.transform.position.y, target.vec2Position.y);

            //get vector2 from the vector3
            Vector2 v2Source = new Vector2(source.x, source.z);

            //get the destination vector from source to destination
            Vector2 newDestination = target.vec2Position - v2Source;
            //normalize the vector
            newDestination.Normalize();

            //if the distance is not within nearby radii for caves
            if (Vector2.Distance(v2Source, target.vec2Position) > 6.0F)
            {
                //calculate the dot product of forward transform and destination vector
                float caveDotValue = Vector2.Dot(transform.forward, newDestination);

                float eyeStrentghBonus = 0.05f;
                //max benefit from eye strength at 11 points, and apply to vision radius
                if (EYESTRENGTH > 11) eyeStrentghBonus *= 11;
                else eyeStrentghBonus *= EYESTRENGTH;

                //if cave target is not visible to this animal, continue onto the next cave target
                if (caveDotValue < defaultDotVisionValue - eyeStrentghBonus) continue;
            }
            else
            {
                //cave target is within nearby radii, therefore this animal is aware of it even without seeing it
                goToShelter = true;
                caveTarget = target;
                return;
            }

            bool canSee = true;
            RaycastHit[] hits;
            hits = Physics.RaycastAll(transform.position, destination - transform.forward, Vector3.Distance(destination, transform.position));

            //if the raycast hits are obstructed by an obstacle, set canSee to false
            foreach (RaycastHit hit in hits)
            {
                if (!hit.transform.root.CompareTag("Cave Housing"))
                {
                    if (!hit.collider.GetComponent<NavMeshAgent>()) canSee = false;
                    if (hit.collider.transform.parent != null)
                    {
                        if (hit.collider.transform.parent.parent != null)
                        {
                            if (!hit.collider.transform.parent.parent.GetComponent<NavMeshAgent>()) canSee = false;
                        }
                    }
                }
            }

            //continue onto next target
            if (!canSee) continue;

            //set this cave as the new target, and stop looking for other targets
            newCaveTarget = target;
            break;
        }

        //if there is no target
        if (newCaveTarget == null)
        {
            //set flags and return
            caveTarget = null;
            goToShelter = false;
            return;
        }
        else
        {
            //set new target, set flag to true
            caveTarget = newCaveTarget;
            goToShelter = true;
        }
    }

    public void GoToShelter(AnimalTypes myType)
    {
        if (isInsideShelter) return;
        if (caveTarget == null) return;

        //set destination to the cave entrance
        navMeshAgent.destination = caveTarget.entrance.transform.position;
        //try to enter the cave
        EnterCave(myType);
    }

    public void ExitCave()
    {
        //if the cave target has been set to null, we cab get the nearest cave as the animal will be inside it
        if (caveTarget == null) caveTarget = GetNearestCave();

        //set flags
        isInsideShelter = false;
        goToShelter = false;
        navMeshAgent.enabled = true;
        navMeshAgent.isStopped = false;

        BoxCollider collider = null;

        foreach (Transform transform in caveTarget.gameObj.GetComponentsInChildren<Transform>())
        {
            //get the cave exit collider
            if (transform.CompareTag("Cave Exit")) collider = transform.gameObject.GetComponent<BoxCollider>();
            //break when the collider is found
            if (collider != null) break;
        }

        //get the position of the cave exit
        Vector3 newPosition = collider.gameObject.transform.position;
        //get a random point in the bounding box on x
        newPosition.x += Random.Range(-collider.bounds.extents.x, collider.bounds.extents.x);
        //set the new y position to the animal's current y position
        newPosition.y = transform.position.y;
        //get a random point in the bounding box on z
        newPosition.z += Random.Range(-collider.bounds.extents.z, collider.bounds.extents.z);

        //set the new position
        transform.position = newPosition;
        //set destination to the new position
        navMeshAgent.destination = transform.position;
        //reset cave target
        caveTarget = null;
    }
    
    private void EnterCave(AnimalTypes myType)
    {
        //get source position
        Vector2 source = new Vector2(transform.position.x, transform.position.z);
        //get destination position
        Vector2 destination = new Vector2(caveTarget.entrance.transform.position.x, caveTarget.entrance.transform.position.z);

        //if we are not close enough to the destination, return
        if (Vector3.Distance(source, destination) > 0.5f) return;

        bool canEnter = false;
        //if the cave does not have a taken type, set it to this animals type
        if (caveTarget.takenAnimalType == AnimalTypes.Empty) canEnter = true;
        else if ((int)caveTarget.takenAnimalType == (int)myType)
        {
            //animals inside the cave are the same type as this animal
            //wolves require 3 empty spaces
            if ((int)myType == 1 && (caveTarget.maxTakenSpace - caveTarget.takenSpace >= 3)) canEnter = true;
            //foxes require 2 empty spaces
            else if ((int)myType == 2 && (caveTarget.maxTakenSpace - caveTarget.takenSpace >= 2)) canEnter = true;
            //rabbits require 1 empty space
            else if ((int)myType == 3 && (caveTarget.maxTakenSpace - caveTarget.takenSpace >= 1)) canEnter = true;
        }
        else if ((int)caveTarget.takenAnimalType > (int)myType)
        {
            //animals inside this cave are lower in the food chain than this animal
            canEnter = true;
            caveTarget.takenSpace = 0;
            //evict all animals currently inside this cave
            FindObjectOfType<SimulationManager>().EvictCave(caveTarget.caveIndex);
        }

        if (!canEnter)
        {
            //set flags
            isInsideShelter = false;
            goToShelter = false;
            caveTarget = null;
            navMeshAgent.destination = transform.position;
            return;
        }

        //set the cave's taken animal type to this animals type
        caveTarget.takenAnimalType = myType;
        //add this animal to the list of animals inside the cave it's entering
        caveTarget.animalsInside.Add(animalIndex);
        isInsideShelter = true;

        //add the space this animal takes up to the takenSpace value for this cave
        if ((int)myType == 1) caveTarget.takenSpace += 3;
        else if ((int)myType == 2) caveTarget.takenSpace += 2;
        else if ((int)myType == 3) caveTarget.takenSpace += 1;

        //stop the navmesh agent and disable it
        navMeshAgent.isStopped = true;
        navMeshAgent.enabled = false;

        BoxCollider collider = null;

        foreach (Transform transform in caveTarget.gameObj.GetComponentsInChildren<Transform>())
        {
            //get the collider bounds for the area inside the cave that the animal can be within
            if (transform.CompareTag("Cave Inside")) collider = transform.gameObject.GetComponent<BoxCollider>();
            //break when the collider is found
            if (collider != null) break;
        }

        //get the position of the cave exit
        Vector3 newPosition = collider.gameObject.transform.position;
        //get a random point in the bounding box on x
        newPosition.x += Random.Range(-collider.bounds.extents.x, collider.bounds.extents.x);
        //set the new y position to the animal's current y position
        newPosition.y = transform.position.y;
        //get a random point in the bounding box on z
        newPosition.z += Random.Range(-collider.bounds.extents.z, collider.bounds.extents.z);

        //set this animal's position to the new position
        transform.position = newPosition;

        //get a random rotation for the y axis of this animal
        Vector3 newRotation = transform.eulerAngles;
        newRotation.y = Random.Range((float)0, (float)360);

        //set the new rotation
        transform.eulerAngles = newRotation;
    }

    public CaveInfo GetNearestCave()
    {
        //get the first cave in the cave list in simulation manager
        CaveInfo nearestCave = simulationManager.caveMap[0];
        //get distance from this animal to that cave
        float distanceToCurrent = Vector3.Distance(nearestCave.gameObj.transform.position, transform.position);

        //loop through every cave in the cave list in simulation manager
        foreach (CaveInfo cave in simulationManager.caveMap)
        {
            //get the distance from this animal to the cave
            float distanceToIndexed = Vector3.Distance(cave.gameObj.transform.position, transform.position);

            //if this animal is closer to this cave than the nearest cave
            if (distanceToIndexed < distanceToCurrent)
            {
                //set this cave as the nearest cave
                nearestCave = cave;
                //update the distance from this animal to nearest cave
                distanceToCurrent = Vector3.Distance(nearestCave.gameObj.transform.position, transform.position);
            }
        }

        //return the nearest cave
        return nearestCave;
    }

    public void DayFinished(AnimalTypes myType)
    {
        //check how much health should increase or decrease based on hunger
        if (HUNGER >= simulationManager.vitals.HungerStats().satisfiedValue) HEALTH += 10;
        else if (HUNGER < simulationManager.vitals.HungerStats().starvingValue) HEALTH -= 10;
        else if (HUNGER < simulationManager.vitals.HungerStats().veryHungryValue) HEALTH -= 5;
        //check how much health should increase or decrease based on thirst
        if (THIRST >= simulationManager.vitals.ThirstStats().satisfiedValue) HEALTH += 10;
        else if (THIRST < simulationManager.vitals.ThirstStats().severelyDehdratedValue) HEALTH -= 15;
        else if (THIRST < simulationManager.vitals.ThirstStats().veryThirstyValue) HEALTH -= 8;

        //add some additional health based on night survivability attribute
        HEALTH += Random.Range(NIGHTSURVIVABILITY / 3, NIGHTSURVIVABILITY);
        //decrease hunger & thirst overnight
        HUNGER -= 25;
        THIRST -= 35;

        //clamp values
        HEALTH = Mathf.Clamp(HEALTH, 0, maxHealth);
        HUNGER = Mathf.Clamp(HUNGER, 0, 100);
        THIRST = Mathf.Clamp(THIRST, 0, 100);

        //check if the animal has died overnight
        if (HEALTH == 0) killOvernight = true;

        if (killOvernight)
        {
            //kills the animal but skips the animation and other unnecessary features
            OvernightDeath();
            return;
        }

        dayCycleOver = false;

        //check if animal can reproduce overnight based on health and some randomness biased by this animal's night survivability attribute
        if (HEALTH >= simulationManager.vitals.HealthStats().healthyValue ||
            (HEALTH > simulationManager.vitals.HealthStats().woundedValue && Random.Range(1, 30) <= NIGHTSURVIVABILITY))
        {
            float randomTypeChance = 1;
            int randomAmount = 0;
            if ((int)myType == 1)
            {
                //suitable wolves have a 50% chance
                randomTypeChance = Random.Range(-1.0f, 1.0f);
                //get a random number for 1 or 2 children
                randomAmount = Random.Range(1, 3);
            }
            else if ((int)myType == 2)
            {
                //suitable foxes have a 62.5% chance
                randomTypeChance = Random.Range(-3.0f, 5.0f);
                //get a random number for 1 or 2 children
                randomAmount = Random.Range(1, 3);
            }
            else if ((int)myType == 3)
            {
                //suitable rabbits have an 85% chance
                randomTypeChance = Random.Range(-1.0f, 6.0f);
                //get a random number for 2, 3 or 4 children
                randomAmount = Random.Range(2, 5);
            }

            //if the random chance hit
            if (randomTypeChance >= 0)
            {
                //create the decided amount of children
                for(int i = 0; i < randomAmount; ++i) MakeNewAnimal(myType);
            }
        }

        //exit the cave as the day is over
        ExitCave();
        //set flags
        seekShelter = false;
        goToShelter = false;
    }

    private void MakeNewAnimal(AnimalTypes myType)
    {
        //if cave target has been set to null, get the nearest cave as the animal will be inside it
        if (caveTarget == null) caveTarget = GetNearestCave();

        BoxCollider collider = null;

        foreach (Transform transform in caveTarget.gameObj.GetComponentsInChildren<Transform>())
        {
            //get the collider bounds for the area inside the cave that the animal can be within
            if (transform.CompareTag("Cave Inside")) collider = transform.gameObject.GetComponent<BoxCollider>();
            //break when the collider is found
            if (collider != null) break;
        }

        //get the position of the cave exit
        Vector3 newPosition = collider.gameObject.transform.position;
        //get a random point in the bounding box on x
        newPosition.x += Random.Range(-collider.bounds.extents.x, collider.bounds.extents.x);
        //set the new y position to the animal's current y position
        newPosition.y = transform.position.y;
        //get a random point in the bounding box on z
        newPosition.z += Random.Range(-collider.bounds.extents.z, collider.bounds.extents.z);

        //get a random rotation on the y axis
        Quaternion newRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

        //instantiate the new animal as a child of this animal
        simulationManager.CreateAnimalAsChild(myType, newPosition, newRotation, animalIndex);
    }

    public float CalculateShelterCost(float shelterCost)
    {
        //calculate the shelter action cost using night survivability
        float costValue = shelterCost;
        if (shelterCostActive)
        {
            costValue += (0.15f * NIGHTSURVIVABILITY) / 100.0f;
            costValue = Mathf.Clamp(costValue, 0, 1);
        }

        return costValue;
    }

    public float CalculateFleeCost()
    {
        //calculate the flee action cost using flee target and eyestrength
        float costValue = 0;
        if (fleeTarget != null)
        {
            float distance = Vector3.Distance(fleeTarget.gameObj.transform.position, transform.position);
            costValue = 1 - (distance / EYESTRENGTH);
            costValue += Mathf.Pow(costValue, 3.0f);
            costValue = Mathf.Clamp(costValue, 0, 1);
        }

        return costValue;
    }

    public float CalculateChaseCost()
    {
        //calculate the chase action cost using hunger
        float costValue = 0;
        if (chaseTarget != null)
        {
            costValue = (100 - Mathf.Clamp((float)HUNGER / 1.25f, 1, 99)) / 100.0f;
            costValue += Mathf.Pow(costValue, 5.0f);
            costValue = Mathf.Clamp(costValue, 0, 0.92f);
        }

        return costValue;
    }

    public float CalculateAttackCost(float chaseCost)
    {
        //calculate the attack action cost using chase target, attack range and eye strength
        float costValue = 0;
        if (chaseTarget != null)
        {
            if (chaseTarget.gameObj == null) return costValue;

            float distance = Vector3.Distance(chaseTarget.gameObj.transform.position, transform.position);
            float rangeCost = (attackRange - distance) / EYESTRENGTH;
            costValue = Mathf.Pow(Mathf.Abs(rangeCost), 1.3f);
            if (rangeCost < 0) costValue *= -1;
            costValue += chaseCost;
            costValue = Mathf.Clamp(costValue, 0, 0.96f);
        }

        return costValue;
    }

    public float CalculateEatCost()
    {
        //calculate the flee cost using food target, hunger and eye strength
        float costValue = 0;
        if (foodTarget != null)
        {
            float distance = Vector3.Distance(foodTarget.transform.position, transform.position);
            float rangeCost = distance / EYESTRENGTH;
            costValue = (100 - Mathf.Clamp((float)HUNGER / 2.0f, 1, 99)) / 100.0f;
            if (rangeCost < 1) costValue += (1 - rangeCost) / 3.0f;
            costValue = Mathf.Clamp(costValue, 0, 1);
        }

        return costValue;
    }

    public float CalculateDrinkCost()
    {
        //calculate the drink cost using water target, thirst and eye strength
        float costValue = 0;
        if (waterTarget != null)
        {
            float distance = Vector3.Distance(waterTarget, transform.position);
            float rangeCost = distance / EYESTRENGTH;
            costValue = (100 - Mathf.Clamp(THIRST, 1, 99)) / 100.0f;
            if (rangeCost < 1) costValue += (1 - rangeCost) / 3.0f;
            costValue = Mathf.Clamp(costValue, 0, 1);
        }

        return costValue;
    }

    public float CalculateWanderCost()
    {
        //calculate wander cost using hunger and thirst
        float costValue;
        float hungerCost = Mathf.Clamp(HUNGER, 1, 99) / 100.0f;
        float thirstCost = Mathf.Clamp(THIRST, 1, 99) / 100.0f;
        costValue = hungerCost + thirstCost;
        costValue /= 2;
        costValue = Mathf.Pow(costValue, 0.8f);
        costValue = Mathf.Clamp(costValue, 0, 0.92f);

        return costValue;
    }

    public void WanderBehaviour()
    {
        //guard clauses
        if (navMeshAgent == null) return;
        if (!navMeshAgent.enabled) return;
        if (!navMeshAgent.isActiveAndEnabled) return;
        if (!navMeshAgent.isOnNavMesh) return;

        //get destination to wander to
        Vector3 destination = GetMoveDestination(135, EYESTRENGTH, transform.eulerAngles.y);
        //set navmesh agent destination to calculated destination
        navMeshAgent.SetDestination(destination);
    }

    public void ChaseBehaviour()
    {
        //guard clauses
        if (chaseTarget == null) return;
        if (chaseTarget.gameObj == null) return;
        if (navMeshAgent == null) return;
        if (!navMeshAgent.enabled) return;
        if (!navMeshAgent.isActiveAndEnabled) return;

        //set navmesh agent destination to the chase target
        navMeshAgent.SetDestination(chaseTarget.gameObj.transform.position);
    }

    public void FleeBehaviour()
    {
        //guard clauses
        if (fleeTarget == null) return;
        if (fleeTarget.gameObj == null) return;
        if (navMeshAgent == null) return;
        if (!navMeshAgent.enabled) return;
        if (!navMeshAgent.isActiveAndEnabled) return;

        Vector2 myPosition = new Vector2(transform.position.x, transform.position.z);

        //get the difference from this animal's position and the chasing animal's position
        Vector2 fleeTargetdifference = Vector2.zero;
        fleeTargetdifference.x = fleeTarget.gameObj.transform.position.x - myPosition.x;
        fleeTargetdifference.y = fleeTarget.gameObj.transform.position.z - myPosition.y;

        //get the position to flee to when running from the chasing animal
        Vector2 fleeToPosition = Vector2.zero;
        fleeToPosition.x = myPosition.x - fleeTargetdifference.x;
        fleeToPosition.y = myPosition.y - fleeTargetdifference.y;

        //get the difference from this animal's position and the flee to position
        Vector2 fleeToPositiondifference = fleeToPosition - myPosition;
        //get the sign of the angle the animal will turn
        float sign = (fleeToPosition.y < myPosition.y) ? -1.0f : 1.0f;
        //get the angle the animal will turn, and apply the sign
        float fleeAngle = Vector2.Angle(Vector2.up, fleeToPositiondifference) * sign;

        //calculate the new destination to flee to
        Vector3 newDestination = GetMoveDestination(30, EYESTRENGTH, fleeAngle);
        
        //if the calculated new destination is very close to the current destination, return to not set it
        if (Vector3.Distance(newDestination, navMeshAgent.destination) < 0.5f) return;

        //set the navmesh agent's destination to the new destination
        navMeshAgent.SetDestination(newDestination);
    }
}
