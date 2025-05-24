using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace EverdrivenDays
{
    public class SongLoader : MonoBehaviour
    {
        [SerializeField] private RhythmGameController rhythmGameController;
        
        [Header("BPM Detection")]
        [SerializeField] private bool automaticBpmDetection = true;
        [SerializeField] private float fallbackBpm = 120f;
        [SerializeField] private float minBpm = 60f;
        [SerializeField] private float maxBpm = 200f;
        
        [Header("Song Settings")]
        [SerializeField] private string songName = "LIFE";
        [SerializeField] private int difficulty = 5;
        [SerializeField] private float offset = 0f;
        
        private void Start()
        {
            if (rhythmGameController == null)
            {
                rhythmGameController = FindAnyObjectByType<RhythmGameController>();
                
                if (rhythmGameController == null)
                {
                    Debug.LogError("Could not find RhythmGameController in the scene!");
                    return;
                }
            }
        }
        
        public void LoadSongFromFile(string filePath)
        {
            StartCoroutine(LoadAudioFile(filePath));
        }
        
        public void LoadSongFromResources(string resourcePath)
        {
            AudioClip clip = Resources.Load<AudioClip>(resourcePath);
            
            if (clip == null)
            {
                Debug.LogError($"Could not load audio clip from resources: {resourcePath}");
                return;
            }
            
            ProcessAudioClip(clip);
        }
        
        private IEnumerator LoadAudioFile(string filePath)
        {
            string uri = string.Empty;
            
            // Determine if this is a local file or URL
            if (filePath.StartsWith("http"))
            {
                uri = filePath;
            }
            else
            {
                uri = "file://" + filePath;
            }
            
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.UNKNOWN))
            {
                yield return request.SendWebRequest();
                
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to load audio file: {request.error}");
                    yield break;
                }
                
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip == null)
                {
                    Debug.LogError("Failed to convert audio data to AudioClip");
                    yield break;
                }
                
                // Set the clip name to the filename if not already set
                if (string.IsNullOrEmpty(songName))
                {
                    songName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                }
                
                clip.name = songName;
                
                ProcessAudioClip(clip);
            }
        }
        
        private void ProcessAudioClip(AudioClip clip)
        {
            float bpm = fallbackBpm;
            
            if (automaticBpmDetection)
            {
                bpm = DetectBPM(clip);
                
                // If BPM detection failed, use fallback
                if (bpm <= 0f)
                {
                    Debug.LogWarning($"BPM detection failed for {clip.name}. Using fallback: {fallbackBpm}");
                    bpm = fallbackBpm;
                }
                
                Debug.Log($"Detected BPM for {clip.name}: {bpm}");
            }
            
            // Create and configure the song data
            SongData songData = new SongData
            {
                songName = songName,
                songClip = clip,
                bpm = bpm,
                offset = offset,
                difficulty = difficulty,
                generateNotes = true
            };
            
            // Start the game with this song
            rhythmGameController.StartGame(songData);
        }
        
        private float DetectBPM(AudioClip clip)
        {
            if (clip == null) return -1f;
            
            // This is a simplified BPM detection algorithm
            // In a real implementation, you'd want to use more sophisticated
            // beat detection algorithms like energy-based onset detection
            
            // For now, we'll analyze sample amplitude to detect beats
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);
            
            // Work with mono for simplicity
            float[] monoSamples = ConvertToMono(samples, clip.channels);
            
            // Perform a simple energy-based onset detection
            float[] energyCurve = CalculateEnergyCurve(monoSamples, clip.frequency);
            
            // Find peaks in the energy curve that indicate potential beats
            List<int> peaks = FindPeaks(energyCurve);
            
            if (peaks.Count < 2)
            {
                Debug.LogWarning("Not enough peaks detected for BPM calculation");
                return fallbackBpm;
            }
            
            // Calculate average time between peaks
            float totalTime = 0f;
            int validIntervals = 0;
            
            for (int i = 1; i < peaks.Count; i++)
            {
                int sampleInterval = peaks[i] - peaks[i - 1];
                float timeInterval = (float)sampleInterval / clip.frequency;
                
                // Ignore very short intervals (noise)
                if (timeInterval >= 0.2f)
                {
                    totalTime += timeInterval;
                    validIntervals++;
                }
            }
            
            if (validIntervals == 0)
            {
                Debug.LogWarning("No valid intervals detected for BPM calculation");
                return fallbackBpm;
            }
            
            float averageInterval = totalTime / validIntervals;
            float bpm = 60f / averageInterval;
            
            // Round to nearest whole BPM and clamp to reasonable range
            bpm = Mathf.Round(bpm);
            bpm = Mathf.Clamp(bpm, minBpm, maxBpm);
            
            return bpm;
        }
        
        private float[] ConvertToMono(float[] samples, int channels)
        {
            if (channels == 1) return samples;
            
            float[] monoSamples = new float[samples.Length / channels];
            
            for (int i = 0; i < monoSamples.Length; i++)
            {
                float sum = 0f;
                for (int c = 0; c < channels; c++)
                {
                    sum += samples[i * channels + c];
                }
                monoSamples[i] = sum / channels;
            }
            
            return monoSamples;
        }
        
        private float[] CalculateEnergyCurve(float[] samples, int sampleRate)
        {
            // Calculate energy in windows of 1024 samples
            int windowSize = 1024;
            int numWindows = samples.Length / windowSize;
            float[] energyCurve = new float[numWindows];
            
            for (int window = 0; window < numWindows; window++)
            {
                float energy = 0f;
                int offset = window * windowSize;
                
                for (int i = 0; i < windowSize; i++)
                {
                    if (offset + i < samples.Length)
                    {
                        energy += samples[offset + i] * samples[offset + i];
                    }
                }
                
                energyCurve[window] = energy / windowSize;
            }
            
            return energyCurve;
        }
        
        private List<int> FindPeaks(float[] curve)
        {
            List<int> peaks = new List<int>();
            
            // Simple threshold for what constitutes a peak
            float threshold = 0.1f;
            
            // Calculate the average energy
            float averageEnergy = 0f;
            for (int i = 0; i < curve.Length; i++)
            {
                averageEnergy += curve[i];
            }
            averageEnergy /= curve.Length;
            
            // Set threshold relative to average
            threshold = averageEnergy * 1.5f;
            
            // Find peaks
            for (int i = 2; i < curve.Length - 2; i++)
            {
                if (curve[i] > threshold &&
                    curve[i] > curve[i - 1] && curve[i] > curve[i - 2] &&
                    curve[i] > curve[i + 1] && curve[i] > curve[i + 2])
                {
                    peaks.Add(i);
                }
            }
            
            return peaks;
        }
        
        // Public method to load a song and start the game immediately
        public void QuickStart(string resourcePath)
        {
            LoadSongFromResources(resourcePath);
        }
    }
} 