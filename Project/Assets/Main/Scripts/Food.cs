using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Food : MonoBehaviour
{
    [SerializeField] private FoodTypes foodType = FoodTypes.Empty;

    public enum FoodTypes
    {
        Empty,
        Meat,
        Vegetarian
    }

    public FoodTypes GetFoodType() { return foodType; }


}
