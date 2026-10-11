using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Data/ShipData")]

public class ShipData : Data
{
    [Min(0f)] public float riseSpeed = 7f;
    [Min(0f)] public float fallSpeed = 7f;
    [Min(0.01f)] public float verticalAcceleration = 15f;
    [Min(0f)] public float rotationSpeed = 1f;
}
