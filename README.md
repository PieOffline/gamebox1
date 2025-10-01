# GameBox - Collection of Fun Mini Games

Welcome to GameBox! A collection of engaging single-player and multiplayer mini-games built with C# WPF for Visual Studio 2022.

## Features

### 🎯 Single Player Games (10 games)
- **🏓 Pong** - Classic paddle and ball game (Fully implemented)
- **🔗 Connect the Dots** - Draw lines between numbered dots (Fully implemented)
- **🐍 Snake** - Grow by eating food (Coming soon)
- **🧩 Tetris** - Falling blocks puzzle (Coming soon)
- **🃏 Memory Match** - Flip cards to find pairs (Coming soon)
- **🧱 Breakout** - Break bricks with ball and paddle (Coming soon)
- **🌌 Asteroids** - Spaceship shooting asteroids (Coming soon)
- **🌀 Maze Runner** - Navigate through maze (Coming soon)
- **🎵 Simon Says** - Repeat color sequences (Coming soon)
- **🔢 2048** - Combine numbered tiles (Coming soon)

### 🌐 LAN Multiplayer Games (5 games)
- **⭕ Tic-Tac-Toe** - Classic online game (Coming soon)
- **✂️ Rock Paper Scissors** - Play RPS online (Coming soon)
- **♔ Checkers** - Board game online (Coming soon)
- **🚗 Tank Battle** - Battle tanks online (Coming soon)
- **🏁 Racing Game** - Race cars online (Coming soon)

### 🎮 Special Features

#### Fruit Code Multiplayer System
Each player gets a unique **fruit code** based on their IP address:
- IP ending in .1 = "Apple"
- IP ending in .2 = "Blackberry"
- IP ending in .3 = "Carrot"
- And so on... (supports full range 1-255)
- IP ending in .255 = "UncleSam"

To play multiplayer games:
1. Player 1 (host) clicks on a multiplayer game
2. Enter your opponent's fruit code (e.g., "Apple", "Blackberry")
3. The system connects you over LAN
4. Start playing!

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

## Architecture

- **Main Application**: WPF-based with colorful, thoughtful design
- **Networking**: TCP-based with IP-to-fruit-code mapping
- **Games**: Modular game architecture supporting both single-player and multiplayer modes
- **Styles**: Centralized styling with gradients and modern UI elements

## Development Status

✅ **Completed**:
- Project structure and build system optimized for Visual Studio 2022
- Main menu with game launcher and colorful UI
- Fruit code multiplayer system with LAN networking
- Score tracking and statistics system
- **All 10 Single-Player Games Fully Functional**:
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
