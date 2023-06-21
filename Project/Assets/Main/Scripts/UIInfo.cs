using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIInfo : MonoBehaviour
{
    //how much the sky objects will rotate per hour
    private const float DAYINCREMENT = 360 / 24;

    //references
    [SerializeField] private SimulationManager simulationManager;
    [HideInInspector] private UIAnimalInsights uIAnimalInsights;

    //canvas and relevant UI elements
    [SerializeField] private GameObject canvasObj;
    [SerializeField] private GameObject attributesObj;
    [SerializeField] private GameObject vitalsObj;
    [HideInInspector] public bool canvasEnabledOverride = false;
    private bool canvasEnabled = true;

    //sun, moon and stars objects
    [SerializeField] private GameObject sunLight;
    [SerializeField] private GameObject moon;
    [SerializeField] private GameObject stars;

    //the current time
    [SerializeField] private TextMeshProUGUI timeText;

    //information of the previous day
    [SerializeField] private TextMeshProUGUI generalCurrentDayText;
    [SerializeField] private TextMeshProUGUI generalLastDayText;

    //how quickly the day will progress
    [SerializeField] private float baseDayTimeScale = 0.9f; //0.6 = 1 day in 1 min

    //day timers
    private float dayTimer = 0;
    private float dayTimeScale;
    //base text fields text
    private string baseText = "Day ";
    private string baseDailyText = " Time - ";
    //mins/hours/days counters
    private int daysCount = 0;
    private int hoursCount = 0;
    private int minsCount = 0;
    //animals counters
    private int wolvesCount = 0;
    private int foxesCount = 0;
    private int rabbitsCount = 0;
    //stats update timer
    private float generalStatsUpdateTimer = 0;
    private float generalStatsUpdateTime = 0.25f;
    //ID of the current animal
    private int currentID = -1;
    public void SetCurrentID(int value) { currentID = value; }

    //pausing the simulation
    [HideInInspector] public bool stopDayNightCycle = false;


    private void Start()
    {
        //get day timescale
        dayTimeScale = baseDayTimeScale;
        //get reference to animal insights class
        uIAnimalInsights = GetComponent<UIAnimalInsights>();
    }

    // Update is called once per frame
    void Update()
    {
        //toggle animal UI info with space
        if (Input.GetKeyDown(KeyCode.Space)) canvasEnabledOverride = !canvasEnabledOverride;

        if (canvasEnabledOverride != canvasEnabled)
        {
            canvasEnabled = canvasEnabledOverride;

            if (currentID >= 0)
            {
                //update is active or not
                attributesObj.SetActive(canvasEnabled);
                vitalsObj.SetActive(canvasEnabled);
                //call enable info on animal insights
                uIAnimalInsights.EnableInfo(canvasEnabled);
            }
            else
            {
                //set flags
                canvasEnabledOverride = false;
                canvasEnabled = false;
                //call enable info on animal insights
                uIAnimalInsights.EnableInfo(canvasEnabled);
                //set is active to false
                attributesObj.SetActive(false);
                vitalsObj.SetActive(false);
            }
        }

        //return if simulation is paused
        if (stopDayNightCycle) return;

        if (generalStatsUpdateTimer < generalStatsUpdateTime) generalStatsUpdateTimer += Time.deltaTime;
        else
        {
            //reset timer
            generalStatsUpdateTimer = 0;
            //reset counters
            wolvesCount = 0;
            foxesCount = 0;
            rabbitsCount = 0;

            foreach (AnimalInfo info in simulationManager.animalsStatus)
            {
                //for every animal in simulation manager's list of animals
                //update correct counter
                if (info.type == Animal.AnimalTypes.Wolf) wolvesCount++;
                else if (info.type == Animal.AnimalTypes.Fox) foxesCount++;
                else if (info.type == Animal.AnimalTypes.Rabbit) rabbitsCount++;
            }

            //update text field for number of wolves, foxes and rabbits
            generalCurrentDayText.text = "Wolves: " + wolvesCount.ToString() + "\n";
            generalCurrentDayText.text += "Foxes: " + foxesCount.ToString() + "\n";
            generalCurrentDayText.text += "Rabbits: " + rabbitsCount.ToString();
        }

        if (daysCount == 0)
        {
            //rotate the sky object by correct amount
            sunLight.transform.Rotate(new Vector3(-DAYINCREMENT, 0, 0));
            //set counters
            hoursCount = 7;
            daysCount = 1;
        }

        //day start 07:00
        //day end 24:00
        bool isNightTime = false;

        if (dayTimer < dayTimeScale) dayTimer += Time.deltaTime;
        else
        {
            dayTimer = 0;
            //update mins counter
            minsCount += 10;

            //check update hours count and reset mins count
            if (minsCount >= 60)
            {
                minsCount = 0;
                hoursCount++;
            }

            //check update days count and reset mins count
            if (hoursCount == 24 && minsCount == 0)
            {
                hoursCount = 0;
                daysCount++;
                //tell simulation manager the day is over
                simulationManager.DayOver();
            }

            //tell simulation manager the day is ending
            if (hoursCount == 19 && minsCount == 0) simulationManager.DayEnding();
            //tell simulation manager the day is starting
            if (hoursCount == 7 && minsCount == 0) simulationManager.DayStarting();

            if (hoursCount == 2 && minsCount == 0)
            {
                //tell simulation manager to cleanup the previous day
                simulationManager.CleanUpDay();
                //update the previous day text field
                generalLastDayText.text = generalCurrentDayText.text;
            }

            //check if it's night time
            if (hoursCount >= 0 && hoursCount < 7) isNightTime = true;
            //rotate the sky object by correct amount
            sunLight.transform.Rotate(new Vector3(DAYINCREMENT / 6, 0, 0));

            //get new rotation
            float starRotation = -DAYINCREMENT / 50;
            //check for might speed multiplier
            if (isNightTime) starRotation /= 0.3f;
            else starRotation /= dayTimeScale;
            //rotate by correct amount
            stars.transform.Rotate(new Vector3(starRotation / 3, 0, starRotation));

            //get hours count
            string hours = hoursCount.ToString();
            //apply desired format to hours count
            if (hours.Length == 1) hours = "0" + hours;
            //get mins count
            string mins = minsCount.ToString();
            //apply desired format to mins count
            if (mins.Length == 1) mins = "0" + mins;

            //create new time text contents
            string newText = baseText + daysCount + baseDailyText + hours + ":" + mins;
            //display time text
            timeText.text = newText;

            if (moon.activeInHierarchy)
            {
                if (hoursCount == 8 && minsCount >= 20)
                {
                    //disable at this time
                    moon.SetActive(false);
                    stars.SetActive(false);
                }
            }
            else
            {
                if (hoursCount == 19 && minsCount >= 30)
                {
                    //enable at this time
                    moon.SetActive(true);
                    stars.SetActive(true);
                }
            }

            //set timescale
            if (!isNightTime) dayTimeScale = baseDayTimeScale;
            else dayTimeScale = 0.3f / 2.5f;
        }
    }

    public void SelectAnimal(int ID)
    {
        //set new ID
        currentID = ID;
        uIAnimalInsights.SetCurrentID(currentID);

        //get animal type
        int type = (int)simulationManager.animalsStatus[currentID].type;
        int insightType = -1;
        //get behaviour display type
        if (type == 1) insightType = 3;
        else if (type == 2) insightType = 2;
        else if (type == 3) insightType = 1;
        //set behaviour display type
        if (insightType > 0) uIAnimalInsights.SetType(insightType);
        //enable information if desired
        if (uIAnimalInsights.updateInfo) uIAnimalInsights.EnableInfo(true);
    }
}
