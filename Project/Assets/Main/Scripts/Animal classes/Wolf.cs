using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public class Wolf : Animal
{
    //animal type
    [SerializeField] private AnimalTypes animalType = AnimalTypes.Wolf;

    private bool isReady = false;

    public Dictionary<string, float> actions = new Dictionary<string, float>(7);
    
    //action costs
    [SerializeField] private float shelterCost;
    [SerializeField] private float fleeCost;
    [SerializeField] private float chaseCost;
    [SerializeField] private float attackCost;
    [SerializeField] private float eatCost;
    [SerializeField] private float drinkCost;
    [SerializeField] private float wanderCost;

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
    private float attackCooldownTime = 0.75f;

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
        foodValue = 7;

        //attributes scales
        BASEATTRIBUTESAMOUNT = 50;
        BASEATTRIBUTESRANGE = 20;
        MAXBASEATTRIBUTEAMOUNT = 70;
        MAXATTRIBUTEVALUE = 20;
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

        if (dayCycleOver)
        {
            //the day has finished
            DayFinished(animalType);
            shelterCost = 0;
        }

        if (killed) return;

        if (isInsideShelter) return;

        //if the animal has eaten food
        if (eaten)
        {
            HUNGER += 25;
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
            hungerTime = Random.Range(0.3f, 0.5f);
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
            if (THIRST >= simulationManager.vitals.ThirstStats().satisfiedValue) HEALTH += 3;
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

        if (attackCooldownTimer <= attackCooldownTime) attackCooldownTimer += timersUpdateAmount;
        else
        {
            attackCooldownTimer = 0;
            //check if this animal can attack
            CheckCanAttack();
        }

        if (lookForFoodtimer < lookForFoodWaitTime) lookForFoodtimer += timersUpdateAmount;
        else
        {
            lookForFoodtimer = 0;

            if (HUNGER < simulationManager.vitals.HungerStats().veryHungryValue) lookForFoodWaitTime = 0.05f;
            else lookForFoodWaitTime = Random.Range(0.1f, 0.2f);

            //look for food objects
            LookForFood(Food.FoodTypes.Meat);
            //look for animals to chase and kill
            LookForTargets(animalType);
        }

        if (lookForWaterTimer < lookForWaterWaitTime) lookForWaterTimer += timersUpdateAmount;
        else
        {
            lookForWaterTimer = 0;

            if (THIRST <= simulationManager.vitals.ThirstStats().veryThirstyValue) lookForWaterWaitTime = 0.05f;
            else lookForWaterWaitTime = Random.Range(0.1f, 0.2f);
            //look for water objects
            LookForWater();
        }


        if (wanderTimer < wanderWaitTime) wanderTimer += timersUpdateAmount;
        else
        {
            wanderTimer = 0;
            //wander time delay based on hunger and thirst
            if (HUNGER < simulationManager.vitals.HungerStats().satisfiedValue || THIRST < simulationManager.vitals.ThirstStats().satisfiedValue) wanderWaitTime = 0.5f;
            else wanderWaitTime = Random.Range(7, 12);
        }

        //dictionary of actions and their values
        Dictionary<string, float> newActions = new Dictionary<string, float>();
        float actionCost;

        //shelter cost
        actionCost = CalculateShelterCost(shelterCost);
        shelterCost = actionCost;
        newActions.Add("Shelter", shelterCost);

        //flee cost
        actionCost = CalculateFleeCost();
        fleeCost = actionCost;
        newActions.Add("Flee", fleeCost);

        //chase cost
        actionCost = CalculateChaseCost();
        chaseCost = actionCost;
        newActions.Add("Chase", chaseCost);

        //attack cost
        actionCost = CalculateAttackCost(chaseCost);
        attackCost = actionCost;
        newActions.Add("Attack", attackCost);

        //eat cost
        actionCost = CalculateEatCost();
        eatCost = actionCost;
        newActions.Add("Eat", eatCost);

        //drink cost
        actionCost = CalculateDrinkCost();
        drinkCost = actionCost;
        newActions.Add("Drink", drinkCost);

        //wander cost
        actionCost = CalculateWanderCost();
        wanderCost = actionCost;
        newActions.Add("Wander", wanderCost);

        //order dictionary by ascending values
        actions = newActions.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

        //switch the last (highest) value's key, perform it's corresponding action
        switch (actions.Last().Key)
        {
            case "Shelter":
                if (!goToShelter)
                {
                    if (seekShelterTimer < seekShelterTime) seekShelterTimer += timersUpdateAmount;
                    else
                    {
                        seekShelterTimer = 0;
                        seekShelterTime = Random.Range(0.1f, 0.15f);

                        LookForShelter(animalType);
                    }

                    if (!goToShelter)
                    {
                        if (wanderTimer < wanderWaitTime) wanderTimer += timersUpdateAmount;
                        else
                        {
                            wanderTimer = 0;
                            wanderWaitTime = Random.Range(0.05f, 0.1f);

                            WanderBehaviour();
                        }
                    }
                }
                if (goToShelter) GoToShelter(animalType);
                break;

            case "Flee":
                FleeBehaviour();
                break;

            case "Chase":
                ChaseBehaviour();
                break;

            case "Attack":
                if (shouldAttack) Attack();
                break;

            case "Eat":
                GoToFood();
                break;

            case "Drink":
                GoToWater();
                break;

            case "Wander":
                WanderBehaviour();
                break;
        }
    }
}
