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
    public partial class ConnectDotsGame : Window
    {
        private List<Ellipse> dots = new List<Ellipse>();
        private List<Point> dotPositions = new List<Point>();
        private List<Line> lines = new List<Line>();
        private int currentDotIndex = 0;
        private bool gameCompleted = false;
        private Random random = new Random();

        public ConnectDotsGame()
        {
            InitializeComponent();
            GenerateNewPuzzle();
        }

        private void GenerateNewPuzzle()
        {
            ClearGame();
            
            // Generate random dots
            int numberOfDots = random.Next(8, 15);
            GenerateRandomDots(numberOfDots);
            
            // Reset game state
            currentDotIndex = 0;
            gameCompleted = false;
            UpdateStatusText();
        }

        private void ClearGame()
        {
            foreach (var dot in dots)
                GameCanvas.Children.Remove(dot);
            foreach (var line in lines)
                GameCanvas.Children.Remove(line);
            
            dots.Clear();
            dotPositions.Clear();
            lines.Clear();
        }

        private void GenerateRandomDots(int count)
        {
            dotPositions.Clear();
            
            // Generate positions ensuring minimum distance between dots
            for (int i = 0; i < count; i++)
            {
                Point newPosition;
                bool validPosition;
                int attempts = 0;
                
                do
                {
                    newPosition = new Point(
                        random.Next(50, (int)GameCanvas.Width - 50),
                        random.Next(50, (int)GameCanvas.Height - 50)
                    );
                    
                    validPosition = dotPositions.All(p => 
                        Math.Sqrt(Math.Pow(p.X - newPosition.X, 2) + Math.Pow(p.Y - newPosition.Y, 2)) > 60);
                    
                    attempts++;
                } while (!validPosition && attempts < 100);
                
                dotPositions.Add(newPosition);
            }

            // Create visual dots
            for (int i = 0; i < dotPositions.Count; i++)
            {
                var dot = new Ellipse
                {
                    Width = 30,
                    Height = 30,
                    Fill = i == 0 ? Brushes.LimeGreen : Brushes.SkyBlue,
                    Stroke = Brushes.DarkBlue,
                    StrokeThickness = 2,
                    Cursor = Cursors.Hand
                };
                
                Canvas.SetLeft(dot, dotPositions[i].X - 15);
                Canvas.SetTop(dot, dotPositions[i].Y - 15);
                
                // Add number label
                var label = new TextBlock
                {
                    Text = (i + 1).ToString(),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                Canvas.SetLeft(label, dotPositions[i].X - 8);
                Canvas.SetTop(label, dotPositions[i].Y - 8);
                
                dot.MouseLeftButtonDown += Dot_Click;
                dot.Tag = i; // Store dot index
                
                dots.Add(dot);
                GameCanvas.Children.Add(dot);
                GameCanvas.Children.Add(label);
            }
        }

        private void Dot_Click(object sender, MouseButtonEventArgs e)
        {
            if (gameCompleted) return;
            
            var clickedDot = sender as Ellipse;
            if (clickedDot == null) return;
            
            int dotIndex = (int)clickedDot.Tag;
            
            if (dotIndex == currentDotIndex)
            {
                // Correct dot clicked
                clickedDot.Fill = Brushes.LimeGreen;
                
                // Draw line to previous dot
                if (currentDotIndex > 0)
                {
                    var line = new Line
                    {
                        X1 = dotPositions[currentDotIndex - 1].X,
                        Y1 = dotPositions[currentDotIndex - 1].Y,
                        X2 = dotPositions[currentDotIndex].X,
                        Y2 = dotPositions[currentDotIndex].Y,
                        Stroke = Brushes.LimeGreen,
                        StrokeThickness = 3
                    };
                    
                    lines.Add(line);
                    GameCanvas.Children.Add(line);
                }
                
                currentDotIndex++;
                
                // Check if game completed
                if (currentDotIndex >= dots.Count)
                {
                    gameCompleted = true;
                    MessageBox.Show("Congratulations! You've connected all the dots!", 
                        "Puzzle Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                UpdateStatusText();
            }
            else
            {
                // Wrong dot clicked
                MessageBox.Show($"Click dot number {currentDotIndex + 1}!", 
                    "Wrong Dot", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateStatusText()
        {
            if (gameCompleted)
            {
                StatusText.Text = "Puzzle Complete! 🎉";
            }
            else
            {
                StatusText.Text = $"Connect dot {currentDotIndex + 1} of {dots.Count}";
            }
        }

        private void NewPuzzle_Click(object sender, RoutedEventArgs e)
        {
            GenerateNewPuzzle();
        }

        private void BackToMenu_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}