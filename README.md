# Cerebrum - AI-Powered Trivia Game

A Jeopardy-style trivia game built with Unity, featuring AI-powered answer judging and text-to-speech capabilities.

## Features

- **3-Player Trivia Gameplay**: Classic Jeopardy-style board with 6 categories and 5 clue values
- **Voice Recognition**: Players speak their answers using microphone input
- **AI Answer Judging**: OpenAI GPT evaluates player answers for correctness
- **Text-to-Speech**: Clues are read aloud using OpenAI TTS
- **Dynamic Categories**: Loads from preprocessed Jeopardy clue data
- **Animated UI**: Flying clue cards, category reveals, and smooth transitions

## Requirements

- **Unity**: 2022.3 LTS or newer (tested with Unity 6)
- **Platform**: macOS, Windows, or Linux
- **OpenAI API Key**: Required for TTS and answer judging
- **Microphone**: Required for voice input

## Setup Instructions

### 1. Clone the Repository

```bash
git clone https://github.com/cameronhickey/SynapseGame.git
cd SynapseGame
```

### 2. Open in Unity

1. Open Unity Hub
2. Click "Add" and select the cloned project folder
3. Open the project (Unity will regenerate the Library folder automatically)

### 3. Configure OpenAI API Key

The game requires an OpenAI API key for:
- **Text-to-Speech (TTS)**: Reading clues aloud using OpenAI's voice synthesis
- **Answer Judging**: GPT-4o-mini evaluates if player responses are correct
- **Speech-to-Text (STT)**: Whisper transcribes player voice answers

**Setup:**

1. Copy the template file to create your local config:
   ```bash
   cp openai_config.json.template openai_config.json
   ```

2. Edit `openai_config.json` in the project root and add your API key:
   ```json
   {
       "apiKey": "sk-your-actual-api-key-here",
       "ttsModel": "tts-1",
       "ttsVoice": "nova",
       "ttsSpeed": 1.0,
       "sttModel": "whisper-1",
       "judgeModel": "gpt-4o-mini",
       "baseUrl": "https://api.openai.com/v1"
   }
   ```

3. **Important**: The `openai_config.json` file is gitignored - your API key will never be committed!

**Getting an OpenAI API Key:**
1. Go to [platform.openai.com](https://platform.openai.com)
2. Sign up or log in
3. Navigate to API Keys section
4. Create a new secret key
5. Copy and paste into your `openai_config.json`

**For Built Apps:**
When building for distribution, copy your config to StreamingAssets:
```bash
cp openai_config.json Assets/StreamingAssets/openai_config.json
```

### 4. Download Clue Data

The game uses preprocessed Jeopardy clue data. This data is not included in the repository due to size.

**Option A: Preprocess from TSV files**

1. Obtain Jeopardy clue TSV files (not included)
2. Place them in `Assets/Clues/` folder
3. In Unity, go to menu: `Cerebrum > Preprocess Categories`
4. This generates optimized category files in `Assets/_Project/Data/Categories/`

**Option B: Use existing preprocessed data**

If you have access to preprocessed category files:
1. Create folder `Assets/_Project/Data/Categories/`
2. Copy the `.txt` category files into this folder
3. Copy `category_index.txt` to `Assets/_Project/Data/`

### 5. Install Required Packages

The project uses these Unity packages (should auto-install):
- TextMeshPro
- Input System (New)
- Unity UI

If TextMeshPro essentials are missing:
1. Go to `Window > TextMeshPro > Import TMP Essential Resources`

### 6. Font Setup

The game uses custom fonts:
- **Bebas Neue**: For category headers
- **Lora**: For clue text

These should be included in `Assets/_Project/Resources/Fonts/`. If missing:
1. Download fonts from Google Fonts
2. Import into Unity
3. Create TextMeshPro SDF font assets:
   - `Window > TextMeshPro > Font Asset Creator`
   - Generate SDF assets and save to `Assets/_Project/Resources/Fonts/`

## Project Structure

```
Cerebrum2d/
├── openai_config.json           # Your local API config (gitignored)
├── openai_config.json.template  # Template for API config
├── Assets/
│   ├── _Project/
│   │   ├── Data/
│   │   │   ├── Categories/      # Preprocessed clue files (~27k categories)
│   │   │   └── category_index.txt
│   │   ├── Resources/
│   │   │   ├── Fonts/           # TMP font assets
│   │   │   ├── Images/          # UI images and backgrounds
│   │   │   ├── Audio/           # Pre-generated phrase audio
│   │   │   └── OpenAIConfig.asset # Default config (uses JSON override)
│   │   ├── Scenes/
│   │   │   └── MainScene.unity  # Main game scene
│   │   └── Scripts/
│   │       ├── Audio/           # Audio playback and caching
│   │       ├── Core/            # Game bootstrapper
│   │       ├── Data/            # Data models (Board, Category, Clue)
│   │       ├── Editor/          # Unity editor tools
│   │       ├── Game/            # Game logic controllers
│   │       ├── OpenAI/          # OpenAI API integration
│   │       └── UI/              # UI components and animations
│   ├── StreamingAssets/
│   │   ├── Categories/          # Categories bundled for builds
│   │   └── category_index.txt   # Index for built apps
│   ├── Clues/                   # Raw TSV files (gitignored)
│   └── Packages/                # Unity package manifest
```

## How to Play

1. **Start Screen**: Enter player names (Tab to navigate between fields)
2. **Category Reveal**: Watch as categories are revealed with animations
3. **Select Clue**: Click any clue value on the board
4. **Listen**: The clue is read aloud via TTS
5. **Buzz In**: Press your assigned key (Q, G, or M by default) to buzz
6. **Answer**: Speak your answer into the microphone
7. **Scoring**: AI judges your answer - correct answers add points, wrong answers deduct

### Player Controls

| Player | Buzz Key |
|--------|----------|
| Player 1 | Q |
| Player 2 | G |
| Player 3 | M |

## Configuration

### Timing Settings

Edit `AnswerFlowController` in the inspector:
- `Buzz Time Seconds`: Time allowed to buzz in (default: 5s)
- `Answer Time Seconds`: Time allowed to answer (default: 5s)
- `Early Buzz Lockout Ms`: Penalty for buzzing too early (default: 750ms)

### Audio Settings

Edit `openai_config.json` in the project root:
- `ttsVoice`: OpenAI voice to use (alloy, echo, fable, onyx, nova, shimmer)
- `ttsModel`: TTS model (tts-1 for speed, tts-1-hd for quality)
- `ttsSpeed`: Speech rate (0.25 to 4.0, default 1.0)

## Architecture

### Audio Pipeline

The game uses a multi-tier audio system for optimal performance:

1. **Pre-cached Phrases**: Common game phrases ("Correct!", "Time's up!", player name announcements) are pre-generated and bundled
2. **Runtime TTS Cache**: Category names and clue text are generated via OpenAI TTS during game load and cached
3. **Unified Loader**: `UnifiedTTSLoader` manages all TTS requests with automatic caching

### Answer Flow

1. Clue is displayed and read aloud
2. Buzz window opens (players race to buzz in)
3. First player to buzz gets the answer window
4. Voice is recorded and sent to Whisper for transcription
5. GPT-4o-mini judges if the answer is correct
6. Score is updated and play continues

### Native macOS Speech Recognition

For built macOS apps, the game uses native macOS speech recognition (via `NSSpeechRecognizer`) instead of OpenAI Whisper for faster response times. This requires microphone and speech recognition permissions.

## Development

### Building for macOS

1. `File > Build Settings`
2. Select macOS as target platform
3. Ensure `Microphone Usage Description` is set in Player Settings
4. Copy your API config for the build:
   ```bash
   cp openai_config.json Assets/StreamingAssets/openai_config.json
   ```
5. Click "Build" or "Build and Run"

**Note**: Categories are automatically copied to StreamingAssets during build via `CategoryDataCopier`.

### Building for Other Platforms

1. `File > Build Settings`
2. Select target platform
3. Click "Build"

### Adding New Categories

Categories are loaded from text files with format:
```
CATEGORY NAME
200|answer|question
400|answer|question
600|answer|question
800|answer|question
1000|answer|question
```

### Debugging

- Check Unity Console for `[AnswerFlow]`, `[BoardController]`, etc. log prefixes
- Enable verbose logging in individual scripts as needed

## Troubleshooting

### "No categories available"
- Ensure category files exist in `Assets/_Project/Data/Categories/`
- Run `Cerebrum > Preprocess Categories` if using raw TSV files

### TTS not working
- Verify OpenAI API key is configured
- Check Console for API errors
- Ensure internet connection

### Microphone not recording
- Grant microphone permissions to Unity/build
- Check that a microphone device is connected

### Fonts not displaying correctly
- Import TMP Essential Resources
- Regenerate SDF font assets

## License

This project is for educational purposes. Jeopardy is a trademark of Sony Pictures Entertainment.

## Credits

- Built with Unity
- AI powered by OpenAI (GPT-4, Whisper, TTS)
- Fonts: Bebas Neue, Lora (Google Fonts)
