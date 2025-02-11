using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BeatManager : MonoBehaviour
{
    [SerializeField] private AudioSource[] _artistSongs; // Array of audio sources
    [SerializeField] private float beatWindow = 0.1f; // Time window around each beat for sync detection
    [SerializeField] private float[] _bpms; // Array of BPMs corresponding to each audio source
    public IntervalSet[] _intervalSets; // Public array of interval sets for Inspector assignment
    public Game_Manager gm;

    public int GetCurrentAudioIndex() // Get the index of the currently playing audio source
    {
        for (int i = 0; i < _artistSongs.Length; i++)
        {
            if (_artistSongs[i].isPlaying) // Check if this audio source is playing
            {
                return i; // Return the index of the playing audio source
            }
        }
        return -1; // Return -1 if no audio source is currently playing
    }
    public bool IsOnBeat(int audioIndex) // Check if the current time of a specific audio source is within the beat window
    {
        if (audioIndex < 0 || audioIndex >= _artistSongs.Length || audioIndex >= _bpms.Length || audioIndex >= _intervalSets.Length)
        {
            Debug.LogError("Invalid audioIndex!");
            return false;
        }

        AudioSource currentSource = _artistSongs[audioIndex];
        float currentBPM = _bpms[audioIndex];
        Intervals[] currentIntervals = _intervalSets[audioIndex].intervals;

        foreach (Intervals interval in currentIntervals)
        {
            float sampledTime = (currentSource.timeSamples / (currentSource.clip.frequency * interval.GetIntervalLength(currentBPM)));
            if (interval.IsWithin_BeatWindow(sampledTime, beatWindow))
            {
                return true;
            }
        }
        return false;
    }
    public void Add_BPM(float bpm)
    {
        if (gm.rosterIndex == 1)
        {
            if (_bpms.Length > 0)
            {
                float[] firsBPM_Removed = new float[_bpms.Length - 1];
                for (int i = 1; i < _bpms.Length; i++)// Copy all elements except the first one into the new array
                {
                    firsBPM_Removed[i - 1] = _bpms[i];
                }
                _bpms = firsBPM_Removed;
            }


            float[] bpmAdded = new float[_bpms.Length + 1];// Create a new array that is one element larger
            bpmAdded[0] = bpm;
            for (int i = 0; i < _bpms.Length; i++)
            {
                bpmAdded[i + 1] = _bpms[i];
            }
            _bpms = bpmAdded;
        }

        else if (gm.rosterIndex == 2)
        {
            if (_bpms.Length > 0)
            {
                float[] lastBPM_Removed = new float[_bpms.Length - 1];
                for (int i = 0; i < _bpms.Length - 1; i++)// Copy all elements except the last one into the new array
                {
                    lastBPM_Removed[i] = _bpms[i];
                }
                _bpms = lastBPM_Removed;
            }


            float[] bpmAdded = new float[_bpms.Length + 1];// Create a new array that is one element larger
            for (int i = 0; i < _bpms.Length; i++)// Copy the existing elements from _audioSources to the new array
            {
                bpmAdded[i] = _bpms[i];
            }
            bpmAdded[_bpms.Length] = bpm;// Add the new AudioSource to the last position in the new array
            _bpms = bpmAdded;
        }
        
    }
    public void Add_ArtistSong(AudioSource song)
    {
        if (gm.rosterIndex == 1)
        {
            if (_artistSongs.Length > 0)//remove current song before adding
            {
                AudioSource[] firstSongRemoved_FromPlaylist = new AudioSource[_artistSongs.Length - 1];// Create a new array that is one element smaller
                for (int i = 1; i < _artistSongs.Length; i++)// Copy all elements except the first one into the new array
                {
                    firstSongRemoved_FromPlaylist[i - 1] = _artistSongs[i];
                }
                _artistSongs = firstSongRemoved_FromPlaylist;
            }
            else
            {
                Debug.LogError("No AudioSource to remove!");
            }

            AudioSource[] added_ToPlaylist = new AudioSource[_artistSongs.Length + 1];// Create a new array that is one element larger
            added_ToPlaylist[0] = song;// Add the new AudioSource to the first position in the new array

            // Copy the existing elements from _artistSongs to the new array, starting from index 1
            for (int i = 0; i < _artistSongs.Length; i++)
            {
                added_ToPlaylist[i + 1] = _artistSongs[i];
            }
            _artistSongs = added_ToPlaylist;
        }

        else if(gm.rosterIndex == 2)
        {
            if (_artistSongs.Length > 0)
            {
                AudioSource[] lastSongRemoved_FromPlaylist = new AudioSource[_artistSongs.Length - 1];
                for (int i = 0; i < _artistSongs.Length - 1; i++)// Copy all elements except the last one into the new array
                {
                    lastSongRemoved_FromPlaylist[i] = _artistSongs[i];
                }
                _artistSongs = lastSongRemoved_FromPlaylist;
            }
            else
            {
                Debug.LogError("No AudioSource to remove!");
            }

            
            AudioSource[] added_ToPlaylist = new AudioSource[_artistSongs.Length + 1];// Create a new array that is one element larger
            for (int i = 0; i < _artistSongs.Length; i++)// Copy the existing elements from _audioSources to the new array
            {
                added_ToPlaylist[i] = _artistSongs[i];
            }
            
            added_ToPlaylist[_artistSongs.Length] = song;// Add the new AudioSource to the last position in the new array

            // Update the _audioSources reference to the new array
            _artistSongs = added_ToPlaylist;
        }
        if (song == null)
        {
            Debug.LogError("AudioSource cannot be null!");
            return;
        }
    }

    private void Update()
    {
        for (int i = 0; i < _artistSongs.Length; i++)
        {
            if (i >= _bpms.Length)
            {
                Debug.LogError("Mismatch in array sizes for audio sources, BPMs, or interval sets!");
                continue;
            }

            AudioSource currentSource = _artistSongs[i];
            float currentBPM = _bpms[i];
            Intervals[] currentIntervals = _intervalSets[i].intervals;

            foreach (Intervals interval in currentIntervals)
            {
                float sampledTime = (currentSource.timeSamples / (currentSource.clip.frequency * interval.GetIntervalLength(currentBPM)));
                interval.CheckForNewInterval(sampledTime);
            }
        }
    }
}

[System.Serializable]
public class Intervals
{
    [SerializeField] private float _steps; // Number of steps per beat
    [SerializeField] private GameObject _pulseObject; // GameObject with the Pulse script
    private int _lastInterval;
    private float _intervalTime;

    public float GetIntervalLength(float bpm)
    {
        _intervalTime = 60f / (bpm * _steps);
        return _intervalTime;
    }

    public bool IsWithin_BeatWindow(float sampledTime, float beatWindow)
    {
        float currentInterval = Mathf.Floor(sampledTime);
        return Mathf.Abs(sampledTime - currentInterval) < beatWindow;
    }

    public void CheckForNewInterval(float sampledTime)
    {
        int currentInterval = Mathf.FloorToInt(sampledTime);
        if (currentInterval != _lastInterval)
        {
            _lastInterval = currentInterval;

            // Trigger the Pulse function on the assigned GameObject
            if (_pulseObject != null)
            {
                PulseToTheBeat pulseScript = _pulseObject.GetComponent<PulseToTheBeat>();
                if (pulseScript != null)
                {
                    pulseScript.Pulse();
                }
            }
        }
    }
}
[System.Serializable]
public class IntervalSet
{
    public Intervals[] intervals; // Array of Intervals
}
