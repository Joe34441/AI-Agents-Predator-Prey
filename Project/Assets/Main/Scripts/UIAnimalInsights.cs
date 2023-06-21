using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class UIAnimalInsights : MonoBehaviour
{
    //information on an animal's vital stats
    [Header("Vitals")]
    [SerializeField] private TextMeshProUGUI vitalsName;
    [SerializeField] private TextMeshProUGUI vitalsText;

    //information on an animal's attributes
    [Header("Attributes")]
    [SerializeField] private TextMeshProUGUI attributesText;

    //type 1 of how to display the animal's behaviour
    [Header("Type 1")]
    [SerializeField] private GameObject treeType1;
    [SerializeField] private AnimalInsightsType1 innerTreeType1;
    //type 2 of how to display the animal's behaviour
    [Header("Type 2")]
    [SerializeField] private GameObject treeType2;
    [SerializeField] private AnimalInsightsType2 innerTreeType2;
    //type 3 of how to display the animal's behaviour
    [Header("Type 3")]
    [SerializeField] private GameObject treeType3;
    [SerializeField] private AnimalInsightsType3 innerTreeType3;

    //if we should update the displayed information
    [HideInInspector] public bool updateInfo = false;

    //update timers
    private float updateTimer;
    private float updateTime = 0.05f;

    //references
    private SimulationManager simulationManager;
    private GameObject currentAnimalObj;
    
    //current displayed animal's ID within the simulation
    private int currentID;
    //type needed to display the current animal
    private int currentType;
    public void SetType(int value) { currentType = value; }


    private void Start()
    {
        //get a reference to the simulation manager
        simulationManager = FindObjectOfType<SimulationManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (updateTimer < updateTime) updateTimer += Time.deltaTime;
        else if (updateInfo)
        {
            updateTimer = 0;
            //checks for the correct animal being displayed
            Animal currentAnimal = null;
            if (currentID < simulationManager.animalsStatus.Count)
            {
                if (currentAnimalObj == null) currentAnimalObj = simulationManager.animalsStatus[currentID].gameObj;

                currentAnimal = simulationManager.animalsStatus[currentID].gameObj.GetComponent<Animal>();
            }

            if (!currentAnimalObj.GetComponent<Animal>().killed)
            {
                //check if the desired animal to view has been changed
                if (currentAnimal.gameObject != currentAnimalObj)
                {
                    currentID = currentAnimalObj.GetComponent<Animal>().GetAnimalIndex();
                    currentAnimal = simulationManager.animalsStatus[currentID].gameObj.GetComponent<Animal>();
                }
            }

            //if we should not be displaying information on this animal
            if (currentAnimal.gameObject != currentAnimalObj)
            {
                //set flags and reset
                currentAnimalObj = null;
                currentID = -1;
                currentType = 0;
                EnableInfo(false);
                GetComponent<UIInfo>().SetCurrentID(currentID);
                GetComponent<UIInfo>().canvasEnabledOverride = false;
                return;
            }

            //get the desired animals attributes and display them
            attributesText.text = "Strength: " + currentAnimal.STRENGTH + "   Vitality: " + currentAnimal.VITALITY + "   Speed: " + currentAnimal.SPEED;
            attributesText.text += "   Eye Strength: " + currentAnimal.EYESTRENGTH + "   Night Survivability: " + currentAnimal.NIGHTSURVIVABILITY;

            //get the desired animals index and name and display them
            vitalsName.text = currentID + " - ";
            if (currentType == 1) vitalsName.text += "Rabbit";
            else if (currentType == 2) vitalsName.text += "Fox";
            else if (currentType == 3) vitalsName.text += "Wolf";

            //get the value and string text of the animals health
            string healthName = null;
            float healthValue = (float)currentAnimal.HEALTH / (float)currentAnimal.maxHealth * 100;
            if (healthValue >= simulationManager.vitals.HealthStats().healthyValue) healthName = simulationManager.vitals.HealthStats().healthyText;
            else if (healthValue >= simulationManager.vitals.HealthStats().woundedValue) healthName = simulationManager.vitals.HealthStats().woundedText;
            else if (healthValue >= simulationManager.vitals.HealthStats().badlyWoundedValue) healthName = simulationManager.vitals.HealthStats().badlyWoundedText;
            else if (healthValue >= simulationManager.vitals.HealthStats().mortallyWoundedValue) healthName = simulationManager.vitals.HealthStats().mortallyWoundedText;
            else if (healthValue >= simulationManager.vitals.HealthStats().deadValue) healthName = simulationManager.vitals.HealthStats().deadText;

            //get the value and string text of the animals hunger
            string hungerName = null;
            float hungerValue = currentAnimal.HUNGER;
            if (hungerValue >= simulationManager.vitals.HungerStats().fullValue) hungerName = simulationManager.vitals.HungerStats().fullText;
            else if (hungerValue >= simulationManager.vitals.HungerStats().satisfiedValue) hungerName = simulationManager.vitals.HungerStats().satisfiedText;
            else if (hungerValue >= simulationManager.vitals.HungerStats().hungryValue) hungerName = simulationManager.vitals.HungerStats().hungryText;
            else if (hungerValue >= simulationManager.vitals.HungerStats().veryHungryValue) hungerName = simulationManager.vitals.HungerStats().veryHungryText;
            else if (hungerValue >= simulationManager.vitals.HungerStats().starvingValue) hungerName = simulationManager.vitals.HungerStats().starvingText;

            //get the value and string text of the animals thirst
            string thirstName = null;
            float thirstValue = currentAnimal.THIRST;
            if (thirstValue >= simulationManager.vitals.ThirstStats().fullValue) thirstName = simulationManager.vitals.ThirstStats().fullText;
            else if (thirstValue >= simulationManager.vitals.ThirstStats().satisfiedValue) thirstName = simulationManager.vitals.ThirstStats().satisfiedText;
            else if (thirstValue >= simulationManager.vitals.ThirstStats().thirstyValue) thirstName = simulationManager.vitals.ThirstStats().thirstyText;
            else if (thirstValue >= simulationManager.vitals.ThirstStats().veryThirstyValue) thirstName = simulationManager.vitals.ThirstStats().veryThirstyText;
            else if (thirstValue >= simulationManager.vitals.ThirstStats().severelyDehdratedValue) thirstName = simulationManager.vitals.ThirstStats().severelyDehydratedText;

            //display the animals vitals information for health, hunger and thirst
            vitalsText.text = "Health: " + currentAnimal.HEALTH + "/" + currentAnimal.maxHealth + "\n" + healthName + "\n\n";
            vitalsText.text += "Hunger: " + currentAnimal.HUNGER + "/100" + "\n" + hungerName + "\n\n";
            vitalsText.text += "Thirst: " + currentAnimal.THIRST + "/100" + "\n" + thirstName;

            //update the UI showing this animals behaviour choices
            UpdateTreeType();
        }
    }

    public void EnableInfo(bool enabled)
    {
        if (currentID >= 0 && currentType > 0) updateInfo = enabled;
        else updateInfo = false;

        //disable all behaviour displays
        treeType1.SetActive(false);
        treeType2.SetActive(false);
        treeType3.SetActive(false);

        if (updateInfo)
        {
            //enable the correct behaviour display
            if (currentType == 1) treeType1.SetActive(true);
            else if (currentType == 2) treeType2.SetActive(true);
            else if (currentType == 3) treeType3.SetActive(true);
        }
    }

    private void SetColor(Color color, TextMeshProUGUI text, GameObject image)
    {
        //set the text colour
        text.color = color;
        //set the image colour
        image.GetComponent<Image>().color = color;
    }
    
    public void SetCurrentID(int value)
    {
        //set the new ID
        currentID = value;
        //check if ID is valid
        if (simulationManager.animalsStatus.Count > currentID) currentAnimalObj = simulationManager.animalsStatus[currentID].gameObj;
    }

    private void UpdateTreeType()
    {
        if (currentType == 1)
        {
            //reference to current animal
            Animal currentAnimal = currentAnimalObj.GetComponent<Animal>();
            //reset the tree
            innerTreeType1.Reset();
            //update the tree
            if (currentAnimal.goToShelter)
            {
                SetColor(Color.green, innerTreeType1.SGTText, innerTreeType1.SGTLine);
                SetColor(Color.green, innerTreeType1.shelterText, innerTreeType1.shelterLine);
                return;
            }
            else
            {
                SetColor(Color.red, innerTreeType1.SGTText, innerTreeType1.SGTLine);
            }

            if (currentAnimal.seekShelter)
            {
                SetColor(Color.green, innerTreeType1.SSText, innerTreeType1.SSLine);
                SetColor(Color.green, innerTreeType1.shelterText, innerTreeType1.shelterLine);
                return;
            }
            else
            {
                SetColor(Color.red, innerTreeType1.SSText, innerTreeType1.SSLine);
                SetColor(Color.red, innerTreeType1.shelterText, innerTreeType1.shelterLine);
            }

            if (!currentAnimal.shouldFlee)
            {
                SetColor(Color.green, innerTreeType1.RSText, innerTreeType1.RSLine);
                SetColor(Color.green, innerTreeType1.runText, innerTreeType1.runLine);
            }

            if (currentAnimal.shouldFlee)
            {
                SetColor(Color.green, innerTreeType1.RFText, innerTreeType1.RFLine);
                SetColor(Color.green, innerTreeType1.runText, innerTreeType1.runLine);
                return;
            }
            else
            {
                SetColor(Color.red, innerTreeType1.RFText, innerTreeType1.RFLine);
            }

            if (currentAnimal.goToFood)
            {
                SetColor(Color.green, innerTreeType1.LFEText, innerTreeType1.LFELine);
                SetColor(Color.green, innerTreeType1.LFText, innerTreeType1.LFLine);
                SetColor(Color.green, innerTreeType1.LifeText, innerTreeType1.LifeLine);
                return;
            }
            else
            {
                SetColor(Color.red, innerTreeType1.LFEText, innerTreeType1.LFELine);
            }

            if (currentAnimal.HUNGER <= simulationManager.vitals.HungerStats().hungryValue)
            {
                SetColor(Color.green, innerTreeType1.LFSText, innerTreeType1.LFSLine);
                SetColor(Color.green, innerTreeType1.LFText, innerTreeType1.LFLine);
                SetColor(Color.green, innerTreeType1.LifeText, innerTreeType1.LifeLine);
                return;
            }
            else
            {
                SetColor(Color.red, innerTreeType1.LFSText, innerTreeType1.LFSLine);
                SetColor(Color.red, innerTreeType1.LFText, innerTreeType1.LFLine);
            }

            if (currentAnimal.goToWater)
            {
                SetColor(Color.green, innerTreeType1.LWDText, innerTreeType1.LWDLine);
                SetColor(Color.green, innerTreeType1.LWText, innerTreeType1.LWLine);
                SetColor(Color.green, innerTreeType1.LifeText, innerTreeType1.LifeLine);
                return;
            }
            else
            {
                SetColor(Color.red, innerTreeType1.LWDText, innerTreeType1.LWDLine);
            }

            if (currentAnimal.THIRST <= simulationManager.vitals.ThirstStats().thirstyValue)
            {
                SetColor(Color.green, innerTreeType1.LWSText, innerTreeType1.LWSLine);
                SetColor(Color.green, innerTreeType1.LWText, innerTreeType1.LWLine);
                SetColor(Color.green, innerTreeType1.LifeText, innerTreeType1.LifeLine);
                return;
            }
            else
            {
                SetColor(Color.red, innerTreeType1.LWSText, innerTreeType1.LWSLine);
                SetColor(Color.red, innerTreeType1.LWText, innerTreeType1.LWLine);
                SetColor(Color.red, innerTreeType1.LifeText, innerTreeType1.LifeLine);
            }
        }
        else if (currentType == 2)
        {
            //reference to the current animal
            Animal currentAnimal = currentAnimalObj.GetComponent<Animal>();
            //reset the tree
            innerTreeType2.Reset();
            //update the tree
            if (currentAnimal.goToShelter)
            {
                SetColor(Color.green, innerTreeType2.SGTText, innerTreeType2.SGTLine);
                SetColor(Color.green, innerTreeType2.shelterText, innerTreeType2.shelterLine);
                return;
            }
            else
            {
                SetColor(Color.red, innerTreeType2.SGTText, innerTreeType2.SGTLine);
            }

            if (currentAnimal.seekShelter)
            {
                SetColor(Color.green, innerTreeType2.SSText, innerTreeType2.SSLine);
                SetColor(Color.green, innerTreeType2.shelterText, innerTreeType2.shelterLine);
                return;
            }
            else
            {
                SetColor(Color.red, innerTreeType2.SSText, innerTreeType2.SSLine);
                SetColor(Color.red, innerTreeType2.shelterText, innerTreeType2.shelterLine);
            }

            if (!currentAnimal.shouldFlee)
            {
                SetColor(Color.green, innerTreeType2.RSText, innerTreeType2.RSLine);
                SetColor(Color.green, innerTreeType2.runText, innerTreeType2.runLine);
            }

            if (currentAnimal.shouldFlee)
            {
                SetColor(Color.green, innerTreeType2.RFText, innerTreeType2.RFLine);
                SetColor(Color.green, innerTreeType2.runText, innerTreeType2.runLine);
                return;
            }
            else
            {
                SetColor(Color.red, innerTreeType2.RFText, innerTreeType2.RFLine);
            }

            if (currentAnimal.goToFood)
            {
                SetColor(Color.green, innerTreeType2.LFEText, innerTreeType2.LFELine);
                SetColor(Color.green, innerTreeType2.LFText, innerTreeType2.LFLine);
                SetColor(Color.green, innerTreeType2.LifeText, innerTreeType2.LifeLine);
                return;
            }
            else
            {
                SetColor(Color.red, innerTreeType2.LFEText, innerTreeType2.LFELine);
            }

            if (currentAnimal.HUNGER <= simulationManager.vitals.HungerStats().satisfiedValue)
            {
                SetColor(Color.green, innerTreeType2.LFSText, innerTreeType2.LFSLine);
                SetColor(Color.green, innerTreeType2.LFText, innerTreeType2.LFLine);
                SetColor(Color.green, innerTreeType2.LifeText, innerTreeType2.LifeLine);
            }
            else
            {
                SetColor(Color.red, innerTreeType2.LFSText, innerTreeType2.LFSLine);
                SetColor(Color.red, innerTreeType2.LFText, innerTreeType2.LFLine);
            }

            if (currentAnimal.goToWater)
            {
                SetColor(Color.green, innerTreeType2.LWDText, innerTreeType2.LWDLine);
                SetColor(Color.green, innerTreeType2.LWText, innerTreeType2.LWLine);
                SetColor(Color.green, innerTreeType2.LifeText, innerTreeType2.LifeLine);
                return;
            }
            else
            {
                SetColor(Color.red, innerTreeType2.LWDText, innerTreeType2.LWDLine);
            }

            if (currentAnimal.THIRST <= simulationManager.vitals.ThirstStats().thirstyValue)
            {
                SetColor(Color.green, innerTreeType2.LWSText, innerTreeType2.LWSLine);
                SetColor(Color.green, innerTreeType2.LWText, innerTreeType2.LWLine);
                SetColor(Color.green, innerTreeType2.LifeText, innerTreeType2.LifeLine);
            }
            else
            {
                SetColor(Color.red, innerTreeType2.LWSText, innerTreeType2.LWSLine);
                SetColor(Color.red, innerTreeType2.LWText, innerTreeType2.LWLine);
                SetColor(Color.red, innerTreeType2.LifeText, innerTreeType2.LifeLine);
            }

            if (currentAnimal.shouldAttack)
            {
                SetColor(Color.green, innerTreeType2.KAText, innerTreeType2.KALine);
                SetColor(Color.green, innerTreeType2.KillText, innerTreeType2.KillLine);
                return;
            }
            else
            {
                SetColor(Color.red, innerTreeType2.KAText, innerTreeType2.KALine);
            }

            if (currentAnimal.shouldChase)
            {
                SetColor(Color.green, innerTreeType2.KCText, innerTreeType2.KCLine);
                SetColor(Color.green, innerTreeType2.KillText, innerTreeType2.KillLine);
                return;
            }
            else
            {
                SetColor(Color.red, innerTreeType2.KCText, innerTreeType2.KCLine);
                SetColor(Color.red, innerTreeType2.KSText, innerTreeType2.KSLine);
                SetColor(Color.red, innerTreeType2.KillText, innerTreeType2.KillLine);
            }

        }
        else if (currentType == 3)
        {
            //reference to the current animal
            Animal currentAnimal = currentAnimalObj.GetComponent<Animal>();
            //reference to the current animal type
            Wolf currentCreature = currentAnimal.GetComponent<Wolf>();

            //if the animal has no actions, return
            if (currentCreature.actions.Count == 0) return;

            //list of key value pairs to copy from animal's action costs dictionary
            List<KeyValuePair<string, float>> orderedActions = new List<KeyValuePair<string, float>>();

            foreach (KeyValuePair<string, float> pair in currentCreature.actions)
            {
                //the key and value of this item
                KeyValuePair<string, float> newPair = new KeyValuePair<string, float>(pair.Key, pair.Value);
                //insert into correct place in list
                if (orderedActions.Count == 0) orderedActions.Add(newPair);
                else if (newPair.Value <= orderedActions[orderedActions.Count - 1].Value) orderedActions.Add(newPair);
                else
                {
                    for (int i = 0; i < orderedActions.Count; ++i)
                    {
                        if (newPair.Value > orderedActions[i].Value)
                        {
                            orderedActions.Insert(i, newPair);
                            break;
                        }
                    }
                }
            }

            //clear text fields
            innerTreeType3.keysText.text = "";
            innerTreeType3.valuesText.text = "";
            innerTreeType3.planText.text = "";

            for (int i = 0; i < orderedActions.Count; ++i)
            {
                //display the action names and costs in descending order
                string value = orderedActions[i].Value.ToString();
                if (value.Length > 7) value = value.Substring(0, 6);
                innerTreeType3.keysText.text += orderedActions[i].Key.ToString() + " :" + "\n";
                innerTreeType3.valuesText.text += " " + value + "\n";
            }

            //display the action plan if available
            if (orderedActions[0].Key == "Chase") innerTreeType3.planText.text = "Chase --> Attack --> Eat";
            else if (orderedActions[0].Key == "Attack") innerTreeType3.planText.text = "Attack --> Eat";
            else if (orderedActions[0].Key == "Shelter") innerTreeType3.planText.text = "Shelter";
            else if (orderedActions[0].Key == "Wander") innerTreeType3.planText.text = "Wander";
            else innerTreeType3.planText.text = orderedActions[0].Key.ToString() + " --> " + orderedActions[1].Key.ToString();
        }
    }
}

[System.Serializable]
public class AnimalInsightsType1
{
    [SerializeField] public TextMeshProUGUI shelterText;
    [SerializeField] public GameObject shelterLine;

    [SerializeField] public TextMeshProUGUI SGTText;
    [SerializeField] public GameObject SGTLine;

    [SerializeField] public TextMeshProUGUI SSText;
    [SerializeField] public GameObject SSLine;

    [SerializeField] public TextMeshProUGUI runText;
    [SerializeField] public GameObject runLine;

    [SerializeField] public TextMeshProUGUI RFText;
    [SerializeField] public GameObject RFLine;

    [SerializeField] public TextMeshProUGUI RSText;
    [SerializeField] public GameObject RSLine;

    [SerializeField] public TextMeshProUGUI LifeText;
    [SerializeField] public GameObject LifeLine;

    [SerializeField] public TextMeshProUGUI LFText;
    [SerializeField] public GameObject LFLine;

    [SerializeField] public TextMeshProUGUI LFEText;
    [SerializeField] public GameObject LFELine;

    [SerializeField] public TextMeshProUGUI LFSText;
    [SerializeField] public GameObject LFSLine;

    [SerializeField] public TextMeshProUGUI LWText;
    [SerializeField] public GameObject LWLine;

    [SerializeField] public TextMeshProUGUI LWDText;
    [SerializeField] public GameObject LWDLine;

    [SerializeField] public TextMeshProUGUI LWSText;
    [SerializeField] public GameObject LWSLine;

    public void Reset()
    {
        shelterText.color = Color.yellow;
        shelterLine.GetComponent<Image>().color = Color.yellow;

        SGTText.color = Color.yellow;
        SGTLine.GetComponent<Image>().color = Color.yellow;

        SSText.color = Color.yellow;
        SSLine.GetComponent<Image>().color = Color.yellow;

        runText.color = Color.yellow;
        runLine.GetComponent<Image>().color = Color.yellow;

        RFText.color = Color.yellow;
        RFLine.GetComponent<Image>().color = Color.yellow;

        RSText.color = Color.yellow;
        RSLine.GetComponent<Image>().color = Color.yellow;

        LifeText.color = Color.yellow;
        LifeLine.GetComponent<Image>().color = Color.yellow;

        LFText.color = Color.yellow;
        LFLine.GetComponent<Image>().color = Color.yellow;

        LFEText.color = Color.yellow;
        LFELine.GetComponent<Image>().color = Color.yellow;

        LFSText.color = Color.yellow;
        LFSLine.GetComponent<Image>().color = Color.yellow;

        LWText.color = Color.yellow;
        LWLine.GetComponent<Image>().color = Color.yellow;

        LWDText.color = Color.yellow;
        LWDLine.GetComponent<Image>().color = Color.yellow;

        LWSText.color = Color.yellow;
        LWSLine.GetComponent<Image>().color = Color.yellow;
    }
}

[System.Serializable]
public class AnimalInsightsType2
{
    [SerializeField] public TextMeshProUGUI shelterText;
    [SerializeField] public GameObject shelterLine;

    [SerializeField] public TextMeshProUGUI SGTText;
    [SerializeField] public GameObject SGTLine;

    [SerializeField] public TextMeshProUGUI SSText;
    [SerializeField] public GameObject SSLine;

    [SerializeField] public TextMeshProUGUI runText;
    [SerializeField] public GameObject runLine;

    [SerializeField] public TextMeshProUGUI RFText;
    [SerializeField] public GameObject RFLine;

    [SerializeField] public TextMeshProUGUI RSText;
    [SerializeField] public GameObject RSLine;

    [SerializeField] public TextMeshProUGUI LifeText;
    [SerializeField] public GameObject LifeLine;

    [SerializeField] public TextMeshProUGUI LFText;
    [SerializeField] public GameObject LFLine;

    [SerializeField] public TextMeshProUGUI LFEText;
    [SerializeField] public GameObject LFELine;

    [SerializeField] public TextMeshProUGUI LFSText;
    [SerializeField] public GameObject LFSLine;

    [SerializeField] public TextMeshProUGUI LWText;
    [SerializeField] public GameObject LWLine;

    [SerializeField] public TextMeshProUGUI LWDText;
    [SerializeField] public GameObject LWDLine;

    [SerializeField] public TextMeshProUGUI LWSText;
    [SerializeField] public GameObject LWSLine;

    [SerializeField] public TextMeshProUGUI KillText;
    [SerializeField] public GameObject KillLine;

    [SerializeField] public TextMeshProUGUI KAText;
    [SerializeField] public GameObject KALine;

    [SerializeField] public TextMeshProUGUI KCText;
    [SerializeField] public GameObject KCLine;

    [SerializeField] public TextMeshProUGUI KSText;
    [SerializeField] public GameObject KSLine;

    public void Reset()
    {
        shelterText.color = Color.yellow;
        shelterLine.GetComponent<Image>().color = Color.yellow;

        SGTText.color = Color.yellow;
        SGTLine.GetComponent<Image>().color = Color.yellow;

        SSText.color = Color.yellow;
        SSLine.GetComponent<Image>().color = Color.yellow;

        runText.color = Color.yellow;
        runLine.GetComponent<Image>().color = Color.yellow;

        RFText.color = Color.yellow;
        RFLine.GetComponent<Image>().color = Color.yellow;

        RSText.color = Color.yellow;
        RSLine.GetComponent<Image>().color = Color.yellow;

        LifeText.color = Color.yellow;
        LifeLine.GetComponent<Image>().color = Color.yellow;

        LFText.color = Color.yellow;
        LFLine.GetComponent<Image>().color = Color.yellow;

        LFEText.color = Color.yellow;
        LFELine.GetComponent<Image>().color = Color.yellow;

        LFSText.color = Color.yellow;
        LFSLine.GetComponent<Image>().color = Color.yellow;

        LWText.color = Color.yellow;
        LWLine.GetComponent<Image>().color = Color.yellow;

        LWDText.color = Color.yellow;
        LWDLine.GetComponent<Image>().color = Color.yellow;

        LWSText.color = Color.yellow;
        LWSLine.GetComponent<Image>().color = Color.yellow;

        KillText.color = Color.yellow;
        KillLine.GetComponent<Image>().color = Color.yellow;

        KAText.color = Color.yellow;
        KALine.GetComponent<Image>().color = Color.yellow;

        KCText.color = Color.yellow;
        KCLine.GetComponent<Image>().color = Color.yellow;

        KSText.color = Color.yellow;
        KSLine.GetComponent<Image>().color = Color.yellow;
    }
}

[System.Serializable]
public class AnimalInsightsType3
{
    [SerializeField] public TextMeshProUGUI keysText;
    [SerializeField] public TextMeshProUGUI valuesText;
    [SerializeField] public TextMeshProUGUI planText;
}