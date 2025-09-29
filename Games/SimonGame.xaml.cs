using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GameBox.Games
{
    public partial class SimonGame : Window
    {
        private List<int> gameSequence = new List<int>();
        private List<int> playerSequence = new List<int>();
        private int currentRound = 1;
        private int playerIndex = 0;
        private bool gameActive = false;
        private bool showingSequence = false;
        private bool acceptingInput = false;
        private int bestScore = 0;
        
        private Random random = new Random();
        private DispatcherTimer sequenceTimer = null!;
        private DispatcherTimer buttonFlashTimer = null!;
        private int sequenceIndex = 0;
        
        private readonly Button[] colorButtons = new Button[4];
        private readonly Brush[] normalColors = {
            Brushes.Red,
            Brushes.Green, 
            Brushes.Blue,
            Brushes.Yellow
        };
        
        private readonly Brush[] flashColors = {
            Brushes.LightCoral,
            Brushes.LightGreen,
            Brushes.LightBlue,
            Brushes.LightYellow
        };
        
        private readonly string[] colorNames = { "Red", "Green", "Blue", "Yellow" };

        public SimonGame()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            // Store references to color buttons
            colorButtons[0] = RedButton;
            colorButtons[1] = GreenButton;
            colorButtons[2] = BlueButton;
            colorButtons[3] = YellowButton;
            
            // Setup timers
            sequenceTimer = new DispatcherTimer();
            sequenceTimer.Interval = TimeSpan.FromMilliseconds(800);
            sequenceTimer.Tick += SequenceTimer_Tick;
            
            buttonFlashTimer = new DispatcherTimer();
            buttonFlashTimer.Interval = TimeSpan.FromMilliseconds(400);
            buttonFlashTimer.Tick += ButtonFlashTimer_Tick;
            
            UpdateUI();
            StartNewGame();
        }

        private void StartNewGame()
        {
            gameSequence.Clear();
            playerSequence.Clear();
            currentRound = 1;
            playerIndex = 0;
            gameActive = true;
            showingSequence = false;
            acceptingInput = false;
            
            // Add first color to sequence
            AddNewColorToSequence();
            
            UpdateUI();
            ShowSequence();
        }

        private void AddNewColorToSequence()
        {
            int newColor = random.Next(4);
            gameSequence.Add(newColor);
        }

        private void ShowSequence()
        {
            showingSequence = true;
            acceptingInput = false;
            sequenceIndex = 0;
            
            StatusText.Text = "Watch the sequence!";
            SetButtonsEnabled(false);
            
            // Start sequence display
            sequenceTimer.Start();
        }

        private void SequenceTimer_Tick(object? sender, EventArgs e)
        {
            if (sequenceIndex < gameSequence.Count)
            {
                // Flash the current color in sequence
                FlashButton(gameSequence[sequenceIndex]);
                sequenceIndex++;
            }
            else
            {
                // Sequence complete, wait for player input
                sequenceTimer.Stop();
                showingSequence = false;
                acceptingInput = true;
                playerSequence.Clear();
                playerIndex = 0;
                
                StatusText.Text = "Your turn! Repeat the sequence.";
                SetButtonsEnabled(true);
            }
        }

        private void FlashButton(int colorIndex)
        {
            // Flash the button
            colorButtons[colorIndex].Background = flashColors[colorIndex];
            
            // Play a visual flash animation
            buttonFlashTimer.Tag = colorIndex;
            buttonFlashTimer.Start();
        }

        private void ButtonFlashTimer_Tick(object? sender, EventArgs e)
        {
            buttonFlashTimer.Stop();
            
            if (buttonFlashTimer.Tag is int colorIndex)
            {
                // Return button to normal color
                colorButtons[colorIndex].Background = normalColors[colorIndex];
            }
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (!acceptingInput || !gameActive) return;
            
            var button = sender as Button;
            if (button?.Tag is string tagString && int.TryParse(tagString, out int colorIndex))
            {
                ProcessPlayerInput(colorIndex);
            }
        }

        private void ColorButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!acceptingInput || !gameActive) return;
            
            var button = sender as Button;
            if (button?.Tag is string tagString && int.TryParse(tagString, out int colorIndex))
            {
                // Visual feedback when pressed
                button.Background = flashColors[colorIndex];
            }
        }

        private void ColorButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is string tagString && int.TryParse(tagString, out int colorIndex))
            {
                // Return to normal color
                button.Background = normalColors[colorIndex];
            }
        }

        private void ProcessPlayerInput(int colorIndex)
        {
            playerSequence.Add(colorIndex);
            
            // Check if the input matches the sequence so far
            if (playerSequence[playerIndex] != gameSequence[playerIndex])
            {
                // Wrong input - game over
                GameOver();
                return;
            }
            
            playerIndex++;
            
            // Check if player completed the current sequence
            if (playerIndex >= gameSequence.Count)
            {
                // Player completed the sequence correctly
                RoundComplete();
            }
        }

        private void RoundComplete()
        {
            acceptingInput = false;
            currentRound++;
            
            // Update best score
            if (currentRound - 1 > bestScore)
            {
                bestScore = currentRound - 1;
            }
            
            StatusText.Text = $"Great! Round {currentRound - 1} complete!";
            
            // Add new color to sequence after a short delay
            var delayTimer = new DispatcherTimer();
            delayTimer.Interval = TimeSpan.FromMilliseconds(1500);
            delayTimer.Tick += (s, e) =>
            {
                delayTimer.Stop();
                AddNewColorToSequence();
                UpdateUI();
                ShowSequence();
            };
            delayTimer.Start();
        }

        private void GameOver()
        {
            gameActive = false;
            acceptingInput = false;
            showingSequence = false;
            
            sequenceTimer.Stop();
            buttonFlashTimer.Stop();
            
            StatusText.Text = "Game Over!";
            SetButtonsEnabled(false);
            
            string message = $"Game Over!\n\nYou reached round {currentRound - 1}";
            if (currentRound - 1 == bestScore)
            {
                message += "\n🎉 New Best Score!";
            }
            message += $"\n\nSequence was: {string.Join(" → ", gameSequence.Take(currentRound).Select(i => colorNames[i]))}";
            
            MessageBox.Show(message, "Simon Says - Game Over", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SetButtonsEnabled(bool enabled)
        {
            foreach (var button in colorButtons)
            {
                button.IsEnabled = enabled;
            }
            
            ReplayButton.IsEnabled = enabled && gameActive;
        }

        private void UpdateUI()
        {
            ScoreText.Text = $"Round: {currentRound}";
            SequenceLengthText.Text = $"Length: {gameSequence.Count}";
            BestScoreText.Text = $"Best: {bestScore}";
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            sequenceTimer?.Stop();
            buttonFlashTimer?.Stop();
            
            // Reset all button colors
            for (int i = 0; i < colorButtons.Length; i++)
            {
                colorButtons[i].Background = normalColors[i];
            }
            
            StartNewGame();
        }

        private void ReplaySequence_Click(object sender, RoutedEventArgs e)
        {
            if (!gameActive || showingSequence) return;
            
            ShowSequence();
        }

        private void BackToMenu_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            sequenceTimer?.Stop();
            buttonFlashTimer?.Stop();
        }
    }
}