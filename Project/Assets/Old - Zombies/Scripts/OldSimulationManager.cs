using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class OldSimulationManager : MonoBehaviour
{
    [SerializeField] private GameObject AgentPrefab;

    [SerializeField] private GameObject humanSpawns;
    [SerializeField] private GameObject zombieSpawns;

    private List<Transform> humans = new List<Transform>();
    private List<Transform> zombies = new List<Transform>();

    public void AddHuman(Transform trans) { humans.Add(trans); }
    public void AddZombie(Transform trans) { zombies.Add(trans); }

    public struct AgentValues
    {
        //zombies will have more speed
        public NavMeshAgent abc;
        public float moveDistance;
    }

    private AgentValues _Human;
    private AgentValues _Zombie;

    public AgentValues GetHumanValues() { return _Human; }
    public AgentValues GetZombieValues() { return _Zombie; }

    public void RandomiseWanderDistance(float min, float max) { _Human.moveDistance = Random.Range(min, max); }

    public List<Transform> FindZombiesInRange(AgentController agent, float distance)
    {
        List<Transform> result = new List<Transform>();

        foreach (Transform trans in zombies)
        {
            if (Vector3.Distance(agent.transform.position, trans.position) <= distance)
            {
                result.Add(trans);
            }
        }

        return result;
    }

    // Start is called before the first frame update
    void Start()
    {
        _Human.moveDistance = 3;
        _Zombie.abc = null;
    }

    // Update is called once per frame
    void Update()
    {

    }
}

public class Human //maybe remove
{

}


public class Zombie //maybe remove
{

}

public class Navigation
{

}