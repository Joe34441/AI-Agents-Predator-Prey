using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Fox : Animal
{
    //this animal type
    [SerializeField] private AnimalTypes animalType = AnimalTypes.Fox;

    private bool isReady = false;

    //timers
    private float updateTimer = 0;
    private float updateWaitTime = 0.05f;
    private float timersUpdateAmount = 0;

    private float wanderTimer = 0;
    private float wanderWaitTime = 0;

    private float lookForAttackersTimer = 0;
    private float lookForAttackersWaitTime = 0;

    private float lookForFoodtimer = 0;
    private float lookForFoodWaitTime = 0;

    private float lookForWaterTimer = 0;
    private float lookForWaterWaitTime = 0;

    private float attackCooldownTimer = 0;
    private float attackCooldownTime = 0.5f;

    private float eatingTimer = 0;
    private float eatingTime = 1.5f;

    private float hungerTimer = 0;
    private float hungerTime = 0;

    private float drinkingTimer = 0;
    private float drinkingTime = 0;

    private float thirstTimer = 0;
    private float thirstTime = 0;

    private float updateHealthTimer = 0;
    private float updateHealthTime = 1.5f;

    private float seekShelterTimer = 0;
    private float seekShelterTime = 0;

    private void Awake()
    {
        //references
        simulationManager = FindObjectOfType<SimulationManager>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        housing = gameObject.transform.GetChild(0).gameObject;

        //range this animal can attack from
        attackRange = 1;
        //food objects this animal drops when killed
        foodValue = 4;

        //attributes scales
        BASEATTRIBUTESAMOUNT = 35;
        BASEATTRIBUTESRANGE = 15;
        MAXBASEATTRIBUTEAMOUNT = 50;
        MAXATTRIBUTEVALUE = 15;
    }

    public void Setup()
    {
        //randomize attribute amount
        RandomizeAttributeAmount(BASEATTRIBUTESAMOUNT, BASEATTRIBUTESRANGE);
        //randomize attributes
        RandomizeAttributes(animalType);

        //setup using attributes
        HEALTH = VITALITY * 3;
        maxHealth = VITALITY * 4;
        HUNGER = 75;
        THIRST = 75;
        navMeshAgent.speed = (float)SPEED / 4;
        navMeshAgent.acceleration = SPEED;
        kill = false;

        //animal is ready to start
        isReady = true;
    }

    // Update is called once per frame
    void Update()
    {
        //check if animal should not update
        if (!isReady) return;
        if (pauseAnimal) return;

        if (startWaiting)
        {
            if (Time.timeSinceLevelLoad >= startWaitTime) startWaiting = false;
            return;
        }

        //update animations every frame
        UpdateAnimalAnimations();

        //main update timer
        if (updateTimer > updateWaitTime) updateTimer = 0;
        else
        {
            updateTimer += Time.deltaTime;
            return;
        }

        timersUpdateAmount = Time.deltaTime + updateWaitTime;

        //check if animal should be dead
        if (CheckDeath()) Kill(animalType);
        if (killed) return;

        //check where we are in the day
        CheckCycleStage();

        //check if the day has finished
        if (dayCycleOver) DayFinished(animalType);
        if (killed) return;

        if (isInsideShelter) return;

        if (seekShelter && !goToShelter)
        {
            if (seekShelterTimer < seekShelterTime) seekShelterTimer += timersUpdateAmount;
            else
            {
                seekShelterTimer = 0;
                seekShelterTime = Random.Range(0.1f, 0.15f);
                //look for caves to stay in overnight
                LookForShelter(animalType);
            }

            if (!goToShelter)
            {
                if (wanderTimer < wanderWaitTime) wanderTimer += timersUpdateAmount;
                else
                {
                    wanderTimer = 0;
                    wanderWaitTime = Random.Range(0.05f, 0.1f);
                    //wander around the environment
                    WanderBehaviour();
                }
            }
        }

        //check if animal should go to shelter
        if (goToShelter) GoToShelter(animalType);

        //if the animal has eaten food
        if (eaten)
        {
            HUNGER += 50;
            HUNGER = Mathf.Clamp(HUNGER, 0, 100);

            eaten = false;
            eating = true;
        }

        //if the animal has drank water
        if (drank)
        {
            //time to drink depends on how thirsty the animal is
            drinkingTime = (float)(100 - THIRST) / 15.0f;
            THIRST = 100;
            drank = false;
            drinking = true;
        }

        if (hungerTimer < hungerTime) hungerTimer += timersUpdateAmount;
        else
        {
            hungerTimer = 0;
            hungerTime = Random.Range(0.7f, 0.9f);
            //time to decrement hunger value
            HUNGER--;
            HUNGER = Mathf.Clamp(HUNGER, 0, 100);
        }

        if (thirstTimer < thirstTime) thirstTimer += timersUpdateAmount;
        else
        {
            thirstTimer = 0;
            thirstTime = Random.Range(0.5f, 0.7f);
            //time to decrement thirst value
            THIRST--;
            THIRST = Mathf.Clamp(THIRST, 0, 100);
        }

        if (updateHealthTimer < updateHealthTime) updateHealthTimer += timersUpdateAmount;
        else
        {
            updateHealthTimer = 0;
            //increase or decrease health based on hunger value
            if (HUNGER >= simulationManager.vitals.HungerStats().fullValue) HEALTH += 3;
            else if (HUNGER >= simulationManager.vitals.HungerStats().satisfiedValue) HEALTH += 2;
            else if (HUNGER <= simulationManager.vitals.HungerStats().starvingValue) HEALTH -= 2;
            else if (HUNGER <= simulationManager.vitals.HungerStats().veryHungryValue) HEALTH--;
            //increase or decrease thirst based on thirst value
            if (THIRST >= simulationManager.vitals.ThirstStats().fullValue) HEALTH += 3;
            else if (THIRST >= simulationManager.vitals.ThirstStats().satisfiedValue) HEALTH++;
            else if (THIRST <= simulationManager.vitals.ThirstStats().severelyDehdratedValue) HEALTH -= 2;
            else if (THIRST <= simulationManager.vitals.ThirstStats().veryThirstyValue) HEALTH--;
        }

        //check if animal should be dead
        if (CheckDeath()) Kill(animalType);
        if (killed) return;

        //if the animal is eating
        if (eating)
        {
            if (eatingTimer < eatingTime)
            {
                //animal is still eating
                eatingTimer += timersUpdateAmount;
                return;
            }
            else
            {
                //animal has finished eating
                eatingTimer = 0;
                eating = false;
            }
        }

        //if the animal is drinking
        if (drinking)
        {
            if (drinkingTimer < drinkingTime)
            {
                //animal is still drinking
                drinkingTimer += timersUpdateAmount;
                return;
            }
            else
            {
                //animal has finished drinking
                drinkingTimer = 0;
                drinking = false;
            }
        }

        if (lookForAttackersTimer < lookForAttackersWaitTime) lookForAttackersTimer += timersUpdateAmount;
        else
        {
            lookForAttackersTimer = 0;
            lookForAttackersWaitTime = Random.Range(0.1f, 0.25f);
            //look for potential attackers
            LookForAttackers(animalType);
        }

        if (shouldFlee)
        {
            //flee from attacker
            FleeBehaviour();
            return;
        }

        if (THIRST <= simulationManager.vitals.ThirstStats().thirstyValue)
        {
            if (lookForWaterTimer < lookForWaterWaitTime) lookForWaterTimer += timersUpdateAmount;
            else
            {
                lookForWaterTimer = 0;

                if (THIRST <= simulationManager.vitals.ThirstStats().veryThirstyValue) lookForWaterWaitTime = 0.05f;
                else lookForWaterWaitTime = Random.Range(0.1f, 0.2f);
                //look for water objects
                LookForWater();
            }
        }

        if (goToWater)
        {
            //go to the water source
            GoToWater();
            return;
        }

        if (HUNGER < simulationManager.vitals.HungerStats().satisfiedValue)
        {
            if (lookForFoodtimer < lookForFoodWaitTime) lookForFoodtimer += timersUpdateAmount;
            else
            {
                lookForFoodtimer = 0;

                if (HUNGER < simulationManager.vitals.HungerStats().hungryValue) lookForFoodWaitTime = 0.05f;
                else lookForFoodWaitTime = Random.Range(0.1f, 0.2f);

                //look for food objects
                LookForFood(Food.FoodTypes.Meat);
                //look for animals to chase and kill
                LookForTargets(animalType);
            }
        }

        if (goToFood)
        {
            //go to food object
            GoToFood();
            return;
        }

        if (attackCooldownTimer <= attackCooldownTime) attackCooldownTimer += timersUpdateAmount;
        else
        {
            //check if this animal can attack
            CheckCanAttack();

            if (shouldAttack)
            {
                attackCooldownTimer = 0;
                //attack the target animal
                Attack();
            }
        }
        
        if (shouldChase)
        {
            //chase the target animal
            ChaseBehaviour();
            return;
        }

        if (wanderTimer < wanderWaitTime) wanderTimer += timersUpdateAmount;
        else
        {
            wanderTimer = 0;

            if (HUNGER < simulationManager.vitals.HungerStats().hungryValue || THIRST < simulationManager.vitals.ThirstStats().thirstyValue) wanderWaitTime = 0.5f;
            else wanderWaitTime = Random.Range(7, 12);
            //wander around the environment
            WanderBehaviour();
        }
    }
}
