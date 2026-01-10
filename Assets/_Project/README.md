# Cerebrum - board.fun Jeopardy Game

## Setup Instructions

After opening the Unity project:

1. **Run Scene Setup** (one-time)
   - Go to menu: `Cerebrum > Setup Scenes`
   - This will create:
     - `Assets/_Project/Scenes/Home.unity`
     - `Assets/_Project/Scenes/Game.unity`
     - All required prefabs in `Assets/_Project/Resources/Prefabs/`
     - Update Build Settings with correct scene order

2. **Play the Game**
   - Open `Assets/_Project/Scenes/Home.unity`
   - Press Play
   - Click "Start Game" to load the game board

## Project Structure

```
Assets/_Project/
├── Scenes/
│   ├── Home.unity          # Title screen with Start Game button
│   └── Game.unity          # Main game board
├── Scripts/
│   ├── Core/               # Scene loading, game state
│   ├── Data/               # Models (Player, Clue, Category, Board)
│   ├── Game/               # GameManager, BoardController
│   ├── UI/                 # Views and UI components
│   ├── Input/              # Input abstraction (future)
│   ├── OpenAI/             # TTS/STT services (future)
│   └── Editor/             # Editor tools for scene setup
├── Resources/
│   └── Prefabs/            # Runtime-loadable prefabs
└── README.md
```

## Milestones Implemented

### Milestone 1: UI Scaffolding ✓
- Home scene with Start Game navigation
- Game scene with 6x5 Jeopardy grid
- Category headers and player panels
- Clue overlay with question display
- Used clue state tracking

### Milestone 2: Data Layer ✓
- TSV parser for 41 season files in `Assets/Clues/`
- Category construction from historical data
- Random board generation with 6 unique categories
- Real clue data displayed in overlay

## Data Source

The game loads Jeopardy clue data from `Assets/Clues/*.tsv` files.
Each TSV contains columns:
- round, clue_value, daily_double_value, category, comments, answer, question, air_date, notes

Note: In Jeopardy format, "answer" = clue shown to players, "question" = correct response.

## Players (Hardcoded for MVP)
- Cameron
- Calder  
- Lauren

## Upcoming Milestones

- **Milestone 3**: OpenAI TTS for reading questions aloud
- **Milestone 4**: Speech input + answer judging
- **Milestone 5**: board.fun hardware integration
