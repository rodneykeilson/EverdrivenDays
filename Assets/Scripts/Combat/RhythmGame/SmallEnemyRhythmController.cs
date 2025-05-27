using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EverdrivenDays
{
    public class SmallEnemyRhythmController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private GameObject notePrefab;
        [SerializeField] private RectTransform[] laneTargets; // The hit positions
        [SerializeField] private RectTransform[] laneSpawnPoints; // Where notes spawn from
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private TextMeshProUGUI accuracyText;
        [SerializeField] private TextMeshProUGUI gradeText;
        [SerializeField] private TextMeshProUGUI encounterText;
        [SerializeField] private GameObject rhythmGameUI;
        [SerializeField] private GameObject encounterEffectUI;

        [Header("Game Settings")]
        [SerializeField] private float noteSpeed = 500f;
        [SerializeField] private float perfectWindow = 30f; // Time in milliseconds
        [SerializeField] private float goodWindow = 60f; // Time in milliseconds
        [SerializeField] private float okayWindow = 90f; // Time in milliseconds
        [SerializeField] private float encounterEffectDuration = 1f; // Duration for the encounter effect
        [SerializeField] private AudioClip encounterSFX;
        
        [Header("Songs")]
        [SerializeField] private List<SongData> availableSongs = new List<SongData>();
        [SerializeField] private NoteGenerationSettings noteGenSettings = new NoteGenerationSettings();

        [Header("Key Bindings")]
        [SerializeField] private KeyCode[] laneKeys = new KeyCode[4] { KeyCode.D, KeyCode.F, KeyCode.J, KeyCode.K };

        // Scoring
        private int currentScore = 0;
        private int currentCombo = 0;
        private int maxCombo = 0;
        private int perfectHits = 0;
        private int goodHits = 0;
        private int okayHits = 0;
        private int missedHits = 0;

        // Game state
        private bool isPlaying = false;
        private float gameStartTime;
        private float songPosition;
        private List<GameObject> activeNotes = new List<GameObject>();
        private Enemy currentEnemy;
        private Player player;
        private SongData currentSong;
        private float gameDuration; // Variable duration

        // Properties for combat results
        public bool PlayerWon { get; private set; }
        public int FinalScore => currentScore;
        public int MaxCombo => maxCombo;
        public bool IsFullCombo => missedHits == 0;
        public bool IsAllPerfect => perfectHits > 0 && goodHits == 0 && okayHits == 0 && missedHits == 0;

        private void Awake()
        {
            if (rhythmGameUI != null)
                rhythmGameUI.SetActive(false);

            if (encounterEffectUI != null)
                encounterEffectUI.SetActive(false);
        }

        private void Update()
        {
            if (!isPlaying) return;

            // Update song position
            songPosition = Time.time - gameStartTime;

            // Process input
            for (int i = 0; i < laneKeys.Length; i++)
            {
                if (Input.GetKeyDown(laneKeys[i]))
                {
                    ProcessLanePress(i);
                }
            }

            // Update note positions every frame
            MoveNotes();

            // Check for missed notes
            CheckMissedNotes();

            // Periodic debug info
            if (Time.frameCount % 300 == 0)
            {
                Debug.Log($"Game state: Playing={isPlaying}, SongPosition={songPosition:F2}, ActiveNotes={activeNotes.Count}");
                foreach (var note in activeNotes)
                {
                    if (note != null)
                    {
                        var nc = note.GetComponent<NoteController>();
                        Debug.Log($"Note: lane={nc?.Lane}, targetTime={nc?.TargetTime}, currentPos={note.transform.position}");
                    }
                }
            }
        }

        // Move notes from spawn to target using Lerp, matching RhythmGameController
        private void MoveNotes()
        {
            List<GameObject> notesToRemove = new List<GameObject>();
            foreach (var noteObj in activeNotes)
            {
                if (noteObj == null) continue;
                NoteController nc = noteObj.GetComponent<NoteController>();
                if (nc == null) continue;
                int lane = nc.Lane;
                float noteTime = nc.TargetTime;

                // Calculate progress (0 = spawn, 1 = hit target)
                float travelTime = 2.0f; // seconds to reach target
                float timeSinceSpawn = songPosition - (noteTime - travelTime);
                float progress = Mathf.Clamp01(timeSinceSpawn / travelTime);
                Vector3 startPos = laneSpawnPoints[lane].position;
                Vector3 endPos = laneTargets[lane].position;
                noteObj.transform.position = Vector3.Lerp(startPos, endPos, progress);

                // Remove if past hit window
                if (songPosition - noteTime > (okayWindow / 1000f))
                {
                    notesToRemove.Add(noteObj);
                }
            }
            foreach (var note in notesToRemove)
            {
                activeNotes.Remove(note);
                Destroy(note);
            }
        }

        // Check for missed notes (matching RhythmGameController)
        private void CheckMissedNotes()
        {
            List<GameObject> notesToRemove = new List<GameObject>();
            foreach (var noteObj in activeNotes)
            {
                if (noteObj == null) continue;
                NoteController nc = noteObj.GetComponent<NoteController>();
                if (nc == null) continue;
                float noteTime = nc.TargetTime;
                if (songPosition - noteTime > (okayWindow / 1000f))
                {
                    missedHits++;
                    currentCombo = 0;
                    ShowHitFeedback("MISS", Color.red);
                    UpdateScoreUI();
                    notesToRemove.Add(noteObj);
                    Debug.Log($"Missed note in lane {nc.Lane} at {noteTime}, current: {songPosition}");
                }
            }
            foreach (var note in notesToRemove)
            {
                activeNotes.Remove(note);
                Destroy(note);
            }
        }

        private IEnumerator ShowEncounterEffect()
        {
            Debug.Log("Showing encounter effect");
            
            // Hide rhythm game UI during encounter effect
            if (rhythmGameUI != null)
                rhythmGameUI.SetActive(false);
            
            // Show encounter effect UI
            if (encounterEffectUI != null)
            {
                encounterEffectUI.SetActive(true);
                
                // Set encounter text
                if (encounterText != null)
                {
                    encounterText.text = $"Battle with {currentEnemy.name}!";
                    StartCoroutine(ScaleTextAnimation(encounterText.gameObject, 0.5f));
                }
                
                // Play encounter sound effect with a dedicated AudioSource to control its duration
                if (encounterSFX != null)
                {
                    // Create a temporary GameObject with AudioSource
                    GameObject tempAudio = new GameObject("TempEncounterAudio");
                    AudioSource audioSource = tempAudio.AddComponent<AudioSource>();
                    
                    // Configure the audio source
                    audioSource.clip = encounterSFX;
                    audioSource.volume = 1.0f;
                    audioSource.spatialBlend = 0f; // 2D sound
                    audioSource.Play();
                    
                    // Destroy after 1 second to cut the sound
                    Destroy(tempAudio, 1.0f);
                    
                    Debug.Log("Playing encounter sound effect (limited to 1 second)");
                }
                
                // Use CombatEffectsManager to show encounter effect
                if (CombatEffectsManager.Instance != null)
                {
                    Debug.Log("Using CombatEffectsManager to show encounter effect");
                    CombatEffectsManager.Instance.PlayEncounterEffect(currentEnemy.transform.position);
                }
            }
            
            // Wait for the encounter effect duration
            yield return new WaitForSeconds(encounterEffectDuration);
            
            // Hide encounter effect UI
            if (encounterEffectUI != null)
                encounterEffectUI.SetActive(false);
            
            // Start the actual rhythm game
            StartActualGame();
        }

        public void StartGame(Enemy enemy, Player playerRef, AudioClip songClip = null) 
        {
            Debug.Log($"Starting rhythm game with enemy: {enemy.name}");
            
            // Store references
            currentEnemy = enemy;
            player = playerRef;
            
            // Reset game state
            currentScore = 0;
            currentCombo = 0;
            maxCombo = 0;
            perfectHits = 0;
            goodHits = 0;
            okayHits = 0;
            missedHits = 0;
            
            // Clear any active notes
            foreach (var note in activeNotes)
            {
                if (note != null)
                    Destroy(note);
            }
            activeNotes.Clear();
            
            // Select a song
            SelectSong(songClip);
            
            // Store the current state of the HUD instead of always hiding it
            GameObject hudUI = GameObject.FindGameObjectWithTag("HUD");
            
            // Disable player movement during the rhythm game
            if (player != null && player.Input != null)
            {
                player.Input.DisableMovement();
            }
            
            // Show encounter effect first, then start the actual game
            StartCoroutine(ShowEncounterEffect());
        }

        private void SelectSong(AudioClip specificSongClip = null)
        {
            Debug.Log("Selecting song for rhythm game...");
            
            // If a specific song clip is provided, try to find it in available songs
            if (specificSongClip != null)
            {
                Debug.Log($"Looking for specific song clip: {specificSongClip.name}");
                foreach (var song in availableSongs)
                {
                    if (song.songClip == specificSongClip)
                    {
                        currentSong = song;
                        Debug.Log($"Found matching song: {song.songName}");
                        break;
                    }
                }
            }
            
            // If no song was found or specified, pick a random one
            if (currentSong == null && availableSongs.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, availableSongs.Count);
                currentSong = availableSongs[randomIndex];
                Debug.Log($"Selected random song: {currentSong.songName} (index {randomIndex} of {availableSongs.Count})");
            }
            else if (availableSongs.Count == 0)
            {
                Debug.LogError("No songs available in the availableSongs list! Please add songs in the inspector.");
            }
            
            // If we have a song, set its duration as the game duration
            if (currentSong != null && currentSong.songClip != null)
            {
                gameDuration = currentSong.songClip.length;
                Debug.Log($"Set game duration to {gameDuration} seconds based on song length");
                
                // Assign the song to the music source
                if (musicSource != null)
                {
                    musicSource.clip = currentSong.songClip;
                    Debug.Log($"Assigned song clip '{currentSong.songClip.name}' to music source");
                    
                    // Make sure volume is set properly
                    musicSource.volume = 1.0f;
                }
                else
                {
                    Debug.LogError("Music source is null! Make sure to assign an AudioSource component.");
                }
            }
            else
            {
                // Default duration if no song is available
                gameDuration = 15f;
                Debug.LogWarning("No song available for rhythm game. Using default duration.");
            }
        }

        private void StartActualGame()
        {
            Debug.Log("Starting rhythm game...");
            
            // Show rhythm game UI
            if (rhythmGameUI != null)
                rhythmGameUI.SetActive(true);
            
            // Reset UI elements
            if (scoreText != null)
                scoreText.text = "0";
            
            if (comboText != null)
                comboText.text = "0";
            
            if (accuracyText != null)
                accuracyText.text = "Accuracy: 0%";
            
            if (gradeText != null)
                gradeText.text = "";
            
            if (progressBar != null)
            {
                progressBar.value = 0;
                progressBar.maxValue = 1;
            }
            
            // Generate notes for the song
            GenerateNotes();
            
            // Start playing the song
            if (musicSource != null && currentSong != null && currentSong.songClip != null)
            {
                musicSource.clip = currentSong.songClip;
                musicSource.Play();
                Debug.Log("Started playing music");
            }
            else
            {
                Debug.LogWarning("No music source or song clip available!");
            }
            
            // Start game timer
            isPlaying = true;
            gameStartTime = Time.time;
            StartCoroutine(GameTimer());
        }

        private IEnumerator GameTimer()
        {
            float startTime = Time.time;
            float elapsedTime = 0f;
            
            while (isPlaying && elapsedTime < gameDuration)
            {
                elapsedTime = Time.time - startTime;
                songPosition = elapsedTime;
                
                // Update progress bar
                if (progressBar != null)
                {
                    progressBar.value = elapsedTime / gameDuration;
                }
                
                yield return null;
            }
            
            // Game finished
            if (isPlaying)
            {
                EndGame(false);
            }
        }

        // EndGame method is already defined elsewhere in the file

        private void GenerateNotes()
        {
            Debug.Log("Generating notes for rhythm game...");
            
            // If we have a song with BPM info, use that for better rhythm-based generation
            if (currentSong != null && currentSong.bpm > 0)
            {
                GenerateRhythmBasedNotes();
            }
            else
            {
                // Fallback to simple time-based generation
                GenerateSimpleNotes();
            }
        }

        private void GenerateRhythmBasedNotes()
        {
            if (currentSong == null || currentSong.bpm <= 0) return;
            
            Debug.Log($"Generating rhythm-based notes for song: {currentSong.songName} with BPM: {currentSong.bpm}");
            
            float bpm = currentSong.bpm;
            float beatDuration = 60f / bpm; // Duration of one beat in seconds
            float songOffset = currentSong.offset;
            
            // Calculate total beats in the song
            int totalBeats = Mathf.FloorToInt(gameDuration / beatDuration);
            Debug.Log($"Song has {totalBeats} beats with duration {gameDuration} seconds");
            
            // Determine note density based on settings
            float density = noteGenSettings.density / 10f; // Convert 1-10 scale to 0.1-1.0
            
            // Store notes in the currentSong.notes list so they're visible in the inspector
            currentSong.notes.Clear();
            
            // Generate notes on beats
            for (int beat = 0; beat < totalBeats; beat++)
            {
                // Determine if we should place a note on this beat based on density
                if (UnityEngine.Random.value < density)
                {
                    // Determine how many notes to place at this beat (1, 2, or 3)
                    int noteCount = 1;
                    
                    // Chance for double notes
                    if (UnityEngine.Random.Range(0, 100) < noteGenSettings.chanceOfDoubleNote)
                    {
                        noteCount = 2;
                    }
                    // Chance for triple notes
                    else if (UnityEngine.Random.Range(0, 100) < noteGenSettings.chanceOfTripleNote)
                    {
                        noteCount = 3;
                    }
                    
                    // Create unique lanes for this beat
                    List<int> usedLanes = new List<int>();
                    
                    for (int i = 0; i < noteCount; i++)
                    {
                        // Find an unused lane
                        int lane;
                        do
                        {
                            lane = UnityEngine.Random.Range(0, 4);
                        } while (usedLanes.Contains(lane));
                        
                        usedLanes.Add(lane);
                        
                        // Calculate time for this note
                        float time = songOffset + (beat * beatDuration);
                        
                        // Add some randomness if enabled
                        if (noteGenSettings.randomness > 0)
                        {
                            time += UnityEngine.Random.Range(-noteGenSettings.randomness, noteGenSettings.randomness) * beatDuration * 0.25f;
                        }
                        
                        // Determine if this should be a hold note
                        float duration = 0f;
                        if (UnityEngine.Random.Range(0, 100) < noteGenSettings.chanceOfHoldNote)
                        {
                            duration = noteGenSettings.holdNoteDuration * beatDuration;
                        }
                        
                        // Create the note
                        RhythmNote note = new RhythmNote
                        {
                            lane = lane,
                            time = time,
                            duration = duration
                        };
                        
                        // Add to song's notes list
                        currentSong.notes.Add(note);
                        
                        // Schedule note spawn
                        StartCoroutine(SpawnNoteAtTime(note));
                    }
                }
            }
            
            Debug.Log($"Generated {currentSong.notes.Count} notes for the song");
        }

        private IEnumerator SpawnNoteAtTime(RhythmNote note)
        {
            // CRITICAL FIX: Use a fixed time to travel (2 seconds) for consistency
            float timeToTravel = 2.0f; // Notes take 2 seconds to travel from spawn to target
            
            // Calculate when to spawn this note
            float spawnTime = note.time - timeToTravel;
            
            // Log the note spawn scheduling
            Debug.Log($"Scheduling note in lane {note.lane} to spawn at time {spawnTime}, target hit time: {note.time}");
            
            // Wait until it's time to spawn
            float timeUntilSpawn = spawnTime - songPosition;
            if (timeUntilSpawn > 0)
            {
                Debug.Log($"Waiting {timeUntilSpawn} seconds to spawn note in lane {note.lane}");
                yield return new WaitForSeconds(timeUntilSpawn);
            }
            
            // Only spawn if we're still playing
            if (isPlaying)
            {
                // Spawn the note
                SpawnNote(note);
                Debug.Log($"Spawned note in lane {note.lane} at time {songPosition}, should be hit at {note.time}");
                
                // CRITICAL FIX: Force an immediate update of the note's position
                // This ensures the note is correctly positioned right after spawning
                foreach (GameObject noteObj in activeNotes)
                {
                    if (noteObj == null) continue;
                    
                    NoteController noteController = noteObj.GetComponent<NoteController>();
                    if (noteController != null && noteController.Lane == note.lane && Mathf.Abs(noteController.TargetTime - note.time) < 0.01f)
                    {
                        // Calculate the initial progress
                        float progress = 0f; // Start at spawn point
                        Vector3 startPos = laneSpawnPoints[note.lane].position;
                        Vector3 endPos = laneTargets[note.lane].position;
                        
                        // Set the initial position
                        noteObj.transform.position = startPos;
                        Debug.Log($"Forced initial position of note in lane {note.lane} to {startPos}");
                        break;
                    }
                }
            }
            else
            {
                Debug.Log("Game no longer playing, note spawn canceled");
            }
        }

        private void SpawnNote(RhythmNote note)
        {
            if (!isPlaying || notePrefab == null || note.lane < 0 || note.lane >= 4) 
            {
                Debug.LogWarning($"Cannot spawn note: isPlaying={isPlaying}, notePrefab={(notePrefab != null ? "valid" : "null")}, lane={note.lane}");
                return;
            }
            
            // Make sure the UI is active
            if (rhythmGameUI == null || !rhythmGameUI.activeInHierarchy)
            {
                Debug.LogError("Cannot spawn note: Rhythm game UI is null or inactive");
                return;
            }
            
            // Make sure lane spawn points are set up
            if (laneSpawnPoints == null || laneSpawnPoints.Length <= note.lane || laneSpawnPoints[note.lane] == null)
            {
                Debug.LogError($"Cannot spawn note: Lane spawn point {note.lane} is not set up properly");
                return;
            }
            
            // CRITICAL FIX: Create the note as a direct child of the Canvas
            Canvas mainCanvas = rhythmGameUI.GetComponentInParent<Canvas>();
            if (mainCanvas == null)
            {
                Debug.LogError("Cannot find Canvas component in parent hierarchy");
                return;
            }
            
            // Instantiate the note as a direct child of the Canvas
            GameObject noteObj = Instantiate(notePrefab, mainCanvas.transform);
            
            // Ensure the note is active and visible
            noteObj.SetActive(true);
            
            // Get the RectTransform component
            RectTransform noteRect = noteObj.GetComponent<RectTransform>();
            if (noteRect != null)
            {
                // CRITICAL FIX: Set the anchoring and pivot for proper UI positioning
                noteRect.anchorMin = new Vector2(0.5f, 0.5f);
                noteRect.anchorMax = new Vector2(0.5f, 0.5f);
                noteRect.pivot = new Vector2(0.5f, 0.5f);
                
                // Set initial position at spawn point
                noteRect.position = laneSpawnPoints[note.lane].position;
                
                // Make sure the note is properly sized and visible
                noteRect.sizeDelta = new Vector2(80, 80); // Larger size to ensure visibility
                
                Debug.Log($"Spawned note at lane {note.lane}, position: {noteRect.position}, size: {noteRect.sizeDelta}");
            }
            else
            {
                Debug.LogError("Note prefab is missing RectTransform component!");
                Destroy(noteObj);
                return;
            }
            
            // Set up the note's properties
            NoteController noteController = noteObj.GetComponent<NoteController>();
            if (noteController != null)
            {
                noteController.Lane = note.lane;
                noteController.TargetTime = note.time;
                noteController.Duration = note.duration;
                Debug.Log($"Note controller set up: Lane={note.lane}, TargetTime={note.time}, Duration={note.duration}");
            }
            else
            {
                Debug.LogError("Note prefab is missing NoteController component!");
                Destroy(noteObj);
                return;
            }
            
            // Set the note's appearance based on lane
            Image noteImage = noteObj.GetComponent<Image>();
            if (noteImage != null)
            {
                // CRITICAL FIX: Ensure the image is visible
                noteImage.enabled = true;
                noteImage.raycastTarget = false; // No need for raycasting
                
                // Different color for each lane with full opacity
                switch (note.lane)
                {
                    case 0:
                        noteImage.color = new Color(1, 0, 0, 1); // Red
                        break;
                    case 1:
                        noteImage.color = new Color(0, 1, 0, 1); // Green
                        break;
                    case 2:
                        noteImage.color = new Color(0, 0, 1, 1); // Blue
                        break;
                    case 3:
                        noteImage.color = new Color(1, 1, 0, 1); // Yellow
                        break;
                }
            }
            else
            {
                Debug.LogError("Note prefab is missing Image component!");
                Destroy(noteObj);
                return;
            }
            
            // Add to active notes
            activeNotes.Add(noteObj);
            
            Debug.Log($"Note successfully spawned in lane {note.lane} at position {noteObj.transform.position}");
        }

        void GenerateSimpleNotes()
        {
            // Calculate how many notes to generate based on game duration
            int totalNotes = Mathf.RoundToInt(gameDuration * 1.5f); // About 1.5 notes per second
            
            for (int i = 0; i < totalNotes; i++)
            {
                // Create a note
                RhythmNote note = new RhythmNote
                {
                    lane = UnityEngine.Random.Range(0, 4),
                    time = (i + 1) * (gameDuration / totalNotes),
                    duration = 0f // Tap notes for small enemies
                };
                
                // Schedule note spawn
                StartCoroutine(SpawnNoteAtTime(note));
            }
        }

        private void ProcessLanePress(int lane)
        {
            Debug.Log($"Lane {lane} pressed at time {songPosition}");
            
            // Find the closest note in this lane
            GameObject closestNote = null;
            float closestTime = float.MaxValue;
            
            foreach (var note in activeNotes)
            {
                if (note == null) continue;
                
                NoteController noteController = note.GetComponent<NoteController>();
                if (noteController != null && noteController.Lane == lane)
                {
                    float timeDifference = Mathf.Abs(noteController.TargetTime - songPosition);
                    Debug.Log($"Found note in lane {lane} with time difference: {timeDifference * 1000f}ms");
                    
                    if (timeDifference < closestTime)
                    {
                        closestTime = timeDifference;
                        closestNote = note;
                    }
                }
            }
            
            // If we found a note and it's within the hit window
            if (closestNote != null)
            {
                float timeDiffMs = closestTime * 1000f; // Convert to milliseconds
                Debug.Log($"Closest note time difference: {timeDiffMs}ms (windows: perfect={perfectWindow}, good={goodWindow}, okay={okayWindow})");
                
                if (timeDiffMs <= perfectWindow)
                {
                    // Perfect hit
                    currentScore += 100;
                    currentCombo++;
                    perfectHits++;
                    ShowHitFeedback("PERFECT", Color.magenta);
                    Debug.Log("PERFECT hit!");
                }
                else if (timeDiffMs <= goodWindow)
                {
                    // Good hit
                    currentScore += 75;
                    currentCombo++;
                    goodHits++;
                    ShowHitFeedback("GOOD", Color.green);
                    Debug.Log("GOOD hit!");
                }
                else if (timeDiffMs <= okayWindow)
                {
                    // Okay hit
                    currentScore += 50;
                    currentCombo++;
                    okayHits++;
                    ShowHitFeedback("OKAY", Color.yellow);
                    Debug.Log("OKAY hit!");
                }
                else
                {
                    // Too far off, count as a miss
                    // Don't break combo, but don't increase score much
                    currentScore += 5;
                    ShowHitFeedback("MISS", Color.red);
                    missedHits++;
                    currentCombo = 0;
                    Debug.Log("MISS - too far from hit window!");
                }
                
                // Update max combo
                if (currentCombo > maxCombo)
                    maxCombo = currentCombo;
                
                // Remove the note
                activeNotes.Remove(closestNote);
                Destroy(closestNote);
                
                // Update UI
                UpdateScoreUI();
            }
            else
            {
                Debug.Log("No note found in this lane to hit!");
            }
        }

        private void ShowHitFeedback(string text, Color color)
        {
            // This would be implemented to show feedback text
            Debug.Log($"Hit Feedback: {text}");
        }

        // LEGACY: CheckForMissedNotes is replaced by CheckMissedNotes (see above) and is no longer used.
        // Please use CheckMissedNotes for all miss detection logic.
        private void CheckForMissedNotes()
        {
            List<GameObject> notesToRemove = new List<GameObject>();
            
            foreach (var note in activeNotes)
            {
                if (note == null) continue;
                
                NoteController noteController = note.GetComponent<NoteController>();
                if (noteController != null)
                {
                    // If the note's target time has passed by more than the okay window, it's a miss
                    float timeDifference = songPosition - noteController.TargetTime;
                    if (timeDifference > (okayWindow / 1000f))
                    {
                        // Missed note
                        missedHits++;
                        currentCombo = 0;
                        notesToRemove.Add(note);
                        
                        // Show miss feedback
                        ShowHitFeedback("MISS", Color.red);
                        
                        // Update UI
                        UpdateScoreUI();
                        
                        Debug.Log($"Missed note in lane {noteController.Lane} at time {noteController.TargetTime}, current time: {songPosition}");
                    }
                }
            }
            
            // Remove missed notes
            foreach (var note in notesToRemove)
            {
                if (note != null)
                {
                    activeNotes.Remove(note);
                    Destroy(note);
                }
            }
        }

        private void UpdateScoreUI()
        {
            if (scoreText != null)
                scoreText.text = currentScore.ToString();
            
            if (comboText != null)
                comboText.text = currentCombo.ToString();
            
            if (accuracyText != null)
            {
                int totalNotes = perfectHits + goodHits + okayHits + missedHits;
                if (totalNotes > 0)
                {
                    float accuracy = (float)(perfectHits * 100 + goodHits * 75 + okayHits * 50) / (totalNotes * 100) * 100f;
                    accuracyText.text = $"Accuracy: {accuracy:F1}%";
                }
                else
                {
                    accuracyText.text = "Accuracy: 0%";
                }
            }
            
            if (gradeText != null)
            {
                int totalNotes = perfectHits + goodHits + okayHits + missedHits;
                if (totalNotes > 0)
                {
                    float accuracy = (float)(perfectHits * 100 + goodHits * 75 + okayHits * 50) / (totalNotes * 100) * 100f;
                    
                    string grade;
                    if (accuracy >= 95f)
                        grade = "S";
                    else if (accuracy >= 90f)
                        grade = "A+";
                    else if (accuracy >= 80f)
                        grade = "A";
                    else if (accuracy >= 70f)
                        grade = "B";
                    else if (accuracy >= 60f)
                        grade = "C";
                    else if (accuracy >= 50f)
                        grade = "D";
                    else
                        grade = "F";
                    
                    gradeText.text = grade;
                }
            }
        }

        private void EndGame(bool playerWon)
        {
            Debug.Log("Rhythm game ended");
            
            isPlaying = false;
            
            // Stop the music
            if (musicSource != null)
                musicSource.Stop();
            
            // Hide the rhythm game UI
            if (rhythmGameUI != null)
                rhythmGameUI.SetActive(false);
            
            // Calculate results
            int totalNotes = perfectHits + goodHits + okayHits + missedHits;
            float accuracy = totalNotes > 0 ? (float)(perfectHits * 100 + goodHits * 75 + okayHits * 50) / (totalNotes * 100) * 100f : 0f;
            
            // Determine if player won
            // For small enemies, winning condition is simpler - just need decent accuracy
            PlayerWon = accuracy >= 60f; // 60% accuracy or better to win
            
            // Calculate damage
            int damageToEnemy = 0;
            int damageToPlayer = 0;
            
            if (PlayerWon)
            {
                damageToEnemy = CalculateDamageToEnemy();
                
                // Apply damage to enemy
                if (currentEnemy != null)
                {
                    currentEnemy.TakeDamage(damageToEnemy);
                    
                    // Show knockback effect
                    ShowEnemyKnockbackEffect();
                    
                    // Check if the enemy died from this damage (health <= 0)
                    if (currentEnemy.CurrentHealth <= 0)
                    {
                        ShowEnemyDeathEffect();
                    }
                }
            }
            else
            {
                damageToPlayer = CalculateDamageToPlayer();
                
                // Apply damage to player
                if (player != null && player.Stats != null)
                {
                    player.Stats.TakeDamage(damageToPlayer);
                }
            }
            
            // Log results
            Debug.Log($"Game Results - Score: {currentScore}, Accuracy: {accuracy:F1}%, Player Won: {PlayerWon}");
            Debug.Log($"Hit Breakdown - Perfect: {perfectHits}, Good: {goodHits}, Okay: {okayHits}, Miss: {missedHits}");
            Debug.Log($"Damage - To Enemy: {damageToEnemy}, To Player: {damageToPlayer}");
            
            // Re-enable player movement
            if (player != null && player.Input != null)
            {
                player.Input.EnableMovement();
            }
            
            // Notify enemy that combat has ended
            if (currentEnemy != null)
            {
                currentEnemy.OnCombatEnd(PlayerWon);
            }
        }

        private int CalculateDamageToEnemy()
        {
            // Base damage
            int baseDamage = 10;
            
            // Score multiplier (0.0 - 1.0)
            float scoreMultiplier = (float)currentScore / 10000f; // Assuming 10000 is a perfect score
            scoreMultiplier = Mathf.Clamp01(scoreMultiplier);
            
            // Combo multipliers
            float comboMultiplier = 1f;
            if (IsAllPerfect)
                comboMultiplier = 5f; // All perfect = 5x damage
            else if (IsFullCombo)
                comboMultiplier = 2f; // Full combo = 2x damage
            
            // Critical hit chance based on player's crit stat
            int critChance = player != null ? player.Stats.CritChance : 5;
            bool isCritical = UnityEngine.Random.Range(0, 100) < critChance;
            
            // Critical damage multiplier
            float critMultiplier = 1f;
            if (isCritical)
            {
                critMultiplier = player != null ? player.Stats.CritDamage / 100f : 1.5f;
            }
            
            // Calculate final damage
            int damage = Mathf.RoundToInt(baseDamage * (1f + scoreMultiplier) * comboMultiplier * critMultiplier);
            
            // Ensure minimum damage of 1
            return Mathf.Max(1, damage);
        }

        private int CalculateDamageToPlayer()
        {
            // Base damage from misses
            int baseDamage = missedHits * 2;
            
            // Enemy strength factor (would come from enemy stats)
            int enemyStrength = currentEnemy != null ? currentEnemy.Strength : 5;
            
            // Player defense reduction
            int playerDefense = player != null ? player.Stats.Defense : 5;
            
            // Calculate damage with defense reduction
            int damage = Mathf.RoundToInt(baseDamage * (enemyStrength / 10f));
            
            // Apply defense reduction
            damage -= playerDefense;
            
            // Ensure minimum damage of 1
            return Mathf.Max(1, damage);
        }

        private void ShowEnemyDeathEffect()
        {
            if (currentEnemy == null) return;
            
            // Use the CombatEffectsManager to play the death effect
            if (CombatEffectsManager.Instance != null)
            {
                CombatEffectsManager.Instance.PlayDeathEffect(currentEnemy.transform.position);
            }
            else
            {
                // Fallback if manager not found
                Debug.Log("Enemy death effect shown");
            }
        }

        private void ShowEnemyKnockbackEffect()
        {
            if (currentEnemy == null) 
            {
                Debug.LogError("Cannot show knockback effect - enemy is null");
                return;
            }
            
            Debug.Log("Applying knockback effect to enemy");
            
            // Calculate knockback direction (away from player)
            Vector3 knockbackDirection = (currentEnemy.transform.position - player.transform.position).normalized;
            knockbackDirection.y = 0.2f; // Add slight upward force
            
            // Log the knockback parameters
            Debug.Log($"Knockback direction: {knockbackDirection}, force: 10.0");
            
            // CRITICAL FIX: First play the visual effect at the enemy's position
            if (CombatEffectsManager.Instance != null)
            {
                Debug.Log("Playing knockback visual effect");
                // Play the visual effect at the enemy's position
                CombatEffectsManager.Instance.PlayKnockbackEffect(currentEnemy.transform.position);
            }
            
            // Then apply the actual knockback force
            Debug.Log("Applying physical knockback to enemy");
            currentEnemy.ApplyKnockback(knockbackDirection, 10f);
        }
        
        private IEnumerator ScaleTextAnimation(GameObject textObject, float duration)
        {
            float elapsedTime = 0f;
            Vector3 startScale = Vector3.zero;
            Vector3 targetScale = Vector3.one;
            
            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                // Add a slight bounce effect (similar to easeOutBack)
                float tModified = t * (1 + (1 - t) * 0.5f);
                textObject.transform.localScale = Vector3.Lerp(startScale, targetScale, tModified);
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Ensure we end at exactly the target scale
            textObject.transform.localScale = targetScale;
        }
    }
}