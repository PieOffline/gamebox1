using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GameBox.Games
{
    public partial class SolitaireGame : Window
    {
        private const int CardWidth = 70;
        private const int CardHeight = 100;
        private const int Spacing = 10;
        
        private List<Card> deck = new List<Card>();
        private List<List<Card>> tableau = new List<List<Card>>();
        private List<Card> stock = new List<Card>();
        private List<Card> waste = new List<Card>();
        private List<List<Card>> foundations = new List<List<Card>>();
        private int score = 0;

        public SolitaireGame()
        {
            InitializeComponent();
            NewGame();
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            NewGame();
        }

        private void NewGame()
        {
            score = 0;
            UpdateScore();
            
            // Initialize data structures
            tableau.Clear();
            for (int i = 0; i < 7; i++)
                tableau.Add(new List<Card>());
            
            foundations.Clear();
            for (int i = 0; i < 4; i++)
                foundations.Add(new List<Card>());
            
            stock.Clear();
            waste.Clear();
            
            // Create and shuffle deck
            CreateDeck();
            ShuffleDeck();
            
            // Deal cards to tableau
            int cardIndex = 0;
            for (int col = 0; col < 7; col++)
            {
                for (int row = col; row < 7; row++)
                {
                    var card = deck[cardIndex++];
                    card.IsFaceUp = (row == col);
                    tableau[row].Add(card);
                }
            }
            
            // Remaining cards go to stock
            while (cardIndex < deck.Count)
            {
                stock.Add(deck[cardIndex++]);
            }
            
            DrawBoard();
        }

        private void CreateDeck()
        {
            deck.Clear();
            string[] suits = { "♠", "♥", "♦", "♣" };
            string[] ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
            
            foreach (var suit in suits)
            {
                for (int i = 0; i < ranks.Length; i++)
                {
                    deck.Add(new Card
                    {
                        Suit = suit,
                        Rank = ranks[i],
                        Value = i + 1,
                        IsRed = (suit == "♥" || suit == "♦")
                    });
                }
            }
        }

        private void ShuffleDeck()
        {
            var random = new Random();
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                var temp = deck[i];
                deck[i] = deck[j];
                deck[j] = temp;
            }
        }

        private void DrawBoard()
        {
            GameCanvas.Children.Clear();
            
            double startX = 20;
            double startY = 20;
            
            // Draw stock pile
            DrawPile(stock, startX, startY, "Stock");
            
            // Draw waste pile
            DrawPile(waste, startX + CardWidth + Spacing, startY, "Waste");
            
            // Draw foundations
            for (int i = 0; i < 4; i++)
            {
                double x = startX + (CardWidth + Spacing) * (3 + i);
                DrawFoundation(i, x, startY);
            }
            
            // Draw tableau
            startY += CardHeight + Spacing * 3;
            for (int col = 0; col < 7; col++)
            {
                double x = startX + (CardWidth + Spacing) * col;
                DrawTableauColumn(col, x, startY);
            }
        }

        private void DrawPile(List<Card> pile, double x, double y, string name)
        {
            var rect = new Rectangle
            {
                Width = CardWidth,
                Height = CardHeight,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromRgb(45, 90, 61)),
                RadiusX = 5,
                RadiusY = 5
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            GameCanvas.Children.Add(rect);
            
            rect.MouseLeftButtonDown += (s, e) =>
            {
                if (name == "Stock" && stock.Count > 0)
                {
                    var card = stock[stock.Count - 1];
                    stock.RemoveAt(stock.Count - 1);
                    card.IsFaceUp = true;
                    waste.Add(card);
                    DrawBoard();
                }
                else if (name == "Stock" && stock.Count == 0 && waste.Count > 0)
                {
                    // Reset stock from waste
                    while (waste.Count > 0)
                    {
                        var card = waste[waste.Count - 1];
                        waste.RemoveAt(waste.Count - 1);
                        card.IsFaceUp = false;
                        stock.Add(card);
                    }
                    DrawBoard();
                }
            };
            
            if (pile.Count > 0)
            {
                DrawCard(pile[pile.Count - 1], x, y);
            }
        }

        private void DrawFoundation(int index, double x, double y)
        {
            var rect = new Rectangle
            {
                Width = CardWidth,
                Height = CardHeight,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromRgb(45, 90, 61)),
                RadiusX = 5,
                RadiusY = 5
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            GameCanvas.Children.Add(rect);
            
            if (foundations[index].Count > 0)
            {
                DrawCard(foundations[index][foundations[index].Count - 1], x, y);
            }
        }

        private void DrawTableauColumn(int col, double x, double startY)
        {
            if (tableau[col].Count == 0)
            {
                var rect = new Rectangle
                {
                    Width = CardWidth,
                    Height = CardHeight,
                    Stroke = Brushes.White,
                    StrokeThickness = 2,
                    Fill = new SolidColorBrush(Color.FromRgb(45, 90, 61)),
                    RadiusX = 5,
                    RadiusY = 5
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, startY);
                GameCanvas.Children.Add(rect);
                return;
            }
            
            for (int i = 0; i < tableau[col].Count; i++)
            {
                double y = startY + i * 25;
                DrawCard(tableau[col][i], x, y);
            }
        }

        private void DrawCard(Card card, double x, double y)
        {
            var rect = new Rectangle
            {
                Width = CardWidth,
                Height = CardHeight,
                Fill = card.IsFaceUp ? Brushes.White : new SolidColorBrush(Color.FromRgb(25, 50, 100)),
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                RadiusX = 5,
                RadiusY = 5
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            GameCanvas.Children.Add(rect);
            
            if (card.IsFaceUp)
            {
                var text = new TextBlock
                {
                    Text = $"{card.Rank}\n{card.Suit}",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = card.IsRed ? Brushes.Red : Brushes.Black,
                    TextAlignment = TextAlignment.Center,
                    Width = CardWidth,
                    Height = CardHeight,
                    Padding = new Thickness(5)
                };
                Canvas.SetLeft(text, x);
                Canvas.SetTop(text, y);
                GameCanvas.Children.Add(text);
            }
        }

        private void UpdateScore()
        {
            ScoreText.Text = score.ToString();
        }

        private class Card
        {
            public string Suit { get; set; } = "";
            public string Rank { get; set; } = "";
            public int Value { get; set; }
            public bool IsRed { get; set; }
            public bool IsFaceUp { get; set; }
        }
    }
}
