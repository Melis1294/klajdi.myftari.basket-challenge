using UnityEngine;

[DisallowMultipleComponent]
public class ShootingZoneConfigProvider : MonoBehaviour
{
    [Tooltip("ScriptableObject that contains numeric tuning for this shooting zone")]
    public ShootingZoneConfig Config;

    [Tooltip("Scene Transform to use as backboard target for this shooting zone")]
    public Transform BackboardTarget;
}
