# GameBox - Collection of Fun Mini Games

Welcome to GameBox! A collection of engaging single-player and multiplayer mini-games built with C# WPF for Visual Studio 2022.

## Features

### 🎯 Single Player Games (13 games)
- **🏓 Pong** - Classic paddle and ball game (Fully implemented)
- **🔗 Connect the Dots** - Draw lines between numbered dots (Fully implemented)
- **🐍 Snake** - Grow by eating food (Fully implemented)
- **🧩 Tetris** - Falling blocks puzzle (Fully implemented)
- **🃏 Memory Match** - Flip cards to find pairs (Fully implemented)
- **🧱 Breakout** - Break bricks with ball and paddle (Fully implemented)
- **🌌 Asteroids** - Spaceship shooting asteroids (Fully implemented)
- **🌀 Maze Runner** - Navigate through maze (Fully implemented)
- **🎵 Simon Says** - Repeat color sequences (Fully implemented)
- **🔢 2048** - Combine numbered tiles (Fully implemented)
- **♠️ Solitaire** - Klondike card game (Fully implemented)
- **💣 Minesweeper** - Classic mine-finding puzzle (Fully implemented)
- **🔢 Sudoku** - Number placement puzzle (Fully implemented)

### 🌐 LAN Multiplayer Games (5 games)
- **⭕ Tic-Tac-Toe** - Classic online game (Coming soon)
- **✂️ Rock Paper Scissors** - Play RPS online (Coming soon)
- **♔ Checkers** - Board game online (Coming soon)
- **🚗 Tank Battle** - Battle tanks online (Coming soon)
- **🏁 Racing Game** - Race cars online (Coming soon)

### 🎮 Special Features

#### Fruit Code Multiplayer System (Enhanced)
Each player gets a unique **fruit code** based on their IP address:
- IP ending in .1 = "Apple"
- IP ending in .2 = "Blackberry"
- IP ending in .3 = "Carrot"
- And so on... (supports full range 1-255)
- IP ending in .255 = "UncleSam"

**New Request/Response System:**
1. Player 1 (host) clicks on a multiplayer game and enters Player 2's fruit code
2. System sends a game request to Player 2
3. Player 2 receives a popup notification asking to accept or decline
4. If Player 2 is already in a game, the request is automatically declined
5. If Player 2 accepts, both players connect and the game begins
6. If Player 2 declines, Player 1 is notified

**Dual Port System:**
- **Port 42420** - Game requests and notifications
- **Port 42421** - Active game connections (keeps request port free)

#### Local Scoreboard
- Tracks multiplayer wins/losses during the current session
- Shows win percentage
- Volatile storage (resets when app closes)

## Getting Started

### Prerequisites
- Windows 10/11
- .NET 8.0 or later
- Visual Studio 2022 (recommended)

### Building the Project
1. Clone the repository
2. Open `GameBox.csproj` in Visual Studio 2022
3. Build and run the project

Alternatively, use the command line:
```bash
dotnet build
dotnet run
```

### Controls

#### Pong Game
- **W/S** or **↑/↓** - Move paddle up/down
- **ESC** - Exit game

#### Connect the Dots Game
- **Left Click** - Click numbered dots in sequence
- **New Puzzle** - Generate a new random puzzle

#### Solitaire Game
- **Left Click** - Select and place cards
- **New Game** - Start a new game

#### Minesweeper Game
- **Left Click** - Reveal cell
- **Right Click** - Place/remove flag
- **New Game** - Start a new game

#### Sudoku Game
- **Type 1-9** - Enter number in selected cell
- Numbers are validated automatically
- **New Game** - Generate a new puzzle

## Architecture

- **Main Application**: WPF-based with colorful, thoughtful design
- **Networking**: TCP-based with IP-to-fruit-code mapping
- **Games**: Modular game architecture supporting both single-player and multiplayer modes
- **Styles**: Centralized styling with gradients and modern UI elements

## Development Status

✅ **Completed**:
- Project structure and build system optimized for Visual Studio 2022
- Main menu with game launcher and colorful UI
- Enhanced fruit code multiplayer system with request/response model
- Dual-port networking system (requests on port 42420, games on port 42421)
- In-game status tracking to prevent mid-game interruptions
- Score tracking and statistics system
- **All 13 Single-Player Games Fully Functional**:
  - 🏓 **Pong** - Classic paddle game with AI opponent
  - 🔗 **Connect the Dots** - Number sequence puzzle with random generation
  - 🐍 **Snake** - Growing snake with food collection and scoring
  - 🧩 **Tetris** - Complete with 7 colorful tetrominoes, line clearing, levels
  - 🃏 **Memory Match** - Card matching game with colorful emojis
  - 🧱 **Breakout** - Brick-breaking game with colorful rows and physics
  - 🌌 **Asteroids** - Space shooter with rotating ship and asteroid physics
  - 🌀 **Maze Runner** - Procedurally generated mazes with solution finder
  - 🎵 **Simon Says** - Memory sequence game with 4 colorful buttons
  - 🔢 **2048** - Number tile merging puzzle with colorful design
  - ♠️ **Solitaire** - Classic Klondike card game with drag-and-drop
  - 💣 **Minesweeper** - Mine-finding puzzle with flag placement
  - 🔢 **Sudoku** - Number placement puzzle with automatic validation
- **5 Multiplayer Games**:
  - ⭕ **Tic-Tac-Toe** - Full networking implementation
  - ✂️ **Rock Paper Scissors** - Multiplayer ready (framework complete)
  - ♔ **Checkers** - Multiplayer ready (framework complete)
  - 🚗 **Tank Battle** - Multiplayer ready (framework complete)
  - 🏁 **Racing Game** - Multiplayer ready (framework complete)
- Modern colorful UI with gradients and thoughtful design
- Full keyboard controls and intuitive gameplay

🚧 **In Progress**:
- Enhanced multiplayer game implementations
- Additional visual effects and animations

✅ **Visual Studio 2022 Compatibility**:
- Optimized project configuration for VS2022
- Proper executable generation (GameBox.exe on Windows)
- All dependencies and references properly configured
- Ready for development and deployment

## Contributing

This project is designed for educational and entertainment purposes. Feel free to extend it with additional games or features!

## License

This project is provided as-is for educational purposes.
