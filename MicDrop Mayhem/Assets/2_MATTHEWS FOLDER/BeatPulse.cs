using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class BeatPulse : MonoBehaviour
{
    public AudioSource audioSource; // Assign your music source here
    public GameObject[] objToPulse; // Assign your RawImages in the inspector
    public float intensity = 3.5f; // Increased intensity for stronger pulsing
    public float responseSpeed = 0.1f; // Slightly increased response speed for smoothing
    public float minThreshold = 0.005f; // Ensures small movements even in quieter sections
    public int freqRange = 40; // Expanded frequency range to better capture bass and highs
    public float sensitivityMultiplier = 100f; // More aggressive response to spectrum changes
    public float smoothTime = 0.075f; // Added smoothTime for gradual scaling

    private float[] spectrumData = new float[256];
    private Vector3[] initialScales;
    [SerializeField]
    private float[] velocity;

    [Header("BPM and Song Analysis")]
    private float lastPeakTime;
    public float bpm;
    private Queue<float> peakIntervals = new Queue<float>();
    public float DetectedBPM => bpm;
    public float beatThreshold = 0.05f; // Tweak based on your audio sensitivity
    private bool canDetect = true;
    public float beatCooldown = 0.2f; // Minimum time between detected peaks


    ComboManager comboManager = new ComboManager();

    void Start()
    {
        DetectBPM();
        // Store initial scales of all images
        initialScales = new Vector3[objToPulse.Length];
        velocity = new float[objToPulse.Length];
        for (int i = 0; i < objToPulse.Length; i++)
        {
            initialScales[i] = objToPulse[i].transform.localScale;
        }
 
        comboManager.beatWindow = Mathf.Clamp(60f / DetectedBPM, 0.25f, 0.7f);
    }

    void Update()
    {
        
        if (audioSource != null && audioSource.isPlaying)
        {
            AnalyzeAudio();
        }
    }
    
    public void DetectBPM()
    {

    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 200, 30), $"BPM: {Mathf.RoundToInt(bpm)}");
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

        // Smooth scaling for a more visually appealing effect
        for (int i = 0; i < objToPulse.Length; i++)
        {
            Vector3 currentScale = objToPulse[i].transform.localScale;
            Vector3 targetScale = initialScales[i] * (1f + avgAmplitude * sensitivityMultiplier * intensity);
            objToPulse[i].transform.localScale = Vector3.Lerp(currentScale, targetScale, smoothTime);
        }

        

        // --- Beat Detection for BPM Estimation ---
        if (canDetect && avgAmplitude > beatThreshold)
        {
            float timeNow = Time.time;
            float interval = timeNow - lastPeakTime;

            if (interval > beatCooldown && interval < 2f)
            {
                if (interval < 0.3f || interval > 2f)
                    return;

                Debug.Log($"Beat at: {Time.time:F2}, interval: {interval:F2}, avgAmplitude: {avgAmplitude:F3}");
                peakIntervals.Enqueue(interval);
                if (peakIntervals.Count > 8)
                    peakIntervals.Dequeue();

                float avgInterval = 0f;
                foreach (float i in peakIntervals)
                    avgInterval += i;
                avgInterval /= peakIntervals.Count;

                
                if (avgInterval > 0.001f) // prevent divide-by-zero or super small intervals
                {
                    float newBpm = Mathf.Clamp(60f / avgInterval, 60f, 180f);
                    bpm = Mathf.Lerp(bpm, newBpm, 0.1f);
                }
            }

            lastPeakTime = timeNow;
            StartCoroutine(BeatCooldown());
        }
    }

    private IEnumerator BeatCooldown()
    {
        canDetect = false;
        yield return new WaitForSeconds(beatCooldown);
        canDetect = true;
    }
}

