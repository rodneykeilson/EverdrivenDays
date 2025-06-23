using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EverdrivenDays
{
    // This is a direct copy of SmallEnemyRhythmController for boss encounters.
    public class BossRhythmController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private GameObject notePrefab;
        [SerializeField] private RectTransform[] laneTargets;
        [SerializeField] private RectTransform[] laneSpawnPoints;
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
        [SerializeField] private float perfectWindow = 30f;
        [SerializeField] private float goodWindow = 60f;
        [SerializeField] private float okayWindow = 90f;
        [SerializeField] private float encounterEffectDuration = 1f;
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
        private float gameDuration;

        // Properties for combat results
        public bool PlayerWon { get; private set; }
        public int FinalScore => currentScore;
        public int MaxCombo => maxCombo;
        public bool IsFullCombo => missedHits == 0;
        public bool IsAllPerfect => perfectHits > 0 && goodHits == 0 && okayHits == 0 && missedHits == 0;

        private List<Coroutine> noteSpawnCoroutines = new List<Coroutine>();

        private void Awake()
        {
            if (rhythmGameUI != null)
                rhythmGameUI.SetActive(false);
            if (encounterEffectUI != null)
                encounterEffectUI.SetActive(false);
        }

        public void StartBossEncounter(Player playerRef)
        {
            player = playerRef;
            StartCoroutine(BossEncounterSequence());
        }

        private System.Collections.IEnumerator BossEncounterSequence()
        {
            // Play encounter SFX and show text
            if (encounterSFX != null)
                AudioSource.PlayClipAtPoint(encounterSFX, transform.position);

            if (encounterEffectUI != null) encounterEffectUI.SetActive(true);
            if (encounterText != null && currentEnemy != null)
                encounterText.text = $"Boss Encounter! {currentEnemy.name}";

            yield return new WaitForSeconds(encounterEffectDuration);

            if (encounterEffectUI != null) encounterEffectUI.SetActive(false);

            // Show rhythm game UI (fix for missing UI activation)
            if (rhythmGameUI != null)
                rhythmGameUI.SetActive(true);

            // Start rhythm game
            isPlaying = true;
            StartCoroutine(RunBossRhythmGame());
        }

        private System.Collections.IEnumerator RunBossRhythmGame()
        {
            // Pick a song (first in list, or random if you want)
            SongData bossSong = (availableSongs != null && availableSongs.Count > 0) ? availableSongs[0] : null;
            if (bossSong != null && player != null && bossSong.songClip != null)
            {
                musicSource.clip = bossSong.songClip;
                musicSource.Play();
                float songLength = bossSong.songClip.length;
                float timer = 0f;
                PlayerWon = true;
                while (timer < songLength && isPlaying)
                {
                    // Here you would check for player failure (misses, health, etc.)
                    // If player fails, set PlayerWon = false; isPlaying = false; break;
                    timer += Time.deltaTime;
                    yield return null;
                }
                EndBossBattle(PlayerWon);
            }
            else
            {
                // Fallback: just end after 10 seconds
                yield return new WaitForSeconds(10f);
                EndBossBattle(true);
            }
        }

        // --- Boss Rhythm EndGame logic to match SmallEnemyRhythmController ---
        private void EndGame(bool playerWon)
        {
            Debug.Log("Boss rhythm game ended");
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
                        GrantGoldReward();
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
            Debug.Log($"Damage - To Boss: {damageToEnemy}, To Player: {damageToPlayer}");
            if (player != null && player.Input != null)
            {
                player.Input.EnableMovement();
            }
            if (currentEnemy != null)
            {
                currentEnemy.OnCombatEnd(PlayerWon);
            }
        }

        private int CalculateDamageToEnemy()
        {
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
            int damage = Mathf.RoundToInt(baseDamage * (1f + scoreMultiplier) * comboMultiplier * critMultiplier * 25f);
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
        private void GrantGoldReward()
        {
            if (currentEnemy == null || player == null || player.Stats == null) return;
            int baseGold = 10;
            if (currentEnemy.GetType().GetProperty("GoldReward") != null)
            {
                baseGold = (int)currentEnemy.GetType().GetProperty("GoldReward").GetValue(currentEnemy);
            }
            int totalNotes = perfectHits + goodHits + okayHits + missedHits;
            float accuracy = totalNotes > 0 ? (float)(perfectHits * 100 + goodHits * 75 + okayHits * 50) / (totalNotes * 100) : 0f;
            float perfMultiplier = Mathf.Lerp(0.5f, 2f, accuracy / 100f);
            int goldReward = Mathf.RoundToInt(baseGold * perfMultiplier);
            player.Stats.AddGold(goldReward);
            Debug.Log($"Granted {goldReward} gold (base {baseGold}, accuracy {accuracy:F1}%)");
        }

        // Replace EndBossBattle with EndGame logic
        public void EndBossBattle(bool survived)
        {
            EndGame(survived);
        }

        // --- BOSS RHYTHM CONTROLLER REWRITE TO MATCH SMALL ENEMY ---
        // This is now a full copy of SmallEnemyRhythmController, adapted for bosses.
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
            MoveNotes();
            CheckMissedNotes();
        }

        public void StartGame(Enemy enemy, Player playerRef, AudioClip songClip = null)
        {
            Debug.Log($"Starting boss rhythm game with enemy: {enemy.name}");
            currentEnemy = enemy;
            player = playerRef;
            currentScore = 0;
            currentCombo = 0;
            maxCombo = 0;
            perfectHits = 0;
            goodHits = 0;
            okayHits = 0;
            missedHits = 0;
            foreach (var note in activeNotes)
            {
                if (note != null)
                    Destroy(note);
            }
            activeNotes.Clear();
            SelectSong(songClip);
            if (player != null && player.Input != null)
                player.Input.DisableMovement();
            StartCoroutine(ShowEncounterEffect());
        }

        private IEnumerator ShowEncounterEffect()
        {
            Debug.Log("Showing boss encounter effect");
            if (rhythmGameUI != null)
                rhythmGameUI.SetActive(false);
            if (encounterEffectUI != null)
            {
                encounterEffectUI.SetActive(true);
                if (encounterText != null)
                {
                    encounterText.text = $"Boss Encounter! {currentEnemy.name}";
                    // Optionally animate text
                }
                if (encounterSFX != null)
                {
                    GameObject tempAudio = new GameObject("TempBossEncounterAudio");
                    AudioSource audioSource = tempAudio.AddComponent<AudioSource>();
                    audioSource.clip = encounterSFX;
                    audioSource.volume = 1.0f;
                    audioSource.spatialBlend = 0f;
                    audioSource.Play();
                    Destroy(tempAudio, 1.0f);
                }
                if (CombatEffectsManager.Instance != null)
                {
                    CombatEffectsManager.Instance.PlayEncounterEffect(currentEnemy.transform.position);
                }
            }
            yield return new WaitForSeconds(encounterEffectDuration);
            if (encounterEffectUI != null)
                encounterEffectUI.SetActive(false);
            StartActualGame();
        }

        private void StartActualGame()
        {
            Debug.Log("Starting boss rhythm game...");
            if (currentSong != null && currentSong.songClip != null) {
                gameDuration = (currentSong.customDuration > 0f) ? currentSong.customDuration : currentSong.songClip.length;
            } else {
                gameDuration = 15f;
            }
            if (rhythmGameUI != null)
                rhythmGameUI.SetActive(true);
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
            isPlaying = true;
            gameStartTime = Time.time;
            StartCoroutine(GameTimer());
            GenerateNotes();
            if (musicSource != null && currentSong != null && currentSong.songClip != null)
            {
                musicSource.clip = currentSong.songClip;
                musicSource.Play();
                Debug.Log("Started boss music");
            }
            else
            {
                Debug.LogWarning("No music source or song clip available for boss!");
            }
        }

        private IEnumerator GameTimer()
        {
            while (isPlaying)
            {
                if (progressBar != null && gameDuration > 0)
                {
                    progressBar.value = Mathf.Clamp01((Time.time - gameStartTime) / gameDuration);
                }
                songPosition = Time.time - gameStartTime;
                if (songPosition >= gameDuration)
                {
                    if (musicSource != null && musicSource.isPlaying)
                        musicSource.Stop();
                    EndBossBattle(true);
                    yield break;
                }
                yield return null;
            }
        }

        private void GenerateNotes()
        {
            Debug.Log("Generating boss notes...");
            if (currentSong == null) return;
            foreach (var coroutine in noteSpawnCoroutines)
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
            }
            noteSpawnCoroutines.Clear();
            foreach (var noteObj in activeNotes)
            {
                if (noteObj != null)
                    Destroy(noteObj);
            }
            activeNotes.Clear();
            if (currentSong != null && currentSong.songClip != null) {
                gameDuration = (currentSong.customDuration > 0f) ? currentSong.customDuration : currentSong.songClip.length;
            } else {
                gameDuration = 15f;
            }
            GeneratePatternedNotes();
        }

        private void GeneratePatternedNotes()
        {
            if (currentSong == null || currentSong.bpm <= 0) return;
            float bpm = currentSong.bpm;
            float beatDuration = 60f / bpm;
            float songOffset = currentSong.offset + 1.5f;
            float songLength = gameDuration;
            int difficulty = Mathf.Clamp(noteGenSettings.density, 1, 64);
            List<float> rhythmSteps = new List<float>();
            if (noteGenSettings.useQuarterNotes) rhythmSteps.Add(1f);
            if (noteGenSettings.useEighthNotes) rhythmSteps.Add(0.5f);
            if (noteGenSettings.useSixteenthNotes) rhythmSteps.Add(0.25f);
            if (noteGenSettings.useTriplets) rhythmSteps.Add(1f/3f);
            if (rhythmSteps.Count == 0) rhythmSteps.Add(1f);
            float smallestStep = 1f;
            foreach (var step in rhythmSteps) if (step < smallestStep) smallestStep = step;
            float gridStep = smallestStep * beatDuration;
            int numGridSlots = Mathf.FloorToInt(songLength / gridStep);
            float fillRate = Mathf.Lerp(0.3f, 0.95f, Mathf.Clamp01((difficulty - 1f) / 31f));
            List<RhythmNote> notes = new List<RhythmNote>();
            System.Random rng = new System.Random();
            int lastLane = -1;
            float burstChance = Mathf.Lerp(0.05f, 0.25f, (difficulty-1f)/31f);
            float jackChance = Mathf.Lerp(0.02f, 0.15f, (difficulty-1f)/31f);
            float chordChance = Mathf.Lerp(0.10f, 0.25f, (difficulty-1f)/31f);
            float minInterval = 0.12f - 0.06f * ((difficulty-1f)/31f);
            for (int i = 0; i < numGridSlots; i++)
            {
                float t = songOffset + i * gridStep;
                if (t > songLength + songOffset) break;
                if (rng.NextDouble() > fillRate) continue;
                int lane = rng.Next(4);
                double patternRoll = rng.NextDouble();
                if (lastLane == lane && rng.NextDouble() < jackChance) {
                    RhythmNote jackNote = new RhythmNote {
                        lane = lane,
                        time = t,
                        duration = 0f
                    };
                    notes.Add(jackNote);
                } else if (patternRoll < burstChance) {
                    int burstLen = rng.Next(3, 6);
                    float burstStep = minInterval * Mathf.Lerp(1f, 0.6f, (difficulty-1f)/31f);
                    for (int b = 0; b < burstLen && t + b * burstStep < songLength + songOffset; b++) {
                        int burstLane = rng.Next(4);
                        RhythmNote burstNote = new RhythmNote {
                            lane = burstLane,
                            time = t + b * burstStep,
                            duration = 0f
                        };
                        notes.Add(burstNote);
                    }
                } else if (patternRoll < burstChance + chordChance) {
                    int chordSize = rng.Next(2, 4);
                    HashSet<int> chordLanes = new HashSet<int>();
                    while (chordLanes.Count < chordSize) chordLanes.Add(rng.Next(4));
                    foreach (int chordLane in chordLanes) {
                        RhythmNote chordNote = new RhythmNote {
                            lane = chordLane,
                            time = t,
                            duration = 0f
                        };
                        notes.Add(chordNote);
                    }
                } else {
                    RhythmNote note = new RhythmNote {
                        lane = lane,
                        time = t,
                        duration = 0f
                    };
                    notes.Add(note);
                }
                lastLane = lane;
                if (noteGenSettings.chanceOfHoldNote > 0 && rng.Next(100) < noteGenSettings.chanceOfHoldNote) {
                    notes[notes.Count-1].duration = noteGenSettings.holdNoteDuration * beatDuration;
                }
            }
            notes.Sort((a, b) => a.time.CompareTo(b.time));
            float minSpacing = 0.2f;
            float[] lastNoteTimePerLane = new float[4];
            for (int i = 0; i < 4; i++) lastNoteTimePerLane[i] = float.NegativeInfinity;
            for (int i = 0; i < notes.Count; i++)
            {
                RhythmNote note = notes[i];
                float lastTime = lastNoteTimePerLane[note.lane];
                if (note.time - lastTime < minSpacing)
                {
                    continue;
                }
                lastNoteTimePerLane[note.lane] = note.time;
                noteSpawnCoroutines.Add(StartCoroutine(SpawnNoteAtTime(note)));
            }
        }

        private IEnumerator SpawnNoteAtTime(RhythmNote note)
        {
            float distance = Vector3.Distance(laneSpawnPoints[note.lane].position, laneTargets[note.lane].position);
            float travelTime = distance / noteSpeed;
            float timeUntilSpawn = note.time - travelTime - songPosition;
            if (timeUntilSpawn > 0)
                yield return new WaitForSeconds(timeUntilSpawn);
            if (!isPlaying) yield break;
            SpawnNote(note);
        }

        private void SpawnNote(RhythmNote note)
        {
            if (!isPlaying || notePrefab == null || note.lane < 0 || note.lane >= 4) return;
            if (rhythmGameUI == null || !rhythmGameUI.activeInHierarchy) return;
            if (laneSpawnPoints == null || laneSpawnPoints.Length <= note.lane || laneSpawnPoints[note.lane] == null) return;
            GameObject noteObj = Instantiate(notePrefab);
            noteObj.transform.SetParent(rhythmGameUI.transform, false);
            noteObj.SetActive(true);
            RectTransform noteRect = noteObj.GetComponent<RectTransform>();
            if (noteRect != null)
            {
                noteRect.anchorMin = new Vector2(0.5f, 0.5f);
                noteRect.anchorMax = new Vector2(0.5f, 0.5f);
                noteRect.pivot = new Vector2(0.5f, 0.5f);
                noteRect.position = laneSpawnPoints[note.lane].position;
            }
            NoteController noteController = noteObj.GetComponent<NoteController>();
            if (noteController != null)
            {
                noteController.Lane = note.lane;
                noteController.TargetTime = note.time;
                noteController.Duration = note.duration;
            }
            activeNotes.Add(noteObj);
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
                    currentScore += 100;
                    currentCombo++;
                    perfectHits++;
                    ShowHitFeedback(lane, "PERFECT", Color.magenta);
                    Debug.Log("PERFECT hit!");
                }
                else if (timeDiffMs <= goodWindow)
                {
                    currentScore += 75;
                    currentCombo++;
                    goodHits++;
                    ShowHitFeedback(lane, "GOOD", Color.green);
                    Debug.Log("GOOD hit!");
                }
                else if (timeDiffMs <= okayWindow)
                {
                    currentScore += 50;
                    currentCombo++;
                    okayHits++;
                    ShowHitFeedback(lane, "OKAY", Color.yellow);
                    Debug.Log("OKAY hit!");
                }
                else
                {
                    currentScore += 5;
                    ShowHitFeedback(lane, "BAD", Color.red);
                    missedHits++;
                    currentCombo = 0;
                    if (player != null && player.Stats != null)
                        player.Stats.TakeDamage(10);
                    Debug.Log("BAD - too far from hit window!");
                }
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

        private void MoveNotes()
        {
            foreach (var noteObj in activeNotes)
            {
                if (noteObj == null) continue;
                NoteController nc = noteObj.GetComponent<NoteController>();
                if (nc == null) continue;
                int lane = nc.Lane;
                float noteTime = nc.TargetTime;
                Vector3 startPos = laneSpawnPoints[lane].position;
                Vector3 endPos = laneTargets[lane].position;
                float distance = Vector3.Distance(startPos, endPos);
                float travelTime = distance / noteSpeed;
                float timeSinceSpawn = songPosition - (noteTime - travelTime);
                float progress = Mathf.Clamp01(timeSinceSpawn / travelTime);
                noteObj.transform.position = Vector3.Lerp(startPos, endPos, progress);
            }
        }

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

        private void SelectSong(AudioClip specificSongClip = null)
        {
            Debug.Log("Selecting song for boss rhythm game...");
            currentSong = null;
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
        }

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
            float duration = 0.5f;
            float elapsed = 0f;
            Vector3 startPos = feedbackObj.transform.position;
            Vector3 endPos = startPos + Vector3.up * 50f;
            TMPro.TextMeshProUGUI textComp = feedbackObj.GetComponent<TMPro.TextMeshProUGUI>();
            Color startColor = textComp.color;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                feedbackObj.transform.position = Vector3.Lerp(startPos, endPos, t);
                textComp.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            Destroy(feedbackObj);
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
    }
}
