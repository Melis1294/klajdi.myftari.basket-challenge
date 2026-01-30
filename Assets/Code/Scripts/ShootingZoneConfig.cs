using UnityEngine;

[CreateAssetMenu(fileName = "ShootingZoneConfig", menuName = "Basket/ Shooting Zone Config", order = 0)]
public class ShootingZoneConfig : ScriptableObject
{
    [Header("Hoop")]
    public float HoopLaunchScale = 0.15f;
    [Range(0f, 1f)]
    public float HoopArcPreference = 0.9f;

    [Header("Backboard")]
    public float BackboardLaunchScale = 0.11f;
    [Range(0f, 1f)]
    public float BackboardArcPreference = 0.65f;
    [Header("StrengthMultiplier")]
    [Range(0f, 1f)]
    public float StrengthMultiplier = 1f;
    [Range(0f, 20f)]
    public float BackBoardMaxLaunch = 7f;
    [Header("Hoop")]
    public float OpponentHoopLaunchScaleOffset = 0.05f;
    public float OpponentHoopArcPreferenceOffset = 0.01f;
    public float OpponentBackBoardLaunchScaleOffset = 0.05f;
    public float OpponentBackBoardArcPreferenceOffset = 0.01f;
    public float OpponentBackBoardMaxLaunch = 7f;
}
