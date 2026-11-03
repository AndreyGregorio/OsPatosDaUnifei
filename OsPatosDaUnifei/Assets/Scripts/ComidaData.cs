using UnityEngine;

[CreateAssetMenu(fileName = "NewFood", menuName = "UnifeiDucks/Food", order = 1)]
public class ComidaData : ScriptableObject
{
    [Header("Information of the food")]
    public string nameFood;

    [Header("Visual")]
    public Sprite artePixel;
}