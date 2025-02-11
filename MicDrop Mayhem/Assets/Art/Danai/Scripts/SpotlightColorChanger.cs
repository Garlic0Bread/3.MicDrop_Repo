using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpotlightColorChanger : MonoBehaviour
{
    public float cycleDuration = 5f; // Time in seconds to complete a full color cycle

    private Light spotlight;
    private float timer;

    void Start()
    {
        // Get the Light component attached to this GameObject
        spotlight = GetComponent<Light>();

        if (spotlight == null)
        {
            Debug.LogError("No Light component found! Attach this script to a GameObject with a Light component.");
        }
    }

    void Update()
    {
        if (spotlight != null)
        {
            // Increment timer based on the time elapsed since the last frame
            timer += Time.deltaTime / cycleDuration;

            // Calculate a new color based on the timer and cycle it smoothly
            spotlight.color = Color.HSVToRGB(Mathf.Repeat(timer, 1f), 1f, 1f);
        }
    }
}