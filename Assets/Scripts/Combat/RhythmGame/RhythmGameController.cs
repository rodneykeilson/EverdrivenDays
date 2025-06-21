using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace EverdrivenDays
{
    [Serializable]
    public class RhythmNote
    {
        public int lane; // 0, 1, 2, or 3 (4-lane gameplay)
        public float time; // Time in seconds when the note should be hit
        public float duration = 0f; // For hold notes - 0 for tap notes
    }

    [Serializable]
    public class SongData
    {
        public string songName;
        public AudioClip songClip;
        public float bpm;
        public float offset = 0f; // Start offset in seconds
        public List<RhythmNote> notes = new List<RhythmNote>(); // Will be auto-generated if empty
        public int difficulty = 1; // 1-10 scale
        public bool generateNotes = true; // Whether to procedurally generate notes
        [Tooltip("Override the duration of this song for rhythm game (in seconds). 0 = use full song length.")]
        public float customDuration = 0f;
    }

    [Serializable]
    public class NoteGenerationSettings
    {
        [Header("Basic Settings")]
        public bool enabled = true;
        [Range(1, 10)]
        public int density = 5; // 1-10 scale of how many notes to generate
        [Range(0f, 1f)]
        public float randomness = 0.2f; // How much randomness to add to note timings
        
        [Header("Pattern Settings")]
        public bool usePatterns = true;
        [Range(0, 100)]
        public int chanceOfDoubleNote = 15; // % chance of having 2 notes at once
        [Range(0, 100)]
        public int chanceOfTripleNote = 5; // % chance of having 3 notes at once
        [Range(0, 100)]
        public int chanceOfHoldNote = 10; // % chance of generating a hold note
        [Range(0.1f, 2f)]
        public float holdNoteDuration = 0.5f; // Duration of hold notes in beats
        
        [Header("Rhythm Settings")]
        public bool useQuarterNotes = true; // Notes on quarter beats (1, 2, 3, 4)
        public bool useEighthNotes = true; // Notes on eighth beats (1, 1.5, 2, 2.5, etc)
        public bool useSixteenthNotes = false; // Notes on sixteenth beats
        public bool useTriplets = false; // Notes on triplet beats
        
        // Advanced settings (not shown in inspector)
        [HideInInspector]
        public bool useHighDensityPatterns = false; // For extremely difficult patterns
    }

    public class RhythmGameController : MonoBehaviour
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
        [SerializeField] private GameObject resultScreen;
        [SerializeField] private TextMeshProUGUI resultScoreText;
        [SerializeField] private TextMeshProUGUI resultComboText;
        [SerializeField] private TextMeshProUGUI resultGradeText;
        [SerializeField] private Button continueButton;

        [Header("Game Settings")]
        [SerializeField] private List<SongData> availableSongs = new List<SongData>();
        [SerializeField] private float noteSpeed = 500f; // Speed in units per second
        [SerializeField] private float perfectWindow = 30f; // Time in milliseconds (tighter than before)
        [SerializeField] private float goodWindow = 60f; // Time in milliseconds (tighter than before)
        [SerializeField] private float okayWindow = 90f; // Time in milliseconds (tighter than before)

        [Header("Procedural Generation")]
        [SerializeField] private NoteGenerationSettings noteGenSettings = new NoteGenerationSettings();

        [Header("Key Bindings")]
        [SerializeField] private KeyCode[] laneKeys = new KeyCode[4] { KeyCode.D, KeyCode.F, KeyCode.J, KeyCode.K };

        // Game state
        private bool isPlaying = false;
        private float gameStartTime;
        private float currentPlayTime;
        private SongData currentSong;
        private List<GameObject> activeNotes = new List<GameObject>();
        private List<RhythmNote> remainingNotes = new List<RhythmNote>();
        private int currentScore = 0;
        private int maxCombo = 0;
        private int currentCombo = 0;
        private int perfectHits = 0;
        private int goodHits = 0;
        private int okayHits = 0;
        private int missedHits = 0;
        private bool[] lanePressed = new bool[4];

        // Properties for external access
        public bool IsGameActive => isPlaying;
        public bool PlayerWon { get; private set; }

        private void Awake()
        {
            if (musicSource == null)
                musicSource = GetComponent<AudioSource>();

            // Make sure UI is properly set up
            if (resultScreen != null)
                resultScreen.SetActive(false);
        }

        private void Start()
        {
            if (continueButton != null)
                continueButton.onClick.AddListener(EndGame);
        }

        private void Update()
        {
            if (!isPlaying) return;

            // Update time
            currentPlayTime = Time.time - gameStartTime;

            // Update progress bar
            if (progressBar != null && currentSong != null && currentSong.songClip != null)
            {
                progressBar.value = currentPlayTime / currentSong.songClip.length;
            }

            // Process input
            ProcessInput();

            // Move notes
            MoveNotes();

            // Check for missed notes
            CheckMissedNotes();

            // Spawn new notes
            SpawnNotes();

            // Check if the song has ended
            if (currentSong != null && currentSong.songClip != null &&
                currentPlayTime >= currentSong.songClip.length && remainingNotes.Count == 0 && activeNotes.Count == 0)
            {
                ShowResults();
            }
        }

        public void StartGame(int difficulty)
        {
            // Find an appropriate song for the difficulty
            SongData selectedSong = null;
            foreach (var song in availableSongs)
            {
                if (song.difficulty <= difficulty)
                {
                    selectedSong = song;
                    break;
                }
            }

            // If no song found, use the first one
            if (selectedSong == null && availableSongs.Count > 0)
                selectedSong = availableSongs[0];

            if (selectedSong == null)
            {
                Debug.LogError("No songs available for rhythm game!");
                return;
            }

            // Pass the difficulty to the game so we can scale note generation
            StartGame(selectedSong, difficulty);
        }

        public void StartGame(SongData songData, int difficulty = 1)
        {
            currentSong = songData;
            
            // Store original values to reset after game
            float originalPerfectWindow = perfectWindow;
            float originalGoodWindow = goodWindow;
            float originalOkayWindow = okayWindow;
            float originalNoteSpeed = noteSpeed;
            
            // Generate notes procedurally if needed
            if (currentSong.generateNotes && (currentSong.notes == null || currentSong.notes.Count == 0))
            {
                // Make timing windows much tighter at high difficulties
                float difficultyFactor = 1.0f - (Mathf.Min(50, difficulty) / 50f);
                perfectWindow = Mathf.Max(10f, perfectWindow * difficultyFactor); // Can get as low as 10ms
                goodWindow = Mathf.Max(25f, goodWindow * difficultyFactor);
                okayWindow = Mathf.Max(40f, okayWindow * difficultyFactor);
                
                // Increase note speed based on difficulty
                noteSpeed = originalNoteSpeed * (1.0f + (difficulty / 40f));
                
                // Use challenging chart generator for all difficulties now
                GenerateChallengingChart(currentSong, difficulty);
                
                Debug.Log($"Generated {currentSong.notes.Count} notes with difficulty {difficulty}");
            }
            
            remainingNotes = new List<RhythmNote>(currentSong.notes);
            remainingNotes.Sort((a, b) => a.time.CompareTo(b.time));

            // Reset game state
            currentScore = 0;
            currentCombo = 0;
            maxCombo = 0;
            perfectHits = 0;
            goodHits = 0;
            okayHits = 0;
            missedHits = 0;
            isPlaying = true;
            PlayerWon = false;

            // Clear any active notes
            foreach (var note in activeNotes)
            {
                Destroy(note);
            }
            activeNotes.Clear();

            // Set up UI
            UpdateScoreUI();
            if (resultScreen != null)
                resultScreen.SetActive(false);

            // Start the song
            if (musicSource != null && currentSong.songClip != null)
            {
                musicSource.clip = currentSong.songClip;
                musicSource.Play();
            }

            // Record start time
            gameStartTime = Time.time;
        }
        
        // New generation system for high difficulties with proper rhythm game patterns
        private void GenerateChallengingChart(SongData song, int difficulty)
        {
            if (song.songClip == null) {
                Debug.LogError("Cannot generate notes for a song with no audio clip");
                return;
            }
            song.notes = new List<RhythmNote>();

            float songDuration = song.songClip.length;
            float bpm = song.bpm;
            float secondsPerBeat = 60f / bpm;
            float offset = song.offset;

            // Use inspector settings
            int density = noteGenSettings.density; // 1-10
            float randomness = noteGenSettings.randomness; // 0-1
            bool usePatterns = noteGenSettings.usePatterns;
            int chanceOfDouble = noteGenSettings.chanceOfDoubleNote;
            int chanceOfTriple = noteGenSettings.chanceOfTripleNote;
            int chanceOfHold = noteGenSettings.chanceOfHoldNote;
            float holdNoteDuration = noteGenSettings.holdNoteDuration;
            bool useQuarter = noteGenSettings.useQuarterNotes;
            bool useEighth = noteGenSettings.useEighthNotes;
            bool useSixteenth = noteGenSettings.useSixteenthNotes;
            bool useTriplets = noteGenSettings.useTriplets;

            // Determine smallest division
            float smallestDivision = secondsPerBeat;
            if (useSixteenth) smallestDivision = secondsPerBeat / 4f;
            else if (useEighth) smallestDivision = secondsPerBeat / 2f;
            else if (useTriplets) smallestDivision = secondsPerBeat / 3f;
            // If all enabled, use 16th
            if (useSixteenth && useTriplets) smallestDivision = Mathf.Min(secondsPerBeat / 4f, secondsPerBeat / 3f);

            // Build a list of all possible note times
            List<float> noteTimes = new List<float>();
            for (float t = offset; t < songDuration; ) {
                if (useQuarter) noteTimes.Add(t);
                if (useEighth) noteTimes.Add(t + secondsPerBeat / 2f);
                if (useSixteenth) {
                    noteTimes.Add(t + secondsPerBeat / 4f);
                    noteTimes.Add(t + secondsPerBeat / 2f);
                    noteTimes.Add(t + 3f * secondsPerBeat / 4f);
                }
                if (useTriplets) {
                    noteTimes.Add(t + secondsPerBeat / 3f);
                    noteTimes.Add(t + 2f * secondsPerBeat / 3f);
                }
                t += secondsPerBeat;
            }
            // Remove duplicates and sort
            noteTimes = noteTimes.Where(x => x < songDuration).Distinct().OrderBy(x => x).ToList();

            // Track last note time for each lane
            float[] lastNoteTimeInLane = new float[4] { -999f, -999f, -999f, -999f };
            float minLaneInterval = 0.2f; // 200ms

            // Aggressive note placement: at high density, fill almost every slot
            for (int i = 0; i < noteTimes.Count; i++) {
                float t = noteTimes[i];
                // At high density, always place a note; at lower, use randomness
                bool placeNote = density >= 8 || UnityEngine.Random.value < (density / 10f) + (1f - randomness) * 0.2f;
                if (!placeNote) continue;

                // Determine chord size
                int chordSize = 1;
                if (usePatterns) {
                    if (UnityEngine.Random.value < chanceOfTriple / 100f) chordSize = 3;
                    else if (UnityEngine.Random.value < chanceOfDouble / 100f) chordSize = 2;
                    // At max density, allow rare 4-note chords
                    if (density == 10 && UnityEngine.Random.value < 0.1f) chordSize = 4;
                }
                // Pick unique lanes, but only if last note in that lane was at least 200ms ago
                List<int> lanes = new List<int>();
                int attempts = 0;
                while (lanes.Count < chordSize && attempts < 10) {
                    int lane = UnityEngine.Random.Range(0, 4);
                    if (!lanes.Contains(lane) && t - lastNoteTimeInLane[lane] >= minLaneInterval) {
                        lanes.Add(lane);
                        lastNoteTimeInLane[lane] = t;
                    }
                    attempts++;
                }
                foreach (int lane in lanes) {
                    RhythmNote note = new RhythmNote();
                    note.lane = lane;
                    note.time = t;
                    // Hold note?
                    if (usePatterns && UnityEngine.Random.value < chanceOfHold / 100f) note.duration = holdNoteDuration * secondsPerBeat;
                    song.notes.Add(note);
                }
            }
            song.notes.Sort((a, b) => a.time.CompareTo(b.time));
            Debug.Log($"[AggressiveGen] Generated {song.notes.Count} notes for song {song.songName} (density {density})");
        }

        private void ProcessInput()
        {
            for (int lane = 0; lane < 4; lane++)
            {
                bool keyDown = Input.GetKeyDown(laneKeys[lane]);
                bool keyUp = Input.GetKeyUp(laneKeys[lane]);

                if (keyDown)
                {
                    lanePressed[lane] = true;
                    ProcessLanePress(lane);
                }
                else if (keyUp)
                {
                    lanePressed[lane] = false;
                }
            }
        }

        private void ProcessLanePress(int lane)
        {
            GameObject closestNote = null;
            float closestTime = float.MaxValue;
            int closestNoteIndex = -1;

            // Find the closest note in this lane
            for (int i = 0; i < activeNotes.Count; i++)
            {
                NoteController noteController = activeNotes[i].GetComponent<NoteController>();
                if (noteController.Lane == lane)
                {
                    float noteTime = noteController.TargetTime;
                    float timeDifference = Mathf.Abs(currentPlayTime - noteTime);

                    if (timeDifference < closestTime)
                    {
                        closestTime = timeDifference;
                        closestNote = activeNotes[i];
                        closestNoteIndex = i;
                    }
                }
            }

            // If we found a note and it's within the hit window (convert milliseconds to seconds)
            if (closestNote != null && closestTime <= okayWindow/1000f)
            {
                // Determine hit accuracy (convert milliseconds to seconds for comparison)
                string hitResult = "Miss";
                int scoreValue = 0;

                if (closestTime <= perfectWindow/1000f)
                {
                    hitResult = "Perfect";
                    scoreValue = 300;
                    perfectHits++;
                }
                else if (closestTime <= goodWindow/1000f)
                {
                    hitResult = "Good";
                    scoreValue = 200;
                    goodHits++;
                }
                else if (closestTime <= okayWindow/1000f)
                {
                    hitResult = "Okay";
                    scoreValue = 100;
                    okayHits++;
                }

                // Update score
                currentScore += scoreValue;
                currentCombo++;
                maxCombo = Mathf.Max(maxCombo, currentCombo);

                // Visual feedback
                ShowHitFeedback(lane, hitResult);
                
                // Remove the note
                activeNotes.RemoveAt(closestNoteIndex);
                Destroy(closestNote);

                // Update UI
                UpdateScoreUI();
            }
        }

        private void ShowHitFeedback(int lane, string hitResult)
        {
            // Create a temporary text for hit feedback
            // In a real implementation, you'd use a pooling system
            GameObject feedbackObj = new GameObject("HitFeedback");
            feedbackObj.transform.SetParent(transform);
            
            RectTransform rectTransform = feedbackObj.AddComponent<RectTransform>();
            rectTransform.position = laneTargets[lane].position;
            
            TextMeshProUGUI feedbackText = feedbackObj.AddComponent<TextMeshProUGUI>();
            feedbackText.text = hitResult;
            feedbackText.alignment = TextAlignmentOptions.Center;
            feedbackText.color = GetColorForHitResult(hitResult);
            feedbackText.font = TMP_Settings.defaultFontAsset;
            
            // Animate and destroy
            StartCoroutine(AnimateFeedback(feedbackObj));
        }

        private Color GetColorForHitResult(string hitResult)
        {
            switch (hitResult)
            {
                case "Perfect":
                    return Color.yellow;
                case "Good":
                    return Color.green;
                case "Okay":
                    return Color.blue;
                case "Miss":
                    return Color.red;
                default:
                    return Color.white;
            }
        }

        private IEnumerator AnimateFeedback(GameObject feedbackObj)
        {
            float duration = 0.5f;
            float startTime = Time.time;
            TextMeshProUGUI text = feedbackObj.GetComponent<TextMeshProUGUI>();
            
            while (Time.time - startTime < duration)
            {
                float progress = (Time.time - startTime) / duration;
                text.color = new Color(text.color.r, text.color.g, text.color.b, 1 - progress);
                
                // Move upward slightly
                feedbackObj.transform.position += Vector3.up * Time.deltaTime * 50f;
                
                yield return null;
            }
            
            Destroy(feedbackObj);
        }

        private void MoveNotes()
        {
            for (int i = 0; i < activeNotes.Count; i++)
            {
                NoteController noteController = activeNotes[i].GetComponent<NoteController>();
                if (noteController != null)
                {
                    // Calculate position based on time until hit
                    float timeUntilHit = noteController.TargetTime - currentPlayTime;
                    float distanceToTarget = timeUntilHit * noteSpeed;
                    
                    // Move the note
                    RectTransform noteRect = activeNotes[i].GetComponent<RectTransform>();
                    Vector3 targetPos = laneTargets[noteController.Lane].position;
                    Vector3 spawnPos = laneSpawnPoints[noteController.Lane].position;
                    
                    // Interpolate position
                    float t = 1 - (distanceToTarget / Vector3.Distance(spawnPos, targetPos));
                    noteRect.position = Vector3.Lerp(spawnPos, targetPos, t);
                }
            }
        }

        private void CheckMissedNotes()
        {
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                NoteController noteController = activeNotes[i].GetComponent<NoteController>();
                if (noteController != null)
                {
                    float timeDifference = currentPlayTime - noteController.TargetTime;
                    
                    // If the note is past the hit window, count as a miss (convert milliseconds to seconds)
                    if (timeDifference > okayWindow/1000f)
                    {
                        // Count as missed
                        missedHits++;
                        currentCombo = 0;
                        
                        // Show miss feedback
                        ShowHitFeedback(noteController.Lane, "Miss");
                        
                        // Remove the note
                        Destroy(activeNotes[i]);
                        activeNotes.RemoveAt(i);
                        
                        // Update UI
                        UpdateScoreUI();
                    }
                }
            }
        }

        private void SpawnNotes()
        {
            // Calculate how far ahead to spawn notes (based on note speed)
            float spawnTimeWindow = 2.0f; // How many seconds ahead to spawn notes
            
            for (int i = remainingNotes.Count - 1; i >= 0; i--)
            {
                RhythmNote note = remainingNotes[i];
                float timeUntilHit = note.time - currentPlayTime;
                
                // If it's time to spawn this note
                if (timeUntilHit <= spawnTimeWindow && timeUntilHit > 0)
                {
                    SpawnNote(note);
                    remainingNotes.RemoveAt(i);
                }
            }
        }

        private void SpawnNote(RhythmNote note)
        {
            if (notePrefab == null || note.lane < 0 || note.lane >= 4) return;
            
            // Instantiate the note
            GameObject noteObj = Instantiate(notePrefab, laneSpawnPoints[note.lane].position, Quaternion.identity);
            noteObj.transform.SetParent(transform);
            
            // Set up the note controller
            NoteController noteController = noteObj.AddComponent<NoteController>();
            noteController.Lane = note.lane;
            noteController.TargetTime = note.time;
            noteController.Duration = note.duration;
            
            // Style the note based on lane
            Image noteImage = noteObj.GetComponent<Image>();
            if (noteImage != null)
            {
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
            
            // Add to active notes
            activeNotes.Add(noteObj);
        }

        private void UpdateScoreUI()
        {
            if (scoreText != null)
                scoreText.text = $"Score: {currentScore}";
                
            if (comboText != null)
                comboText.text = $"{currentCombo}";
                
            if (accuracyText != null)
            {
                int totalNotes = perfectHits + goodHits + okayHits + missedHits;
                float accuracy = totalNotes > 0 ? 
                    (perfectHits * 1f + goodHits * 0.66f + okayHits * 0.33f) / totalNotes * 100f : 100f;
                accuracyText.text = $"Accuracy: {accuracy:0.00}%";
            }
            
            if (gradeText != null)
                gradeText.text = CalculateGrade();
        }

        private string CalculateGrade()
        {
            int totalNotes = perfectHits + goodHits + okayHits + missedHits;
            if (totalNotes == 0) return "SSS";
            
            float accuracy = (perfectHits * 1f + goodHits * 0.66f + okayHits * 0.33f) / totalNotes * 100f;
            
            if (accuracy >= 95f && missedHits == 0) return "SSS";
            if (accuracy >= 90f) return "SS";
            if (accuracy >= 80f) return "S";
            if (accuracy >= 70f) return "A";
            if (accuracy >= 60f) return "B";
            if (accuracy >= 50f) return "C";
            return "D";
        }

        private void ShowResults()
        {
            isPlaying = false;
            
            // Stop the music
            if (musicSource != null)
                musicSource.Stop();
            
            // Calculate if player won
            string grade = CalculateGrade();
            PlayerWon = grade != "D"; // Consider D grade a failure
            
            // Set up result screen
            if (resultScreen != null)
            {
                resultScreen.SetActive(true);
                
                if (resultScoreText != null)
                    resultScoreText.text = $"Final Score: {currentScore}";
                    
                if (resultComboText != null)
                    resultComboText.text = $"Max Combo: {maxCombo}x";
                    
                if (resultGradeText != null)
                    resultGradeText.text = $"Grade: {grade}";
            }
        }

        private void EndGame()
        {
            if (resultScreen != null)
                resultScreen.SetActive(false);
        }
    }

    
} 