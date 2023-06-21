using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


//########## TASK SUMMARY ####################################################################################
//## 1 - Agents are either Non-infected or Infected (Red). The simulation starts with one infected agent. ####
//## 2 - Non-infected wander around randomly. Infected agents chase the nearest non-infected agent.       ####
//## 3 - When within a certain range infected agents “infect” a non-infected agent.                       ####
//## 4 - Once infected, an agent will stop for a short amount of time and then turn red.                  ####
//## 5 - Non-infected agents flee from the closest infected if they get within a certain range.           ####
//############################################################################################################

public class AgentController : MonoBehaviour
{
    [SerializeField] private Material humanMaterial;
    [SerializeField] private Material zombieMaterial;

    [SerializeField] private GameObject body;

    private Vector3 movePositionDestination;

    private NavMeshAgent navMeshAgent;

    private OldSimulationManager _manager;

    private bool shouldMove;

    private bool infected;

    private bool newInfection;
    private float newInfectionTimer;
    private float newInfectionWaitTime = 2.0f;

    private float minTotalWaitTime = 1.0f;
    private float maxTotalWaitTime = 3.0f;
    private float totalWaitTime = 2.0f;
    private float currentTime;

    [SerializeField] private float maxwanderingTime = 7.0f;
    [SerializeField] private float totalWanderingTime;

    [SerializeField] private bool finished = true;

    private float moveDistance;

    private List<AgentController> nearbyAgents = new List<AgentController>();
    private AgentController chaseTarget;

    private enum States
    {
        Wander,
        Flee,
        Chase
    }

    private States currentState = States.Wander;


    private void RandomiseWanderDistance(float min, float max) { moveDistance = Random.Range(min, max); }
    public float GetMoveDistance() { return moveDistance; }
    public Vector3 GetMovePosition() { return movePositionDestination; }
    public void SetMovePosition(Vector3 newPos) { movePositionDestination = newPos; }
    public void SetShouldMove(bool value) { shouldMove = value; }
    public bool ShouldMove() { return shouldMove; }
    public void Infect() { if (!infected) InfectAgent(); }
    public bool IsInfected() { return infected; }

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();

        _manager = FindObjectOfType<OldSimulationManager>();

        shouldMove = false;

        movePositionDestination = gameObject.transform.position;

        Application.targetFrameRate = 144;
    }

    // Update is called once per frame
    void Update()
    {

        //Debug.Log(1 / Time.deltaTime);

        if (newInfection)
        {
            if (newInfectionTimer >= newInfectionWaitTime) newInfection = false;
            else
            {
                newInfectionTimer += Time.deltaTime;
                navMeshAgent.destination = transform.position;
            }
        }
        else newInfectionTimer = 0.0f;


        switch (currentState)
        {
            case States.Wander:
                Wander();
                return;

            case States.Flee:
                Flee();
                Wander();
                return;

            case States.Chase:
                Chase();
                //Wander();
                return;
        }
    }

    private void Wander()
    {
        if (finished)
        {
            currentTime += Time.deltaTime;
        }
        else if (Vector3.Distance(navMeshAgent.gameObject.transform.position, movePositionDestination) <= 0.1f)
        {
            finished = true;
        }
        else if (Mathf.Abs(navMeshAgent.velocity.x) <= 0.01f &&
            Mathf.Abs(navMeshAgent.velocity.y) <= 0.01f &&
            Mathf.Abs(navMeshAgent.velocity.z) <= 0.01f)
        {
            finished = true;
        }
        else if (!finished)
        {
            if (totalWanderingTime >= maxwanderingTime)
            {
                finished = true;
                totalWanderingTime = 0.0f;
            }
            else
            {
                totalWanderingTime += Time.deltaTime;
            }
        }


        //int number = FindObjectOfType<SimulationManager>().GetHumanValues().a;
        float number = _manager.GetHumanValues().moveDistance;
        //Debug.Log(number);

        if (currentTime >= totalWaitTime)
        {
            RandomiseWanderDistance(1.0f, 5.0f);

            Vector3 currentRotation = gameObject.transform.eulerAngles;

            int currentY = Mathf.RoundToInt(currentRotation.y);
            int newY = Random.Range(currentY - 45, currentY + 45); //get new rotation in 90 degree segment agent is facing

            Vector3 newRototation = new Vector3(currentRotation.x, newY, currentRotation.z);
            transform.eulerAngles = newRototation;

            movePositionDestination = transform.position + transform.forward * moveDistance;


            if (!GetSamplePosition())
            {
                bool goLeft = false;
                if (Random.Range(1, 2) == 1) goLeft = true;

                if (goLeft) newY = Random.Range(currentY - 135, currentY - 45); //90 degree segment directly left
                else newY = Random.Range(currentY + 45, currentY + 135); //90 degree segment directly right

                newRototation = new Vector3(currentRotation.x, newY, currentRotation.z);
                transform.eulerAngles = newRototation;
                movePositionDestination = transform.position + transform.forward * moveDistance;

                if (!GetSamplePosition())
                {
                    goLeft = !goLeft;

                    if (goLeft) newY = Random.Range(currentY - 135, currentY - 45); //90 degree segment directly left
                    else newY = Random.Range(currentY + 45, currentY + 135); //90 degree segment directly right

                    newRototation = new Vector3(currentRotation.x, newY, currentRotation.z);
                    transform.eulerAngles = newRototation;
                    movePositionDestination = transform.position + transform.forward * moveDistance;

                    if (!GetSamplePosition())
                    {
                        movePositionDestination = transform.position - transform.forward * moveDistance; //directly behind agent
                        if (!GetSamplePosition())
                        {
                            Debug.Log("Valid destination not found, retrying");
                            return;
                        }
                    }
                }
            }

            finished = false;
            currentTime = 0.0f;
            totalWaitTime = Random.Range(minTotalWaitTime, maxTotalWaitTime);
        }
    }



    private void Flee()
    {

    }

    private void Chase()
    {
        if (nearbyAgents.Count == 0)
        {
            currentState = States.Wander;
            return;
        }

        bool CanChase = true;

        List<AgentController> orderedList = new List<AgentController>();

        foreach (AgentController agent in nearbyAgents)
        {
            if (orderedList.Count == 0)
            {
                orderedList.Add(agent);
                continue;
            }

            for (int i = 0; i < orderedList.Count; i++)
            {
                if (Vector3.Distance(agent.transform.position, transform.position) <= Vector3.Distance(orderedList[i].transform.position, transform.position))
                {
                    orderedList.Insert(i, agent);
                    break;
                }
            }
        }

        for (int i = 0; i < orderedList.Count; i++)
        {
            RaycastHit[] hits;
            Vector3 destination = orderedList[i].transform.position;
            hits = Physics.RaycastAll(transform.position, (destination - transform.forward), Vector3.Distance(destination, transform.position));

            foreach (RaycastHit hit in hits)
            {
                if (!hit.transform.gameObject.GetComponent<AgentController>()) CanChase = false;
                //if NOT ((hit object IS an infected agent) OR (hit object IS the target object)), dont chase
                else if (!(hit.transform.gameObject.GetComponent<AgentController>().IsInfected() || hit.transform == orderedList[i])) CanChase = false;
            }

            if (CanChase)
            {
                chaseTarget = orderedList[i];
                continue;
            }
        }

        if (chaseTarget == null)
        {
            currentState = States.Wander;
            return;
        }

        //run this every frame
        if (CanChase)
        {
            navMeshAgent.destination = chaseTarget.transform.position;

            if (Vector3.Distance(navMeshAgent.transform.position, chaseTarget.transform.position) <= 0.75f)
            {
                chaseTarget.Infect();
                nearbyAgents.Remove(chaseTarget);

                chaseTarget = null;

                newInfection = true;
            }
        }
        else
        {
            currentState = States.Wander;
        }
    }

    private bool GetSamplePosition()
    {
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(movePositionDestination, out navHit, 2.75f, NavMesh.AllAreas))
        {
            movePositionDestination = navHit.position;
            navMeshAgent.destination = movePositionDestination;

            shouldMove = true;

            return true;
        }

        return false;
    }

    private void InfectAgent()
    {
        infected = true;
        newInfection = true;

        body.GetComponent<Renderer>().material = zombieMaterial;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Agent")) return;

        if (infected == other.GetComponent<AgentController>().IsInfected()) return;


        Vector3 direction = other.transform.position - transform.position;
        float distance = Vector3.Distance(other.transform.position, transform.position);

        RaycastHit rayHit;
        if (Physics.Raycast(transform.position, direction, out rayHit, distance))
        {
            if (rayHit.transform.gameObject == other.gameObject)
            {
                nearbyAgents.Add(other.GetComponent<AgentController>());

                if (infected) currentState = States.Chase;
                else if (!infected) currentState = States.Flee;

                Debug.LogFormat("infected {0} is now in {1}", infected, currentState);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("Agent")) return;

        foreach (AgentController agent in nearbyAgents)
        {
            if (other.GetComponent<AgentController>() == agent)
            {
                nearbyAgents.Remove(agent);
                return;
            }
        }
    }
}
