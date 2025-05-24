using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace EverdrivenDays
{
    [CustomEditor(typeof(SongLoader))]
    public class SongLoaderEditor : Editor
    {
        private string resourcePath = "Songs/LIFE"; // Default resource path
        private string filePath = ""; // Path to external audio file
        
        public override void OnInspectorGUI()
        {
            // Draw the default inspector
            DrawDefaultInspector();
            
            SongLoader songLoader = (SongLoader)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Song Loading Tools", EditorStyles.boldLabel);
            
            // Resource loading section
            EditorGUILayout.LabelField("Load from Resources", EditorStyles.boldLabel);
            resourcePath = EditorGUILayout.TextField("Resource Path", resourcePath);
            
            if (GUILayout.Button("Load from Resources"))
            {
                if (Application.isPlaying)
                {
                    songLoader.LoadSongFromResources(resourcePath);
                }
                else
                {
                    EditorUtility.DisplayDialog("Play Mode Required", 
                        "Please enter Play Mode to load songs.", "OK");
                }
            }
            
            EditorGUILayout.Space();
            
            // File loading section
            EditorGUILayout.LabelField("Load from File", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            filePath = EditorGUILayout.TextField("File Path", filePath);
            
            if (GUILayout.Button("Browse...", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFilePanel("Select Audio File", "", "mp3,wav,ogg");
                if (!string.IsNullOrEmpty(path))
                {
                    filePath = path;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Load from File"))
            {
                if (Application.isPlaying)
                {
                    if (string.IsNullOrEmpty(filePath))
                    {
                        EditorUtility.DisplayDialog("No File Selected", 
                            "Please select an audio file first.", "OK");
                    }
                    else
                    {
                        songLoader.LoadSongFromFile(filePath);
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Play Mode Required", 
                        "Please enter Play Mode to load songs.", "OK");
                }
            }
            
            EditorGUILayout.Space();
            
            // Quick start options
            EditorGUILayout.LabelField("Quick Start", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Quick Start with Default Song"))
            {
                if (Application.isPlaying)
                {
                    songLoader.QuickStart(resourcePath);
                }
                else
                {
                    EditorUtility.DisplayDialog("Play Mode Required", 
                        "Please enter Play Mode to quick start.", "OK");
                }
            }
            
            // Import song to resources
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Import Song to Resources", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Import Audio File to Resources"))
            {
                ImportAudioToResources();
            }
            
            // Help box
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "1. Add an AudioClip to your Resources/Songs folder\n" +
                "2. Set the Song Name to match the file\n" +
                "3. Click 'Load from Resources' or 'Quick Start'\n\n" +
                "For external files, use the Browse button to select a file", 
                MessageType.Info);
        }
        
        private void ImportAudioToResources()
        {
            string path = EditorUtility.OpenFilePanel("Select Audio File to Import", "", "mp3,wav,ogg");
            if (string.IsNullOrEmpty(path))
                return;
                
            // Create Resources directory if it doesn't exist
            string resourcesDir = Path.Combine(Application.dataPath, "Resources");
            if (!Directory.Exists(resourcesDir))
            {
                Directory.CreateDirectory(resourcesDir);
            }
            
            // Create Songs directory if it doesn't exist
            string songsDir = Path.Combine(resourcesDir, "Songs");
            if (!Directory.Exists(songsDir))
            {
                Directory.CreateDirectory(songsDir);
            }
            
            // Get filename
            string fileName = Path.GetFileName(path);
            
            // Destination path
            string destPath = Path.Combine(songsDir, fileName);
            
            try
            {
                // Copy the file
                File.Copy(path, destPath, true);
                AssetDatabase.Refresh();
                
                // Set the resource path to point to the new file
                // Remove extension and adjust path format for Resources.Load
                resourcePath = "Songs/" + Path.GetFileNameWithoutExtension(fileName);
                
                EditorUtility.DisplayDialog("Import Successful", 
                    $"Imported {fileName} to Resources/Songs folder.\n\nResource path set to: {resourcePath}", 
                    "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Import Failed", 
                    $"Failed to import audio file: {e.Message}", "OK");
            }
        }
    }
} 