using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace GameBox.Games
{
    public partial class MemoryMatchGame : Window
    {
        private List<Button> cardButtons = new List<Button>();
        private List<string> cardValues = new List<string>();
        private List<int> flippedCards = new List<int>();
        private int matchedPairs = 0;
        private int moves = 0;
        private DispatcherTimer flipBackTimer = null!;
        private bool isFlipping = false;
        private Random random = new Random();

        // Card emojis for the memory game
        private readonly string[] cardEmojis = {
            "🎮", "🎯", "🎲", "🎪", "🎨", "🎭", "🎸", "🎹",
            "🎺", "🎻", "🏆", "🏅", "⚽", "🏀", "🎾", "🏈",
            "⚾", "🏐", "🏓", "🏸", "🚗", "🚕", "🚙", "🚌"
        };

        public MemoryMatchGame()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            SetupNewGame();
        }

        private void SetupNewGame()
        {
            // Clear previous game
            GameGrid.Children.Clear();
            cardButtons.Clear();
            cardValues.Clear();
            flippedCards.Clear();
            
            matchedPairs = 0;
            moves = 0;
            isFlipping = false;
            
            // Setup timer for flipping cards back
            flipBackTimer = new DispatcherTimer();
            flipBackTimer.Interval = TimeSpan.FromMilliseconds(1500);
            flipBackTimer.Tick += FlipBackTimer_Tick;
            
            // Generate card pairs (4x4 grid = 16 cards = 8 pairs)
            GenerateCardPairs();
            CreateCardButtons();
            UpdateStatusText();
        }

        private void GenerateCardPairs()
        {
            cardValues.Clear();
            
            // Select 8 random emojis for pairs
            var selectedEmojis = cardEmojis.OrderBy(x => random.Next()).Take(8).ToList();
            
            // Add each emoji twice to create pairs
            foreach (var emoji in selectedEmojis)
            {
                cardValues.Add(emoji);
                cardValues.Add(emoji);
            }
            
            // Shuffle the cards
            cardValues = cardValues.OrderBy(x => random.Next()).ToList();
        }

        private void CreateCardButtons()
        {
            for (int i = 0; i < 16; i++)
            {
                var button = new Button
                {
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(2),
                    Background = Brushes.DodgerBlue,
                    Foreground = Brushes.White,
                    Content = "?",
                    BorderBrush = Brushes.DarkBlue,
                    BorderThickness = new Thickness(2),
                    Tag = i // Store card index
                };
                
                button.Click += Card_Click;
                
                int row = i / 4;
                int col = i % 4;
                Grid.SetRow(button, row);
                Grid.SetColumn(button, col);
                
                GameGrid.Children.Add(button);
                cardButtons.Add(button);
            }
        }

        private void Card_Click(object sender, RoutedEventArgs e)
        {
            if (isFlipping) return;
            
            var button = sender as Button;
            if (button == null) return;
            
            int cardIndex = (int)button.Tag;
            
            // Don't allow clicking already flipped or matched cards
            if (flippedCards.Contains(cardIndex) || button.Background == Brushes.LightGreen)
                return;
            
            // Flip the card
            FlipCard(cardIndex, true);
            flippedCards.Add(cardIndex);
            
            // Check for match when 2 cards are flipped
            if (flippedCards.Count == 2)
            {
                moves++;
                UpdateStatusText();
                CheckForMatch();
            }
        }

        private void FlipCard(int cardIndex, bool showValue)
        {
            var button = cardButtons[cardIndex];
            
            if (showValue)
            {
                button.Content = cardValues[cardIndex];
                button.Background = Brushes.White;
                button.Foreground = Brushes.Black;
            }
            else
            {
                button.Content = "?";
                button.Background = Brushes.DodgerBlue;
                button.Foreground = Brushes.White;
            }
        }

        private void CheckForMatch()
        {
            if (flippedCards.Count != 2) return;
            
            int card1 = flippedCards[0];
            int card2 = flippedCards[1];
            
            if (cardValues[card1] == cardValues[card2])
            {
                // Match found!
                cardButtons[card1].Background = Brushes.LightGreen;
                cardButtons[card2].Background = Brushes.LightGreen;
                cardButtons[card1].Foreground = Brushes.DarkGreen;
                cardButtons[card2].Foreground = Brushes.DarkGreen;
                
                matchedPairs++;
                flippedCards.Clear();
                
                // Check if game is complete
                if (matchedPairs == 8)
                {
                    GameComplete();
                }
            }
            else
            {
                // No match - flip cards back after delay
                isFlipping = true;
                flipBackTimer.Start();
            }
        }

        private void FlipBackTimer_Tick(object? sender, EventArgs e)
        {
            flipBackTimer.Stop();
            
            // Flip unmatched cards back
            foreach (int cardIndex in flippedCards)
            {
                FlipCard(cardIndex, false);
            }
            
            flippedCards.Clear();
            isFlipping = false;
        }

        private void GameComplete()
        {
            string performance = moves switch
            {
                <= 12 => "Perfect! 🌟",
                <= 16 => "Excellent! 🎉",
                <= 20 => "Good job! 👍",
                _ => "Well done! ✨"
            };
            
            StatusText.Text = $"Game Complete! {performance}";
            
            MessageBox.Show($"Congratulations! 🎉\n\nYou completed the game in {moves} moves!\n{performance}", 
                "Memory Match Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateStatusText()
        {
            StatusText.Text = $"Moves: {moves} | Pairs found: {matchedPairs}/8";
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            flipBackTimer?.Stop();
            SetupNewGame();
        }

        private void BackToMenu_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            flipBackTimer?.Stop();
        }
    }
}