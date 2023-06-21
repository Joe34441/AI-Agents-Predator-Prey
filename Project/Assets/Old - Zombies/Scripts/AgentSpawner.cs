using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentSpawner : MonoBehaviour
{
    [SerializeField] private GameObject AgentPrefab;
    [SerializeField] private Transform agentParent;

    [Header("Humans")]
    [SerializeField] private GameObject humanSpawnPoint; //change to array, go through each MeshRenderer in array
    [SerializeField] private bool limitHumanSpawns;
    [SerializeField] private int maxHumanSpawns;

    [Header("Zombies")]
    [SerializeField] private GameObject zombieSpawnPoint; //change to array, go through each MeshRenderer in array
    [SerializeField] private bool limitZombieSpawns;
    [SerializeField] private int maxZombieSpawns;


    private OldSimulationManager simulationManager;

    private int spawnCount = 0;
    private float spawnOffset = 1.5f;
    private float startPosOffset = 2.5f;

    private bool spawnInfected;

    private void Awake()
    {
        simulationManager = FindObjectOfType<OldSimulationManager>();
    }

    // Start is called before the first frame update
    void Start()
    {
        MeshRenderer meshRenderer = humanSpawnPoint.GetComponent<MeshRenderer>();
        spawnInfected = false;
        spawnCount = 0;
        SpawnAgents(meshRenderer);

        meshRenderer = zombieSpawnPoint.GetComponent<MeshRenderer>();
        spawnInfected = true;
        spawnCount = 0;
        SpawnAgents(meshRenderer);

        humanSpawnPoint.SetActive(false);
        zombieSpawnPoint.SetActive(false);
    }


    private void SpawnAgents(MeshRenderer meshRenderer)
    {

        float agentXExtent = AgentPrefab.GetComponentInChildren<MeshRenderer>().bounds.size.x;
        float agentZExtent = AgentPrefab.GetComponentInChildren<MeshRenderer>().bounds.size.z;

        int xMax = Mathf.RoundToInt(meshRenderer.bounds.size.x / (agentXExtent * spawnOffset));
        int zMax = Mathf.RoundToInt(meshRenderer.bounds.size.z / (agentZExtent * spawnOffset));

        Vector3 startPos = new Vector3(meshRenderer.bounds.center.x - (meshRenderer.bounds.extents.x / spawnOffset), 1.0f, meshRenderer.bounds.center.z - (meshRenderer.bounds.extents.z / spawnOffset));
        startPos.x -= (agentXExtent * startPosOffset);
        startPos.z -= (agentZExtent * startPosOffset);

        for (int x = 0; x < xMax; x++)
        {
            for (int z = 0; z < zMax; z++)
            {
                if (!spawnInfected && limitHumanSpawns)
                {
                    spawnCount++;
                    if (spawnCount > maxHumanSpawns) return;
                }
                else if (spawnInfected && limitZombieSpawns)
                {
                    spawnCount++;
                    if (spawnCount > maxZombieSpawns) return;
                }

                Vector3 spawnPos = new Vector3(startPos.x + (x * agentXExtent * spawnOffset), startPos.y, startPos.z + (z * agentZExtent * spawnOffset));
                Quaternion rotation = Quaternion.Euler(0.0f, Random.Range(0, 360), 0.0f);

                GameObject agent = Instantiate(AgentPrefab, spawnPos, rotation, agentParent);

                if (!spawnInfected)
                {
                    simulationManager.AddHuman(agent.transform);
                }
                else
                {
                    agent.GetComponent<AgentController>().Infect();
                    simulationManager.AddZombie(agent.transform);
                    //increase speed
                }
            }
        }
    }
}
