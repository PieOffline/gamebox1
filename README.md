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
- And so on...

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
- Project structure and build system
- Main menu with game launcher
- Fruit code multiplayer system
- Score tracking system
- Pong game (fully functional)
- Connect the Dots game (fully functional)
- Colorful UI with modern styling

🚧 **In Progress**:
- Additional single-player games
- Multiplayer networking implementation
- Game-specific multiplayer features

📋 **Planned**:
- Complete all 15 mini-games
- Enhanced UI/UX features
- Game statistics and achievements
- Save/load functionality

## Contributing

This project is designed for educational and entertainment purposes. Feel free to extend it with additional games or features!

## License

This project is provided as-is for educational purposes.
