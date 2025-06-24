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

        [Header("Post-Battle Banter")]
        [SerializeField] private bool postBattleBanter = true; // Default checked
        [Tooltip("Dialog lines to randomly choose from after battle. Assign 5 or more banter lines here.")]
        [SerializeField] private List<DialogLine> postBattleBanterLines = new List<DialogLine>();

        [Header("First Boss Defeat")] // UPDATED
        [Tooltip("Dialog sequence to show after defeating the first boss.")]
        [SerializeField] private List<DialogLine> firstBossDefeatDialog = new List<DialogLine>();
        [Tooltip("Assign the specific Enemy object that is the first boss.")]
        [SerializeField] private Enemy firstBossEnemy;
        [Tooltip("Reference to AdvancedLoadingBar for async scene loading (optional)")]
        [SerializeField] private AdvancedLoadingBar loadingBar;
        [Tooltip("Name of the WorldMap scene to load after first boss dialog.")]
        [SerializeField] private string worldMapSceneName = "WorldMap";

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

        // Track running note spawn coroutines
        private List<Coroutine> noteSpawnCoroutines = new List<Coroutine>();

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
            foreach (var noteObj in activeNotes)
            {
                if (noteObj == null) continue;
                NoteController nc = noteObj.GetComponent<NoteController>();
                if (nc == null) continue;
                int lane = nc.Lane;
                float noteTime = nc.TargetTime;

                // Calculate distance and travel time based on noteSpeed
                Vector3 startPos = laneSpawnPoints[lane].position;
                Vector3 endPos = laneTargets[lane].position;
                float distance = Vector3.Distance(startPos, endPos);
                float travelTime = distance / noteSpeed; // seconds to reach target
                float timeSinceSpawn = songPosition - (noteTime - travelTime);
                float progress = Mathf.Clamp01(timeSinceSpawn / travelTime);
                noteObj.transform.position = Vector3.Lerp(startPos, endPos, progress);
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
                    ShowHitFeedback(nc.Lane, "MISS", Color.red);
                    UpdateScoreUI();
                    // MISS: 5x previous damage (was 1, now 5)
                    if (player != null && player.Stats != null)
                        player.Stats.TakeDamage(5);
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
            currentSong = null;
            Enemy currentEnemyRef = currentEnemy; // Use the current enemy reference if available
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
            // If no song was found or specified, pick a random one that matches the enemy (or any if none assigned)
            if (currentSong == null && availableSongs.Count > 0)
            {
                List<SongData> filtered = new List<SongData>();
                foreach (var song in availableSongs)
                {
                    if (song.allowedEnemies == null || song.allowedEnemies.Count == 0 || (currentEnemyRef != null && song.allowedEnemies.Contains(currentEnemyRef)))
                        filtered.Add(song);
                }
                if (filtered.Count == 0)
                {
                    filtered = availableSongs; // fallback to all if none match
                }
                int randomIndex = UnityEngine.Random.Range(0, filtered.Count);
                currentSong = filtered[randomIndex];
                Debug.Log($"Selected random song: {currentSong.songName} (index {randomIndex} of {filtered.Count})");
            }
            else if (availableSongs.Count == 0)
            {
                Debug.LogError("No songs available in the availableSongs list! Please add songs in the inspector.");
            }
        }

        private void StartActualGame()
        {
            Debug.Log("Starting rhythm game...");
            // Set gameDuration from per-song override or clip length
            if (currentSong != null && currentSong.songClip != null)
            {
                gameDuration = (currentSong.customDuration > 0f) ? currentSong.customDuration : currentSong.songClip.length;
            }
            else
            {
                gameDuration = 15f;
            }
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
            // Start game timer and set state BEFORE generating notes
            isPlaying = true;
            gameStartTime = Time.time;
            StartCoroutine(GameTimer());
            // Now generate notes for the song (so coroutines use correct songPosition)
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
        }

        private IEnumerator GameTimer()
        {
            while (isPlaying)
            {
                // Update progress bar
                if (progressBar != null && gameDuration > 0)
                {
                    progressBar.value = Mathf.Clamp01((Time.time - gameStartTime) / gameDuration);
                }
                // End game and stop music if songPosition exceeds gameDuration
                songPosition = Time.time - gameStartTime;
                if (songPosition >= gameDuration)
                {
                    if (musicSource != null && musicSource.isPlaying)
                        musicSource.Stop();
                    EndGame(true);
                    yield break;
                }
                yield return null;
            }
        }

        // EndGame method is already defined elsewhere in the file

        private void GenerateNotes()
        {
            Debug.Log("Generating notes for rhythm game...");
            if (currentSong == null) return;
            // Stop all running note spawn coroutines from previous encounters
            foreach (var coroutine in noteSpawnCoroutines)
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
            }
            noteSpawnCoroutines.Clear();
            // Clear any active notes from previous encounters
            foreach (var noteObj in activeNotes)
            {
                if (noteObj != null)
                    Destroy(noteObj);
            }
            activeNotes.Clear();
            // Always generate a fresh set of notes for every encounter
            // Set gameDuration from per-song override or clip length
            if (currentSong != null && currentSong.songClip != null)
            {
                gameDuration = (currentSong.customDuration > 0f) ? currentSong.customDuration : currentSong.songClip.length;
            }
            else
            {
                gameDuration = 15f;
            }
            // Use advanced pattern generator
            GeneratePatternedNotes();
        }

        // Advanced pattern-based generator for challenging rhythm game charts
        private void GeneratePatternedNotes()
        {
            if (currentSong == null || currentSong.bpm <= 0) return;
            Debug.Log($"[PatternGen] Generating grid-based notes for song: {currentSong.songName} BPM: {currentSong.bpm}");
            float bpm = currentSong.bpm;
            float beatDuration = 60f / bpm;
            // Add 1.5s of emptiness at the start for player reaction
            float songOffset = currentSong.offset + 1.5f;
            float songLength = gameDuration;
            int difficulty = Mathf.Clamp(noteGenSettings.density, 1, 64);

            // --- Rhythm grid calculation ---
            List<float> rhythmSteps = new List<float>();
            if (noteGenSettings.useQuarterNotes) rhythmSteps.Add(1f);
            if (noteGenSettings.useEighthNotes) rhythmSteps.Add(0.5f);
            if (noteGenSettings.useSixteenthNotes) rhythmSteps.Add(0.25f);
            if (noteGenSettings.useTriplets) rhythmSteps.Add(1f / 3f);
            if (rhythmSteps.Count == 0) rhythmSteps.Add(1f);

            // Find smallest subdivision
            float smallestStep = 1f;
            foreach (var step in rhythmSteps) if (step < smallestStep) smallestStep = step;

            float gridStep = smallestStep * beatDuration;
            int numGridSlots = Mathf.FloorToInt(songLength / gridStep);

            // Fill rate: at diff 32, fill ~95% of slots; lower diffs fill less
            float fillRate = Mathf.Lerp(0.3f, 0.95f, Mathf.Clamp01((difficulty - 1f) / 31f));

            List<RhythmNote> notes = new List<RhythmNote>();
            System.Random rng = new System.Random();
            int lastLane = -1;
            float burstChance = Mathf.Lerp(0.05f, 0.25f, (difficulty - 1f) / 31f);
            float jackChance = Mathf.Lerp(0.02f, 0.15f, (difficulty - 1f) / 31f);
            float chordChance = Mathf.Lerp(0.10f, 0.25f, (difficulty - 1f) / 31f);
            float minInterval = 0.12f - 0.06f * ((difficulty - 1f) / 31f);

            for (int i = 0; i < numGridSlots; i++)
            {
                float t = songOffset + i * gridStep;
                if (t > songLength + songOffset) break;
                if (rng.NextDouble() > fillRate) continue; // skip slot for rests

                int lane = rng.Next(4);
                double patternRoll = rng.NextDouble();
                if (lastLane == lane && rng.NextDouble() < jackChance)
                {
                    RhythmNote jackNote = new RhythmNote
                    {
                        lane = lane,
                        time = t,
                        duration = 0f
                    };
                    notes.Add(jackNote);
                }
                else if (patternRoll < burstChance)
                {
                    int burstLen = rng.Next(3, 6);
                    float burstStep = minInterval * Mathf.Lerp(1f, 0.6f, (difficulty - 1f) / 31f);
                    for (int b = 0; b < burstLen && t + b * burstStep < songLength + songOffset; b++)
                    {
                        int burstLane = rng.Next(4);
                        RhythmNote burstNote = new RhythmNote
                        {
                            lane = burstLane,
                            time = t + b * burstStep,
                            duration = 0f
                        };
                        notes.Add(burstNote);
                    }
                }
                else if (patternRoll < burstChance + chordChance)
                {
                    int chordSize = rng.Next(2, 4);
                    HashSet<int> chordLanes = new HashSet<int>();
                    while (chordLanes.Count < chordSize) chordLanes.Add(rng.Next(4));
                    foreach (int chordLane in chordLanes)
                    {
                        RhythmNote chordNote = new RhythmNote
                        {
                            lane = chordLane,
                            time = t,
                            duration = 0f
                        };
                        notes.Add(chordNote);
                    }
                }
                else
                {
                    RhythmNote note = new RhythmNote
                    {
                        lane = lane,
                        time = t,
                        duration = 0f
                    };
                    notes.Add(note);
                }
                lastLane = lane;
                if (noteGenSettings.chanceOfHoldNote > 0 && rng.Next(100) < noteGenSettings.chanceOfHoldNote)
                {
                    notes[notes.Count - 1].duration = noteGenSettings.holdNoteDuration * beatDuration;
                }
            }
            notes.Sort((a, b) => a.time.CompareTo(b.time));

            // --- Post-process to prevent unfair note clumping ---
            float minSpacing = 0.2f; // 200ms
            // For each lane, track the last note time
            float[] lastNoteTimePerLane = new float[4];
            for (int i = 0; i < 4; i++) lastNoteTimePerLane[i] = float.NegativeInfinity;

            for (int i = 0; i < notes.Count; i++)
            {
                RhythmNote note = notes[i];
                float lastTime = lastNoteTimePerLane[note.lane];
                if (note.time - lastTime < minSpacing)
                {
                    // Try to move to another lane with enough spacing
                    bool moved = false;
                    for (int lane = 0; lane < 4; lane++)
                    {
                        if (lane == note.lane) continue;
                        // Find last note in this lane before this note
                        float prevTime = float.NegativeInfinity;
                        for (int j = i - 1; j >= 0; j--)
                        {
                            if (notes[j].lane == lane) { prevTime = notes[j].time; break; }
                        }
                        if (note.time - prevTime >= minSpacing)
                        {
                            note.lane = lane;
                            lastNoteTimePerLane[lane] = note.time;
                            moved = true;
                            break;
                        }
                    }
                    if (!moved)
                    {
                        // Push this note forward in time
                        float newTime = lastTime + minSpacing;
                        note.time = newTime;
                        lastNoteTimePerLane[note.lane] = newTime;
                    }
                }
                else
                {
                    lastNoteTimePerLane[note.lane] = note.time;
                }
            }
            notes.Sort((a, b) => a.time.CompareTo(b.time)); // Resort in case of time changes
            // Do not persist notes in currentSong.notes; just use local notes for this encounter
            foreach (var n in notes)
            {
                Coroutine c = StartCoroutine(SpawnNoteAtTime(n));
                noteSpawnCoroutines.Add(c);
            }
            Debug.Log($"[PatternGen] Generated {notes.Count} notes for the song");
        }

        private IEnumerator SpawnNoteAtTime(RhythmNote note)
        {
            // Calculate distance and travel time based on noteSpeed
            Vector3 startPos = laneSpawnPoints[note.lane].position;
            Vector3 endPos = laneTargets[note.lane].position;
            float distance = Vector3.Distance(startPos, endPos);
            float timeToTravel = distance / noteSpeed; // seconds to reach target

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
                // Force an immediate update of the note's position
                foreach (GameObject noteObj in activeNotes)
                {
                    if (noteObj == null) continue;
                    NoteController noteController = noteObj.GetComponent<NoteController>();
                    if (noteController != null && noteController.Lane == note.lane && Mathf.Abs(noteController.TargetTime - note.time) < 0.01f)
                    {
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

            // Instantiate the note and parent it to the rhythmGameUI for proper UI hierarchy (like RhythmGameController)
            GameObject noteObj = Instantiate(notePrefab);
            noteObj.transform.SetParent(rhythmGameUI.transform, false);
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
                    ShowHitFeedback(lane, "PERFECT", Color.magenta);
                    Debug.Log("PERFECT hit!");
                }
                else if (timeDiffMs <= goodWindow)
                {
                    // Good hit
                    currentScore += 75;
                    currentCombo++;
                    goodHits++;
                    ShowHitFeedback(lane, "GOOD", Color.green);
                    Debug.Log("GOOD hit!");
                }
                else if (timeDiffMs <= okayWindow)
                {
                    // Okay hit
                    currentScore += 50;
                    currentCombo++;
                    okayHits++;
                    ShowHitFeedback(lane, "OKAY", Color.yellow);
                    Debug.Log("OKAY hit!");
                }
                else
                {
                    // Too far off, count as a BAD (early/late tap)
                    currentScore += 5;
                    ShowHitFeedback(lane, "BAD", Color.red);
                    missedHits++;
                    currentCombo = 0;
                    // BAD: 2x miss damage (miss = 5, bad = 10)
                    if (player != null && player.Stats != null)
                        player.Stats.TakeDamage(10);
                    Debug.Log("BAD - too far from hit window!");
                }

                // Update max combo
                if (currentCombo > maxCombo)
                    maxCombo = currentCombo;

                activeNotes.Remove(closestNote);
                Destroy(closestNote);
                UpdateScoreUI();
            }
            else
            {
                Debug.Log("No note found in this lane to hit!");
            }
        }

        // Show feedback at the exact pressed lane
        private void ShowHitFeedback(int laneIndex, string text, Color color)
        {
            Vector3 feedbackPos = laneTargets[laneIndex].position;
            GameObject feedbackObj = new GameObject("HitFeedback");
            feedbackObj.transform.SetParent(rhythmGameUI.transform, false);
            feedbackObj.transform.position = feedbackPos;

            var textComp = feedbackObj.AddComponent<TMPro.TextMeshProUGUI>();
            textComp.text = text;
            textComp.fontSize = 48;
            textComp.color = color;
            textComp.alignment = TMPro.TextAlignmentOptions.Center;
            textComp.fontStyle = TMPro.FontStyles.Bold;
            textComp.enableAutoSizing = true;

            // Animate and destroy
            StartCoroutine(AnimateHitFeedback(feedbackObj));
        }

        private IEnumerator AnimateHitFeedback(GameObject feedbackObj)
        {
            float duration = 0.7f;
            float startTime = Time.time;
            var text = feedbackObj.GetComponent<TMPro.TextMeshProUGUI>();
            Vector3 startPos = feedbackObj.transform.position;
            Vector3 endPos = startPos + Vector3.up * 80f;

            while (Time.time - startTime < duration)
            {
                float t = (Time.time - startTime) / duration;
                feedbackObj.transform.position = Vector3.Lerp(startPos, endPos, t);
                if (text != null)
                    text.color = new Color(text.color.r, text.color.g, text.color.b, 1 - t);
                yield return null;
            }
            Destroy(feedbackObj);
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

                        // Show miss feedback (use new signature)
                        ShowHitFeedback(noteController.Lane, "MISS", Color.red);

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
            if (musicSource != null)
                musicSource.Stop();
            if (rhythmGameUI != null)
                rhythmGameUI.SetActive(false);
            int totalNotes = perfectHits + goodHits + okayHits + missedHits;
            float accuracy = totalNotes > 0 ? (float)(perfectHits * 100 + goodHits * 75 + okayHits * 50) / (totalNotes * 100) * 100f : 0f;
            PlayerWon = accuracy >= 60f;
            int damageToEnemy = 0;
            int damageToPlayer = 0;
            if (PlayerWon)
            {
                damageToEnemy = CalculateDamageToEnemy();
                if (currentEnemy != null)
                {
                    currentEnemy.TakeDamage(damageToEnemy);
                    ShowEnemyKnockbackEffect();
                    if (currentEnemy.CurrentHealth <= 0)
                    {
                        ShowEnemyDeathEffect();
                        // Grant gold reward on enemy defeat
                        GrantGoldReward();
                        // --- FIRST BOSS DEFEAT LOGIC ---
                        if (IsFirstBoss(currentEnemy))
                        {
                            StartCoroutine(HandleFirstBossDefeatSequence());
                            return; // Don't continue normal banter/flow
                        }
                    }
                }
            }
            else
            {
                damageToPlayer = CalculateDamageToPlayer();
                if (player != null && player.Stats != null)
                {
                    player.Stats.TakeDamage(damageToPlayer);
                }
            }
            Debug.Log($"Game Results - Score: {currentScore}, Accuracy: {accuracy:F1}%, Player Won: {PlayerWon}");
            Debug.Log($"Hit Breakdown - Perfect: {perfectHits}, Good: {goodHits}, Okay: {okayHits}, Miss: {missedHits}");
            Debug.Log($"Damage - To Enemy: {damageToEnemy}, To Player: {damageToPlayer}");
            if (player != null && player.Input != null)
            {
                player.Input.EnableMovement();
            }
            if (currentEnemy != null)
            {
                currentEnemy.OnCombatEnd(PlayerWon);
            }
            // --- POST-BATTLE BANTER ---
            if (postBattleBanter && !IsBanterExcluded(currentEnemy))
            {
                TriggerPostBattleBanter();
            }
        }


        // Add gold reward calculation and grant logic
        private void GrantGoldReward()
        {
            if (currentEnemy == null || player == null || player.Stats == null) return;
            // Get base gold from enemy
            int baseGold = 10;
            if (currentEnemy.GetType().GetProperty("GoldReward") != null)
            {
                baseGold = (int)currentEnemy.GetType().GetProperty("GoldReward").GetValue(currentEnemy);
            }
            // Calculate performance multiplier (accuracy: 0.5x to 2x)
            int totalNotes = perfectHits + goodHits + okayHits + missedHits;
            float accuracy = totalNotes > 0 ? (float)(perfectHits * 100 + goodHits * 75 + okayHits * 50) / (totalNotes * 100) : 0f;
            float perfMultiplier = Mathf.Lerp(0.5f, 2f, accuracy / 100f);
            int goldReward = Mathf.RoundToInt(baseGold * perfMultiplier);
            // Grant gold
            player.Stats.AddGold(goldReward);
            // Optionally, show gold feedback UI here
            Debug.Log($"Granted {goldReward} gold (base {baseGold}, accuracy {accuracy:F1}%)");
        }

        // Returns true if this enemy should NOT get post-battle banter
        private bool IsBanterExcluded(Enemy enemy)
        {
            if (enemy == null) return true;
            // Example: check by tag, name, or a custom property
            if (enemy.CompareTag("Boss") || enemy.name == "FirstEnemy")
                return true;
            return false;
        }
        // Triggers a random post-battle dialog line
        private void TriggerPostBattleBanter()
        {
            if (postBattleBanterLines != null && postBattleBanterLines.Count > 0)
            {
                int idx = UnityEngine.Random.Range(0, postBattleBanterLines.Count);
                var banter = new List<DialogLine> { postBattleBanterLines[idx] };
                if (DialogManager.Instance != null)
                    DialogManager.Instance.ShowDialog(banter);
            }
        }

        // Returns true if this enemy is the first boss
        private bool IsFirstBoss(Enemy enemy)
        {
            return enemy != null && enemy == firstBossEnemy;
        }

        // Handles the sequence after defeating the first boss
        private IEnumerator HandleFirstBossDefeatSequence()
        {
            yield return new WaitForSeconds(1f);
            if (firstBossDefeatDialog != null && firstBossDefeatDialog.Count > 0)
            {
                bool dialogDone = false;
                DialogManager.Instance.ShowDialog(firstBossDefeatDialog, () => { dialogDone = true; });
                // Wait for dialog to finish
                while (!dialogDone) yield return null;
            }
            // Show loading screen and transfer to WorldMap
            if (loadingBar != null)
            {
                loadingBar.sceneToLoad = worldMapSceneName;
                loadingBar.StartLoading();
            }
            else
            {
                // Fallback: load directly
                UnityEngine.SceneManagement.SceneManager.LoadScene(worldMapSceneName);
            }
        }

        // --- HEALING/SHOP SYSTEM ---
        // To be called from a UI button (see Unity setup checklist)
        public void HealPlayer(int healAmount, int goldCost)
        {
            if (player == null || player.Stats == null) return;
            if (player.Stats.Gold < goldCost)
            {
                Debug.Log("Not enough gold to heal!");
                // Optionally show UI feedback
                return;
            }
            player.Stats.AddGold(-goldCost);
            player.Stats.Heal(healAmount);
            Debug.Log($"Player healed for {healAmount} HP, spent {goldCost} gold");
            // Optionally show heal feedback UI
        }
        // --- LEVEL UP FEEDBACK ---
        // To be called from PlayerStats.OnLevelUp event
        public void ShowLevelUpFeedback()
        {
            // Assumes LevelUpFeedbackUI is present in the scene and referenced
            var feedbackUI = GameObject.FindObjectOfType<EverdrivenDays.LevelUpFeedbackUI>();
            if (feedbackUI != null)
            {
                feedbackUI.ShowLevelUpFeedback();
            }
        }
        // --- UI REFERENCE CHECKLIST (for Unity setup) ---
        // 1. Assign all UI fields in the Inspector: musicSource, notePrefab, laneTargets, laneSpawnPoints, progressBar, scoreText, comboText, accuracyText, gradeText, encounterText, rhythmGameUI, encounterEffectUI.
        // 2. Add a gold display TextMeshProUGUI to your main HUD and bind it to CharacterStats.OnGoldChanged.
        // 3. Add a heal/shop panel with a heal button. Hook the button to call HealPlayer(healAmount, goldCost).
        // 4. Add LevelUpFeedbackUI to your UI canvas and assign its fields. In PlayerStats, call SmallEnemyRhythmController.ShowLevelUpFeedback() from OnLevelUp.
        // 5. Remove all item/inventory UI from the canvas.
        // 6. Test all UI connections in Play mode.
        public void SetPlayer(Player playerRef)
        {
            player = playerRef;
        }

        // --- RESTORED PRIVATE METHODS ---
        private int CalculateDamageToEnemy()
        {
            // Example: base damage + combo/accuracy bonuses
            int baseDamage = 10;
            float scoreMultiplier = (float)currentScore / 10000f;
            scoreMultiplier = Mathf.Clamp01(scoreMultiplier);
            float comboMultiplier = 1f;
            if (IsAllPerfect)
                comboMultiplier = 5f;
            else if (IsFullCombo)
                comboMultiplier = 2f;
            int critChance = player != null ? player.Stats.CritChance : 5;
            bool isCritical = UnityEngine.Random.Range(0, 100) < critChance;
            float critMultiplier = isCritical ? (player != null ? player.Stats.CritDamage / 100f : 1.5f) : 1f;
            // --- NEW: Multiply by number of perfect hits ---
            int perfectMultiplier = Mathf.Max(1, perfectHits); // At least 1
            int damage = Mathf.RoundToInt(baseDamage * (1f + scoreMultiplier) * comboMultiplier * critMultiplier * perfectMultiplier);
            return Mathf.Max(1, damage);
        }
        private int CalculateDamageToPlayer()
        {
            int baseDamage = missedHits * 2;
            int enemyStrength = currentEnemy != null ? currentEnemy.Strength : 5;
            int playerDefense = player != null ? player.Stats.Defense : 5;
            int damage = Mathf.RoundToInt(baseDamage * (enemyStrength / 10f));
            damage -= playerDefense;
            return Mathf.Max(1, damage);
        }
        private void ShowEnemyKnockbackEffect()
        {
            if (currentEnemy == null) return;
            Vector3 knockbackDirection = (currentEnemy.transform.position - player.transform.position).normalized;
            knockbackDirection.y = 0.2f;
            if (CombatEffectsManager.Instance != null)
                CombatEffectsManager.Instance.PlayKnockbackEffect(currentEnemy.transform.position);
            currentEnemy.ApplyKnockback(knockbackDirection, 10f);
        }
        private void ShowEnemyDeathEffect()
        {
            if (currentEnemy == null) return;
            if (CombatEffectsManager.Instance != null)
                CombatEffectsManager.Instance.PlayDeathEffect(currentEnemy.transform.position);
        }
        private IEnumerator ScaleTextAnimation(GameObject textObject, float duration)
        {
            float elapsedTime = 0f;
            Vector3 startScale = Vector3.zero;
            Vector3 targetScale = Vector3.one;
            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                float tModified = t * (1 + (1 - t) * 0.5f);
                textObject.transform.localScale = Vector3.Lerp(startScale, targetScale, tModified);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            textObject.transform.localScale = targetScale;
        }
    }
}