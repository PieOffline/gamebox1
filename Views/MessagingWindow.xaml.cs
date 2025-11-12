using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GameBox.Utils;

namespace GameBox.Views
{
    public partial class MessagingWindow : Window
    {
        private TcpListener? listener;
        private TcpClient? client;
        private NetworkStream? stream;
        private bool isConnected = false;
        private string? connectedFruitCode;
        private const int MessagingPort = 42424;

        public MessagingWindow()
        {
            InitializeComponent();
            StartListening();
        }

        private async void StartListening()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, MessagingPort);
                listener.Start();
                StatusText.Text = "Listening for incoming connections...";
                
                // Accept connections in the background
                _ = Task.Run(async () =>
                {
                    while (listener != null)
                    {
                        try
                        {
                            var incomingClient = await listener.AcceptTcpClientAsync();
                            
                            Dispatcher.Invoke(() =>
                            {
                                if (!isConnected)
                                {
                                    client = incomingClient;
                                    stream = client.GetStream();
                                    isConnected = true;
                                    
                                    // Get the IP and fruit code of the connected peer
                                    var endpoint = (IPEndPoint?)client.Client.RemoteEndPoint;
                                    if (endpoint != null)
                                    {
                                        connectedFruitCode = NetworkUtils.IpToFruitCode(endpoint.Address.ToString());
                                        StatusText.Text = $"Connected to {connectedFruitCode}";
                                        StatusText.Foreground = Brushes.Green;
                                    }
                                    
                                    _ = Task.Run(ListenForMessages);
                                }
                                else
                                {
                                    // Already connected, close the new connection
                                    incomingClient.Close();
                                }
                            });
                        }
                        catch
                        {
                            break;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to start listening: {ex.Message}";
                
                StatusText.Foreground = Brushes.Red;
            }
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (isConnected)
            {
                MessageBox.Show("Already connected to someone. Close the current connection first.", 
                    "Already Connected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string fruitCode = FruitCodeTextBox.Text.Trim();
            if (string.IsNullOrEmpty(fruitCode))
            {
                MessageBox.Show("Please enter a fruit code to connect to.", 
                    "No Fruit Code", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Convert fruit code to IP
                string? targetIp = NetworkUtils.FruitCodeToIp(fruitCode);
                if (targetIp == null)
                {
                    MessageBox.Show($"Could not resolve fruit code '{fruitCode}' to an IP address.", 
                        "Invalid Fruit Code", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                StatusText.Text = $"Connecting to {fruitCode}...";
                StatusText.Foreground = Brushes.Gray;
                
                client = new TcpClient();
                await client.ConnectAsync(targetIp, MessagingPort);
                stream = client.GetStream();
                isConnected = true;
                connectedFruitCode = fruitCode;
                
                StatusText.Text = $"Connected to {fruitCode}";
                StatusText.Foreground = Brushes.Green;
                
                _ = Task.Run(ListenForMessages);
            }
            catch (Exception ex)
            {
                StatusText.Text = "Failed to connect";
                StatusText.Foreground = Brushes.Red;
                MessageBox.Show($"Failed to connect to {fruitCode}: {ex.Message}", 
                    "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ListenForMessages()
        {
            try
            {
                byte[] buffer = new byte[4096];
                
                while (client?.Connected == true && stream != null)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        
                        Dispatcher.Invoke(() =>
                        {
                            DisplayMessage(message, false);
                        });
                    }
                }
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    if (isConnected)
                    {
                        StatusText.Text = "Connection lost";
                        StatusText.Foreground = Brushes.Red;
                        isConnected = false;
                        connectedFruitCode = null;
                    }
                });
            }
        }

        private void DisplayMessage(string message, bool isSent)
        {
            var border = new Border
            {
                Background = isSent ? new SolidColorBrush(Color.FromRgb(220, 240, 255)) : 
                                      new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                BorderBrush = isSent ? Brushes.Blue : Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10),
                Margin = new Thickness(5),
                HorizontalAlignment = isSent ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 350
            };

            var stackPanel = new StackPanel();
            
            var senderLabel = new TextBlock
            {
                Text = isSent ? "You" : (connectedFruitCode ?? "Unknown"),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = isSent ? Brushes.Blue : Brushes.DarkGray,
                Margin = new Thickness(0, 0, 0, 3)
            };

            var messageText = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14
            };

            var timeText = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm:ss"),
                FontSize = 9,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 3, 0, 0)
            };

            stackPanel.Children.Add(senderLabel);
            stackPanel.Children.Add(messageText);
            stackPanel.Children.Add(timeText);

            border.Child = stackPanel;
            MessagePanel.Children.Add(border);

            // Auto-scroll to bottom
            MessageScrollViewer.ScrollToBottom();
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        private async void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await SendMessage();
                e.Handled = true;
            }
        }

        private async Task SendMessage()
        {
            if (!isConnected || stream == null)
            {
                MessageBox.Show("Not connected to anyone. Please connect first.", 
                    "Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string message = MessageTextBox.Text.Trim();
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
                
                DisplayMessage(message, true);
                MessageTextBox.Clear();
            }
            catch (Exception ex)
            {
                StatusText.Text = "Failed to send message";
                StatusText.Foreground = Brushes.Red;
                MessageBox.Show($"Failed to send message: {ex.Message}", 
                    "Send Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            try
            {
                stream?.Close();
                client?.Close();
                listener?.Stop();
            }
            catch { }
        }
    }
}
