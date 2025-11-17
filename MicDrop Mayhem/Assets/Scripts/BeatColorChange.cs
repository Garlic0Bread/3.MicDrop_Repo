using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BeatColorChange : MonoBehaviour
{
    [Header("Beat Pulse Reference")]
    public BeatPulse beatPulse; // Assign your BeatPulse manager in inspector

    [Header("Color Settings")]
    public Color baseColor = Color.white;
    public Color beatColor = Color.red;
    [Range(0f, 10f)]
    public float colorIntensity = 2f;
    [Range(0f, 1f)]
    public float colorResponseSpeed = 0.1f;

    [Header("Rainbow Mode")]
    public bool rainbowMode = false;
    [Range(0.1f, 5f)]
    public float rainbowSpeed = 1f;
    [Range(0f, 1f)]
    public float rainbowSaturation = 1f;
    [Range(0f, 1f)]
    public float rainbowValue = 1f;

    [Header("Target Type")]
    public bool targetSprite = true; // Set to false for UI Image

    private SpriteRenderer spriteRenderer;
    private Image uiImage;
    private Color currentColor;
    private Color targetColor;
    private float rainbowHue = 0f;

    [Header("BPM Sync Options")]
    public bool syncWithBPM = false;
    public float bpmColorChangeMultiplier = 1f;
    private float beatTimer = 0f;
    private float beatInterval = 0f;

    [Header("Advanced Options")]
    public bool useAlphaChannel = true;
    public bool affectChildren = false;
    private SpriteRenderer[] childSprites;
    private Image[] childImages;

    void Start()
    {
        // Get the appropriate component based on target type
        if (targetSprite)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError("BeatColorChange: No SpriteRenderer found on this GameObject!");
                return;
            }
            currentColor = spriteRenderer.color;
        }
        else
        {
            uiImage = GetComponent<Image>();
            if (uiImage == null)
            {
                Debug.LogError("BeatColorChange: No Image component found on this GameObject!");
                return;
            }
            currentColor = uiImage.color;
        }

        // Get child components if affecting children
        if (affectChildren)
        {
            if (targetSprite)
            {
                childSprites = GetComponentsInChildren<SpriteRenderer>();
                // Remove the parent sprite from children array
                childSprites = System.Array.FindAll(childSprites, sprite => sprite != spriteRenderer);
            }
            else
            {
                childImages = GetComponentsInChildren<Image>();
                // Remove the parent image from children array
                childImages = System.Array.FindAll(childImages, image => image != uiImage);
            }
        }

        targetColor = baseColor;
        currentColor = baseColor;

        // If no BeatPulse assigned, try to find one
        if (beatPulse == null)
        {
            beatPulse = FindObjectOfType<BeatPulse>();
            if (beatPulse == null)
                Debug.LogWarning("BeatColorChange: No BeatPulse found in scene!");
        }

        ApplyColor(); // Apply initial color
    }

    void Update()
    {
        if (beatPulse == null) return;

        if (rainbowMode)
        {
            UpdateRainbowColor();
        }
        else if (syncWithBPM)
        {
            UpdateBPMColor();
        }
        else
        {
            UpdateAmplitudeColor();
        }

        ApplyColor();
    }

    private void UpdateRainbowColor()
    {
        // Cycle through hue values for rainbow effect
        rainbowHue += Time.deltaTime * rainbowSpeed * 0.1f;
        if (rainbowHue >= 1f)
        {
            rainbowHue = 0f;
        }

        // Convert HSV to RGB for rainbow color
        Color rainbowColor = Color.HSVToRGB(rainbowHue, rainbowSaturation, rainbowValue);

        float intensity = 0f;

        // Get intensity from BeatPulse system to make rainbow pulse with beat
        if (beatPulse.objToPulse != null && beatPulse.objToPulse.Length > 0)
        {
            Vector3 currentScale = beatPulse.objToPulse[0].transform.localScale;
            intensity = Mathf.Max(
                Mathf.Abs(currentScale.x - 1f),
                Mathf.Abs(currentScale.y - 1f),
                Mathf.Abs(currentScale.z - 1f)
            ) * colorIntensity;
        }

        intensity = Mathf.Clamp01(intensity);

        // In rainbow mode, we lerp between base color and rainbow color based on intensity
        targetColor = Color.Lerp(baseColor, rainbowColor, intensity);
        currentColor = Color.Lerp(currentColor, targetColor, colorResponseSpeed);
    }

    private void UpdateAmplitudeColor()
    {
        float intensity = 0f;

        // Get intensity from BeatPulse system
        if (beatPulse.objToPulse != null && beatPulse.objToPulse.Length > 0)
        {
            Vector3 currentScale = beatPulse.objToPulse[0].transform.localScale;
            intensity = Mathf.Max(
                Mathf.Abs(currentScale.x - 1f),
                Mathf.Abs(currentScale.y - 1f),
                Mathf.Abs(currentScale.z - 1f)
            ) * colorIntensity;
        }

        intensity = Mathf.Clamp01(intensity);
        targetColor = Color.Lerp(baseColor, beatColor, intensity);
        currentColor = Color.Lerp(currentColor, targetColor, colorResponseSpeed);
    }

    private void UpdateBPMColor()
    {
        if (beatPulse.bpm <= 0) return;

        // Calculate beat interval based on BPM
        beatInterval = 60f / beatPulse.bpm * bpmColorChangeMultiplier;
        beatTimer += Time.deltaTime;

        // Create a pulsing effect based on BPM
        float pulse = Mathf.PingPong(beatTimer / beatInterval, 1f);
        float intensity = (Mathf.Sin(pulse * Mathf.PI * 2f) + 1f) * 0.5f;

        targetColor = Color.Lerp(baseColor, beatColor, intensity);
        currentColor = Color.Lerp(currentColor, targetColor, colorResponseSpeed);

        // Reset timer when we complete a cycle
        if (beatTimer >= beatInterval * 2f)
        {
            beatTimer = 0f;
        }
    }

    private void ApplyColor()
    {
        Color finalColor = currentColor;

        // Apply alpha channel setting
        if (!useAlphaChannel)
        {
            finalColor.a = targetSprite ? spriteRenderer.color.a : uiImage.color.a;
        }

        // Apply to main component
        if (targetSprite && spriteRenderer != null)
        {
            spriteRenderer.color = finalColor;
        }
        else if (!targetSprite && uiImage != null)
        {
            uiImage.color = finalColor;
        }

        // Apply to children if enabled
        if (affectChildren)
        {
            if (targetSprite && childSprites != null)
            {
                foreach (SpriteRenderer childSprite in childSprites)
                {
                    if (childSprite != null)
                        childSprite.color = finalColor;
                }
            }
            else if (!targetSprite && childImages != null)
            {
                foreach (Image childImage in childImages)
                {
                    if (childImage != null)
                        childImage.color = finalColor;
                }
            }
        }
    }

    // Public method to toggle rainbow mode
    public void ToggleRainbowMode()
    {
        rainbowMode = !rainbowMode;
        Debug.Log($"Rainbow mode: {(rainbowMode ? "ON" : "OFF")}");
    }

    // Public method to set rainbow mode directly
    public void SetRainbowMode(bool enableRainbow)
    {
        rainbowMode = enableRainbow;
        Debug.Log($"Rainbow mode: {(rainbowMode ? "ON" : "OFF")}");
    }

    // Public method to set rainbow speed
    public void SetRainbowSpeed(float speed)
    {
        rainbowSpeed = Mathf.Clamp(speed, 0.1f, 5f);
    }

    // Public method to set rainbow saturation and value
    public void SetRainbowQuality(float saturation, float value)
    {
        rainbowSaturation = Mathf.Clamp01(saturation);
        rainbowValue = Mathf.Clamp01(value);
    }

    // Public method to manually trigger color change
    public void TriggerColorChange(Color newColor, float duration = 0.5f)
    {
        if (!rainbowMode) // Only allow manual color changes when not in rainbow mode
        {
            StartCoroutine(ManualColorChangeRoutine(newColor, duration));
        }
    }

    private IEnumerator ManualColorChangeRoutine(Color target, float duration)
    {
        Color startColor = currentColor;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            currentColor = Color.Lerp(startColor, target, timer / duration);
            ApplyColor();
            yield return null;
        }

        // Return to beat-synced color after manual change
        StartCoroutine(ReturnToBeatColor());
    }

    private IEnumerator ReturnToBeatColor()
    {
        Color startColor = currentColor;
        float timer = 0f;
        float returnDuration = 1f;

        while (timer < returnDuration)
        {
            timer += Time.deltaTime;
            currentColor = Color.Lerp(startColor, targetColor, timer / returnDuration);
            ApplyColor();
            yield return null;
        }
    }

    // Method to change colors dynamically
    public void SetColors(Color newBaseColor, Color newBeatColor, float transitionTime = 1f)
    {
        if (!rainbowMode) // Only allow color changes when not in rainbow mode
        {
            StartCoroutine(TransitionColorsRoutine(newBaseColor, newBeatColor, transitionTime));
        }
    }

    private IEnumerator TransitionColorsRoutine(Color newBase, Color newBeat, float duration)
    {
        Color oldBase = baseColor;
        Color oldBeat = beatColor;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            baseColor = Color.Lerp(oldBase, newBase, timer / duration);
            beatColor = Color.Lerp(oldBeat, newBeat, timer / duration);
            yield return null;
        }
    }

    // Method to instantly set colors without transition
    public void SetColorsInstant(Color newBaseColor, Color newBeatColor)
    {
        if (!rainbowMode) // Only allow color changes when not in rainbow mode
        {
            baseColor = newBaseColor;
            beatColor = newBeatColor;
        }
    }

    // Method to toggle between Sprite and UI Image mode at runtime
    public void SetTargetType(bool useSprite)
    {
        if (useSprite != targetSprite)
        {
            targetSprite = useSprite;

            // Re-initialize components
            if (targetSprite)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                uiImage = null;
            }
            else
            {
                uiImage = GetComponent<Image>();
                spriteRenderer = null;
            }
        }
    }

    // Get current color state information
    public bool IsRainbowModeActive() => rainbowMode;
    public Color GetCurrentColor() => currentColor;
    public Color GetTargetColor() => targetColor;

    void OnValidate()
    {
        // Update color immediately in editor when values change
        if (Application.isPlaying)
        {
            ApplyColor();
        }
    }
}