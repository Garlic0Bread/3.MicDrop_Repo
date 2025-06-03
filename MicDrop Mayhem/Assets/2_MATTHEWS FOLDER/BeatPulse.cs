using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BeatPulse : MonoBehaviour
{
    public AudioSource audioSource; // Assign your music source here
    public RawImage[] images; // Assign your RawImages in the inspector
    public float intensity = 3.5f; // Increased intensity for stronger pulsing
    public float responseSpeed = 0.1f; // Slightly increased response speed for smoothing
    public float minThreshold = 0.005f; // Ensures small movements even in quieter sections
    public int freqRange = 40; // Expanded frequency range to better capture bass and highs
    public float sensitivityMultiplier = 100f; // More aggressive response to spectrum changes
    public float smoothTime = 0.075f; // Added smoothTime for gradual scaling

    private float[] spectrumData = new float[256];
    private float[] initialScales;
    private float velocity = 0f;

    void Start()
    {
        // Store initial scales of all images
        initialScales = new float[images.Length];
        for (int i = 0; i < images.Length; i++)
        {
            initialScales[i] = images[i].rectTransform.localScale.x;
        }
    }

    void Update()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            AnalyzeAudio();
        }
    }

    void AnalyzeAudio()
    {
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.Hamming);
        float sum = 0f;

        // Capture a broader range of frequencies
        for (int i = 0; i < freqRange; i++)
        {
            sum += spectrumData[i];
        }

        float avgAmplitude = sum / freqRange;
        avgAmplitude = Mathf.Max(avgAmplitude, minThreshold); // Prevents stillness

        float targetScale = 1f + (avgAmplitude * sensitivityMultiplier * intensity);

        // Smooth scaling for a more visually appealing effect
        for (int i = 0; i < images.Length; i++)
        {
            float newScale = Mathf.SmoothDamp(images[i].rectTransform.localScale.x, targetScale, ref velocity, smoothTime);
            images[i].rectTransform.localScale = new Vector3(newScale, newScale, 1f);
        }
    }
}

