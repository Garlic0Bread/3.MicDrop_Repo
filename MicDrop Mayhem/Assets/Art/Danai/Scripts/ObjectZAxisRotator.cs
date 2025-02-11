using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectZAxisRotator : MonoBehaviour
{
    public float rotationSpeed = 2f; // Speed of the rotation
    public float maxAngle = 30f; // Maximum angle of rotation on Z-axis

    private float currentAngle = 0f;
    private int direction = 1;

    void Update()
    {
        // Calculate the new angle
        currentAngle += direction * rotationSpeed * Time.deltaTime;

        // Check if the current angle exceeds the max or min angle
        if (currentAngle > maxAngle)
        {
            currentAngle = maxAngle;
            direction = -1; // Reverse direction
        }
        else if (currentAngle < -maxAngle)
        {
            currentAngle = -maxAngle;
            direction = 1; // Reverse direction
        }

        // Apply the rotation to the object
        transform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
    }
}
