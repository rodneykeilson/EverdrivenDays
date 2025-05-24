# Procedural Rhythm Game System - Everdriven Days

This system creates a 4-lane rhythm game with automatic note generation based on BPM. It can automatically detect BPM from audio files and generate notes that follow rhythm patterns.

## Setup Instructions

1. Create a new scene or use an existing one
2. Add a Canvas for the UI elements
3. Create a new empty GameObject and add the `RhythmGameController` component
4. Create another GameObject and add the `SongLoader` component
5. Assign the RhythmGameController reference in the SongLoader component
6. Configure the UI elements as shown below

## Required UI Elements

Set up your UI hierarchy like this:

```
Canvas
├── RhythmGamePanel
│   ├── LanesContainer
│   │   ├── Lane1
│   │   │   ├── Target
│   │   │   └── SpawnPoint
│   │   ├── Lane2
│   │   │   ├── Target
│   │   │   └── SpawnPoint
│   │   ├── Lane3
│   │   │   ├── Target
│   │   │   └── SpawnPoint
│   │   └── Lane4
│   │       ├── Target
│   │       └── SpawnPoint
│   ├── ProgressBar
│   ├── ScoreText
│   ├── ComboText
│   ├── AccuracyText
│   └── GradeText
└── ResultScreen
    ├── ResultScoreText
    ├── ResultComboText
    ├── ResultGradeText
    └── ContinueButton
```

## Adding Songs

### Method 1: Using the Resources Folder
1. Create a `Resources` folder in your Assets directory
2. Create a `Songs` subfolder within Resources
3. Add your audio files to the Songs folder
4. Use the SongLoader editor to load songs from Resources

### Method 2: Using External Files
1. Use the SongLoader editor's Browse button to select an external audio file
2. Click "Load from File" to load the song directly

### Method 3: Importing to Resources
1. Use the "Import Audio File to Resources" button in the SongLoader editor
2. Select your audio file
3. The file will be copied to the Resources/Songs folder

## Customizing Note Generation

You can customize note generation using the NoteGenerationSettings in the RhythmGameController:

- **Density**: Controls how many notes to generate (1-10 scale)
- **Randomness**: How much random variation to add to note timings
- **Pattern Settings**: Control the chances of double/triple notes and hold notes
- **Rhythm Settings**: Choose which beat divisions to use (quarter notes, eighth notes, etc.)

## BPM Detection

The system includes automatic BPM detection. It uses a simple energy-based algorithm to estimate BPM from audio files.

- Enable/disable BPM detection with the `automaticBpmDetection` toggle
- Set a fallback BPM in case detection fails
- Set min/max BPM values to keep detection within reasonable ranges

## Key Bindings

Default key bindings for the 4 lanes:
- Lane 1: D
- Lane 2: F
- Lane 3: J
- Lane 4: K

These can be changed in the RhythmGameController inspector.

## Editor Integration

A custom editor for SongLoader provides:
- One-click loading of songs from Resources or external files
- Importing audio files to the Resources folder
- Quick start option for testing

## Scoring System

- Perfect hit: 300 points
- Good hit: 200 points
- Okay hit: 100 points
- Miss: 0 points

Grades:
- SSS: 95%+ accuracy and no misses
- SS: 90%+ accuracy
- S: 80%+ accuracy
- A: 70%+ accuracy
- B: 60%+ accuracy
- C: 50%+ accuracy
- D: Below 50% accuracy

## Integration with Combat System

To integrate with your combat system:
- Add the RhythmGameController to your combat scene
- Use `rhythmGameController.StartGame(difficulty)` to start a game
- Check `rhythmGameController.PlayerWon` after the game ends
- Use the result to determine combat outcome

## Troubleshooting

- **No audio plays**: Make sure your audio file is set up correctly and the AudioSource component is present
- **BPM detection fails**: Set a fallback BPM and check the console for error messages
- **Notes are not aligned with beats**: Adjust the offset value in the SongData
- **Performance issues**: Reduce the note density or the number of notes generated 