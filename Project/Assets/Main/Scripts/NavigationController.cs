using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavigationController : MonoBehaviour
{
    [SerializeField] private GameObject destination;

    private NavMeshAgent agent;

    public void StopAgent()
    {
        agent.isStopped = true;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!destination) return;

        agent.destination = destination.transform.position;
    }
}
