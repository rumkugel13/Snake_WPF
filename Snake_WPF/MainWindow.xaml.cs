using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SnakeWPF
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            lb_connected.Visibility = Visibility.Hidden;
            lb_count.Visibility = Visibility.Hidden;
            lb_pause.Visibility = Visibility.Hidden;

            textBox.Visibility = Visibility.Hidden;
            button.Visibility = Visibility.Hidden;
            label.Visibility = Visibility.Hidden;

            playerstats[0] = ps_player_1;
            playerstats[1] = ps_player_2;
            CreateField();
        }

        bool running = false;
        bool pause = false;
        Rectangle[,] block;
        const int fieldSizeX = 29;
        const int fieldSizeY = 21;
        const int blockSize = 23;
        const int maxFood = 2;
        Brush backgroundColor = Brushes.Black;
        Brush wallColor = Brushes.DarkGray;
        int speed = 200;
        long count = 0;

        bool debug = false;

        Client client;
        bool multiPC = false;
        bool asServer;
        Server server;

        public Brush[] brushes = new Brush[] { Brushes.Red, Brushes.DarkRed, Brushes.Lime,
            Brushes.DarkGreen, Brushes.Blue, Brushes.DarkBlue, Brushes.DarkOrange, Brushes.Yellow,
            Brushes.Purple, Brushes.DeepPink, Brushes.Cyan, Brushes.Turquoise };

        int players = 1;
        PlayerStats[] playerstats = new PlayerStats[2];
        static Snake[] snake = new Snake[2];

        public enum Directions { Left, Up, Right, Down }

        void CreateField()
        {
            block = new Rectangle[fieldSizeX, fieldSizeY];
            for (int i = 0; i < fieldSizeX; i++)
            {
                for (int j = 0; j < fieldSizeY; j++)
                {
                    block[i, j] = new Rectangle();
                    block[i, j].VerticalAlignment = VerticalAlignment.Top;
                    block[i, j].HorizontalAlignment = HorizontalAlignment.Left;
                    block[i, j].Height = blockSize;
                    block[i, j].Width = blockSize;
                    block[i, j].StrokeThickness = 1;
                    ChangeColor(i, j, backgroundColor);
                    block[i, j].Margin = new Thickness((block[i, j].Width * i) + rec_field.Margin.Left, (block[i, j].Height * j) + rec_field.Margin.Top, 0, 0);
                    //rec[i,j].ToolTip = i + "|" + j;
                    grid_window.Children.Add(block[i, j]);
                }
            }
        }

        void Window_KeyDown(object sender, KeyEventArgs e)
        {
            Directions direction = snake[0].Direction;
            int snakeNr = 0;

            if (running && !pause)
            {
                switch (e.Key)
                {
                    case (Key.Left):
                        direction = Directions.Left;
                        snakeNr = (players == 2) ? 1 : snakeNr;
                        break;
                    case (Key.A): direction = Directions.Left; break;

                    case (Key.Up):
                        direction = Directions.Up;
                        snakeNr = (players == 2) ? 1 : snakeNr;
                        break;
                    case (Key.W): direction = Directions.Up; break;

                    case (Key.Right):
                        direction = Directions.Right;
                        snakeNr = (players == 2) ? 1 : snakeNr;
                        break;
                    case (Key.D): direction = Directions.Right; break;

                    case (Key.Down):
                        direction = Directions.Down;
                        snakeNr = (players == 2) ? 1 : snakeNr;
                        break;
                    case (Key.S): direction = Directions.Down; break;

                    case (Key.Escape): GameOver(true); break;

                    default:
                        break;
                }
                if (multiPC && !asServer)
                    snakeNr = 1;
                else if (multiPC && asServer)
                    snakeNr = 0;
                ChangeDirection(direction, snakeNr);
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:
                    if (!multiPC && running)
                    {
                        pause = !pause;
                        if (pause)
                        {
                            lb_gameover.Visibility = Visibility.Hidden;
                            lb_pause.Visibility = Visibility.Visible;
                            grid_gameover.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            grid_gameover.Visibility = Visibility.Hidden;
                        }
                    }
                    break;
                case Key.F1:
                    if (textBox.Visibility == Visibility.Visible)
                    {
                        textBox.Visibility = Visibility.Hidden;
                        button.Visibility = Visibility.Hidden;
                        label.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        textBox.Visibility = Visibility.Visible;
                        button.Visibility = Visibility.Visible;
                        label.Visibility = Visibility.Visible;
                    }
                    break;
            }
        }

        public void ChangeDirection(Directions direction, int snakeNr)
        {
            Directions check = snake[snakeNr].Direction;
            if ((int)direction - (int)check != 2 && (int)check - (int)direction != 2 /*true when not opposite*/
                && !snake[snakeNr].DirChanged)
            {
                snake[snakeNr].Direction = direction;
                snake[snakeNr].DirChanged = true;
            }
        }

        async void GameTickMulti(bool isAllowed)
        {
            while (pause)
                await Task.Delay(10);

            if (running && isAllowed && asServer)
                CheckAndGoDirection(1);

            if (running && isAllowed && !asServer)
                MoveAllPlayers();
                //CheckAndGoDirection(0);   //ticktock wait on client and server

            //if(running) //ticktock wait on client and server
            if (running && asServer)
            {
                await Task.Delay((int)(speed * 0.75));    //increase speed because of ping
                //await Task.Delay((int)(speed * 0.75 * 0.5));  //ticktock wait on client and server
                //DebugPrint("Waited " + speed*0.75 + "ms\n");
            }

            if (running && asServer)
                CheckAndGoDirection(0);

            //if (running && !asServer)     //ticktock wait on client and server
            //    CheckAndGoDirection(1);

            if (running && asServer && server != null)
            {
                server.SendMessage(new byte[] { 2, (byte)snake[0].Direction, 0, 0 });
                DebugPrint(/*"Sent: */"2|" + (int)snake[0].Direction + "|0|0 => ChangeDirection()\n");
            }

            if (running && !asServer && client != null)
            {
                client.SendMessage(new byte[] { 2, (byte)snake[1].Direction, 1, 0 });
                DebugPrint(/*"Sent: */"2|" + (int)snake[1].Direction + "|1|0 => ChangeDirection()\n");
            }
        }

        async void GameTickSingle()
        {
            do
            {
                await Task.Delay(speed);
                while (pause)
                    await Task.Delay(10);
                MoveAllPlayers();
            }
            while (running);
        }

        async Task Countdown(int countdown)
        {
            while (countdown > 0)
            {
                pause = true;//don't allow direction change
                ShowCountdownNumber(countdown);
                await Task.Delay(1000);
                DebugPrint("Waited " + 1000 + "ms\n");
                countdown--;
                pause = false;
            }
            ShowCountdownNumber(countdown);
        }

        void ShowCountdownNumber(int number)
        {
            lb_count.Visibility = (number > 0) ? Visibility.Visible : Visibility.Hidden;
            lb_count.Content = (number == 0) ? "" : number.ToString();
            MoveAllPlayers();
        }

        void MoveAllPlayers()
        {
            for (int s = 0; running && s < players; s++)
                CheckAndGoDirection(s);
            //DebugPrint("Moved\n");
        }

        void CheckAndGoDirection(int snakeNr)
        {
            int tempX = snake[snakeNr].HeadX;
            int tempY = snake[snakeNr].HeadY;

            switch (snake[snakeNr].Direction)
            {
                case Directions.Left:
                    tempX = (tempX != 0) ? (tempX - 1) : (fieldSizeX - 1);
                    break;
                case Directions.Up:
                    tempY = (tempY != 0) ? (tempY - 1) : (fieldSizeY - 1);
                    break;
                case Directions.Right:
                    tempX = (tempX != fieldSizeX - 1) ? (tempX + 1) : (0);
                    break;
                case Directions.Down:
                    tempY = (tempY != fieldSizeY - 1) ? (tempY + 1) : (0);
                    break;
            }
            if (CheckNextBlockColor(tempX, tempY, snakeNr))
            {
                //snake[snake_nr].Head_X = temp_x;
                //snake[snake_nr].Head_Y = temp_y;
                MoveSnake(tempX, tempY, snakeNr);
                snake[snakeNr].DirChanged = false;
            }
            else GameOver(true);

            UpdateStats(snakeNr);
        }

        bool CheckNextBlockColor(int x, int y, int snakeNr)  //false if crash
        {
            Random rand = new Random();
            if (block[x, y].Stroke == playerstats[0].BlockColor ||
                block[x, y].Stroke == playerstats[1].BlockColor ||
                block[x, y].Stroke == wallColor)
            {
                return false;
            }
            else if (block[x, y].Stroke != backgroundColor)
            {
                //snake[snake_nr].Length += 20;
                snake[snakeNr].ChangedLength += 2;
                snake[snakeNr].Points += rand.Next(80, 120) + snake[snakeNr].Length;
                snake[snakeNr].Food++;
                if (!multiPC || (multiPC && asServer))
                    AddNewFoodBlock(1);
                return true;
            }
            else return true;   //if color is background
        }

        public void MoveSnake(int newX, int newY, int snakeNr)
        {
            snake[snakeNr].HeadX = newX;
            snake[snakeNr].HeadY = newY;
            snake[snakeNr].CoordsX[snake[snakeNr].Length + 1] = snake[snakeNr].HeadX;
            snake[snakeNr].CoordsY[snake[snakeNr].Length + 1] = snake[snakeNr].HeadY;

            if (snake[snakeNr].ChangedLength == 0)
            {
                for (int i = 0; i <= snake[snakeNr].Length; i++)
                {
                    snake[snakeNr].CoordsX[i] = snake[snakeNr].CoordsX[i + 1];
                    snake[snakeNr].CoordsY[i] = snake[snakeNr].CoordsY[i + 1];
                }
                ChangeColor(snake[snakeNr].CoordsX[0], snake[snakeNr].CoordsY[0], backgroundColor);
            }
            if (snake[snakeNr].ChangedLength > 0)
            {
                snake[snakeNr].ChangedLength--;
                snake[snakeNr].Length++;
            }
            ChangeColor(snake[snakeNr].HeadX, snake[snakeNr].HeadY, playerstats[snakeNr].BlockColor);
        }

        void AddNewFoodBlock(int number)
        {
            Random rand = new Random();
            int foodAdded = 0;
            while (foodAdded < number)
            {
                int randomX = rand.Next(0, fieldSizeX - 1);
                int randomY = rand.Next(0, fieldSizeY - 1);
                if ((block[randomX, randomY].Stroke == backgroundColor))
                {
                    int randomColor = rand.Next(brushes.Length);
                    Brush color = brushes[randomColor];
                    if (color != playerstats[0].BlockColor && color != playerstats[1].BlockColor) //cant be gray/black because brushes
                    {
                        ChangeColor(randomX, randomY, color);
                        foodAdded++;
                        if (multiPC && asServer && server != null)
                            SendCoords(randomX, randomY, randomColor);
                    }
                }
            }
        }

        void ChangeColor(int x, int y, Brush color)
        {
            Color myColor = ((SolidColorBrush)color).Color;
            myColor.A = (byte)(255 * 0.9); // 255 * 0.9 is approx. 230
            block[x, y].Fill = new SolidColorBrush(myColor);
            block[x, y].Stroke = color;
        }

        void SendCoords(int x, int y, int snakeNr)
        {
            server.SendMessage(new byte[] { 3, (byte)x, (byte)y, (byte)snakeNr });
            DebugPrint(/*"Sent: */"3|" + x + "|" + y + "|" + snakeNr + " => AddNewBlock()\n");
        }

        void SendMessage(byte[] data)
        {
            if (multiPC && asServer && server != null)
                server.SendMessage(data);
            if (multiPC && !asServer && client != null)
                client.SendMessage(data);
        }

        public void AddNewBlock(int x, int y, Brush color)
        {
            ChangeColor(x, y, color);
        }

        public void GameOver(bool send)
        {
            running = false;
            lb_gameover.Visibility = Visibility.Visible;
            lb_pause.Visibility = Visibility.Hidden;
            grid_gameover.Visibility = Visibility.Visible;
            grid_options.IsEnabled = true;
            if(multiPC)
                grid_online.IsEnabled = true;
            if(!multiPC || (multiPC && asServer))
                bt_start.IsEnabled = true;
            if (multiPC && asServer && server != null && send)
            {
                server.SendMessage(new byte[] { 1, 2, 0, 0 });
                DebugPrint(/*"Sent: */"1|2|0|0 => GameOver()\n");
            }
            else if (multiPC && !asServer && client != null && send)
            {
                client.SendMessage(new byte[] { 1, 2, 0, 0 });
                DebugPrint(/*"Sent: */"1|2|0|0 => GameOver()\n");
            }
        }

        void UpdateStats(int snakeNr)
        {
            playerstats[snakeNr].X = snake[snakeNr].HeadX;
            playerstats[snakeNr].Y = snake[snakeNr].HeadY;
            playerstats[snakeNr].Points = snake[snakeNr].Points;
            playerstats[snakeNr].Length = snake[snakeNr].Length;
            playerstats[snakeNr].Food = snake[snakeNr].Food;
            playerstats[snakeNr].Direction = snake[snakeNr].Direction;
        }

        async void Prepare()
        {
            if (multiPC && asServer && server != null)
            {
                server.SendMessage(new byte[] { 1, 0, 0, 0 });    //send prepare command
                DebugPrint("Sent: 1|0|0|0 => Prepare()\n");
            }

            running = true;

            NewSnake(3, 3, Directions.Right, 0);

            UpdateStats(0);

            if (players == 2)
            {
                NewSnake(fieldSizeX - 4, fieldSizeY - 4, Directions.Left, 1);
                //playerstats[1].Visibility = Visibility.Visible;
                UpdateStats(1);
            }
            else
            {
                //playerstats[1].Visibility = Visibility.Hidden;
            }

            ResetField();

            if (!multiPC || (multiPC && asServer))
                AddNewFoodBlock(maxFood);

            if (cb_wall.IsChecked == true)
                Wall(true);
            else Wall(false);
            
            grid_gameover.Visibility = Visibility.Hidden;
            grid_options.IsEnabled = false;
            grid_online.IsEnabled = false;
            bt_start.IsEnabled = false;

            await Countdown(3);

            if (!multiPC)
                GameTickSingle();
            else if (multiPC && asServer)
                GameTickMulti(false);
        }

        void ResetField()
        {
            for (int i = 0; i < fieldSizeX; i++)
                for (int j = 0; j < fieldSizeY; j++)
                    ChangeColor(i, j, backgroundColor);
        }

        void NewSnake(int x, int y, Directions direction, int snakeNr)
        {
            snake[snakeNr].HeadX = x;
            snake[snakeNr].HeadY = y;

            snake[snakeNr].Length = 0;
            snake[snakeNr].ChangedLength = 5;
            snake[snakeNr].Direction = direction;
            snake[snakeNr].Points = 0;
            snake[snakeNr].Food = 0;
            snake[snakeNr].CoordsX = new int[fieldSizeX * fieldSizeY];
            snake[snakeNr].CoordsY = new int[fieldSizeX * fieldSizeY];
            //snake[snake_nr].Coords_X[0] = x;
            //snake[snake_nr].Coords_Y[0] = y;
        }

        void Wall(bool set)
        {
            Brush tempColor = set ? wallColor : backgroundColor;
            for (int i = 0; i < fieldSizeX; i++)
                ChangeColor(i, 0, tempColor);
            for (int i = 0; i < fieldSizeY; i++)
                ChangeColor(0, i, tempColor);
            for (int i = 0; i < fieldSizeX; i++)
                ChangeColor(i, fieldSizeY - 1, tempColor);
            for (int i = 0; i < fieldSizeY; i++)
                ChangeColor(fieldSizeX - 1, i, tempColor);
        }

        private void SnakeWindow_Deactivated(object sender, EventArgs e)
        {
            //GamePause();
        }

        struct Snake
        {
            public int[] CoordsX;
            public int[] CoordsY;

            public int Length;
            public int ChangedLength;
            public int HeadX;
            public int HeadY;

            public int Points;
            public int Food;

            public Directions Direction;
            public bool DirChanged;
        }

        private void bt_start_Click(object sender, RoutedEventArgs e)
        {
            Prepare();
        }

        private void bt_online_Click(object sender, RoutedEventArgs e)
        {
            if (rb_server.IsChecked == true)
            {
                server = new Server();
                server.OnConnectionChange += OnConnectionChange;
                server.OnDataReceived += OnDataReceived;

                string HostName = System.Net.Dns.GetHostName();
                string IpAdresse = "localhost";

                foreach (IPAddress address in Dns.GetHostEntry(HostName).AddressList)
                {
                    if (address.AddressFamily != AddressFamily.InterNetworkV6)
                    {
                        IpAdresse = address.ToString();
                        break;
                    }
                }

                Console.WriteLine("Server started(" + 25566 + "|" + HostName + "|" + IpAdresse + ")\n");
                lb_status.Content = "Server gestartet";
                tb_ip.Text = IpAdresse;
            }
            else if (rb_client.IsChecked == true)
            {
                if (tb_ip.Text == "")
                {
                    client = new Client();
                    tb_ip.Text = "localhost";
                }
                else
                    client = new Client(tb_ip.Text);
                client.OnConnectionChange += OnConnectionChange;
                client.OnDataReceived += OnDataReceived;
            }
            rb_client.IsEnabled = false;
            rb_server.IsEnabled = false;
            bt_online.IsEnabled = false;
            bt_end.IsEnabled = true;
            multiPC = true;
        }

        private void bt_end_Click(object sender, RoutedEventArgs e)
        {
            if (asServer)
            {
                if (server != null)
                    server.StopServer();
                tb_ip.Text = "";
            }
            else
            {
                if (client != null)
                    client.StopClient();
            }
            rb_client.IsEnabled = true;
            rb_server.IsEnabled = true;
            bt_end.IsEnabled = false;
            bt_online.IsEnabled = true;
            multiPC = false;
            lb_status.Content = "";
        }

        void DebugPrint(string msg)
        {
            if (debug)
            {
                textBox.AppendText(count + " " + msg);
                count++;
            }
        }

        private void OnDataReceived(object sender, byte[] data)
        {
            Console.WriteLine("Received: " + data[0] + "|" + data[1] + "|" + data[2] + "|" + data[3]);
            string received = /*"Received: " + */data[0] + "|" + data[1] + "|" + data[2] + "|" + data[3] + " => ";
            switch (data[0])
            {
                case 1:
                    switch (data[1])
                    {
                        case 0:
                            DebugPrint(received + "Prepare()\n");
                            Prepare();
                            break;
                        case 1:
                            DebugPrint(received + "GameTick()\n");
                            GameTickMulti(true);
                            break;
                        case 2:
                            DebugPrint(received + "GameOver()\n");
                            GameOver(false);
                            break;
                            //case 3: Pause(); break; <-?
                    }
                    break;

                case 2:
                    DebugPrint(received + "ChangeDirection()\n");
                    ChangeDirection((Directions)data[1], data[2]);
                    GameTickMulti(true);
                    break;

                case 3:
                    DebugPrint(received + "AddNewBlock()\n");
                    AddNewBlock(data[1], data[2], brushes[data[3]]);
                    break;

                default:
                    DebugPrint(received + "FAIL 0xdead\n");
                    break;
            }
        }

        private void OnConnectionChange(object sender, bool isConnected)
        {
            if (isConnected)
            {
                lb_connected.Visibility = Visibility.Visible;
                if (asServer)
                    bt_start.IsEnabled = true;
                rb_multi_local.IsEnabled = false;
                rb_single.IsEnabled = false;
            }
            else
            {
                lb_connected.Visibility = Visibility.Hidden;
                GameOver(false);
                bt_start.IsEnabled = false;
                //multipc = false;
                rb_multi_local.IsEnabled = true;
                rb_single.IsEnabled = true;
            }
        }

        private void SnakeWindow_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void rb_single_Checked(object sender, RoutedEventArgs e)
        {
            players = 1;
            lb_player1.Content = "Spieler 1:";
            lb_player2.Content = "oder:";
            grid_online.IsEnabled = false;
            ps_player_1.IsEnabled = true;
            ps_player_2.IsEnabled = false;
            bt_start.IsEnabled = true;
            cb_wall.IsEnabled = true;
            cb_speed.IsEnabled = true;
            //ps_player_1.ChangeAllowed = false;
            //ps_player_2.ChangeAllowed = false;
            debug = false;
        }

        private void rb_multi_local_Checked(object sender, RoutedEventArgs e)
        {
            players = 2;
            lb_player1.Content = "Spieler 1:";
            lb_player2.Content = "Spieler 2:";
            grid_online.IsEnabled = false;
            ps_player_1.IsEnabled = true;
            ps_player_2.IsEnabled = true;
            bt_start.IsEnabled = true;
            cb_wall.IsEnabled = true;
            cb_speed.IsEnabled = true;
            //ps_player_1.ChangeAllowed = false;
            //ps_player_2.ChangeAllowed = false;
            debug = false;
        }

        private void rb_multi_online_Checked(object sender, RoutedEventArgs e)
        {
            players = 2;
            lb_player1.Content = "Spieler 1:";
            lb_player2.Content = "oder:";
            grid_online.IsEnabled = true;
            ps_player_1.IsEnabled = true;
            ps_player_2.IsEnabled = false;
            rb_server.IsChecked = true;
            bt_start.IsEnabled = false;
            cb_wall.IsChecked = false;
            cb_wall.IsEnabled = false;
            cb_speed.SelectedIndex = 0;
            cb_speed.IsEnabled = false;
            //ps_player_1.ChangeAllowed = false;
            //ps_player_2.ChangeAllowed = false;
            debug = true;
        }

        private void cb_speed_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            switch (cb_speed.SelectedIndex)
            {
                case 0: speed = 200; break;
                case 1: speed = 150; break;
                case 2: speed = 100; break;
                case 3: speed = 50; break;
            }
        }

        private void rb_server_Checked(object sender, RoutedEventArgs e)
        {
            ps_player_1.IsEnabled = true;
            ps_player_2.IsEnabled = false;
            lb_player1.Content = "Spieler 1:";
            bt_online.Content = "Server";
            asServer = true;
            lb_status.Content = "";
        }

        private void rb_client_Checked(object sender, RoutedEventArgs e)
        {
            ps_player_1.IsEnabled = false;
            ps_player_2.IsEnabled = true;
            lb_player1.Content = "Spieler 2:";
            bt_online.Content = "Verbinden";
            asServer = false;
            lb_status.Content = "";
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            count = 0;
            textBox.Text = "";
        }

        private void cb_wall_Checked(object sender, RoutedEventArgs e)
        {
            Wall(true);
        }

        private void cb_wall_Unchecked(object sender, RoutedEventArgs e)
        {
            Wall(false);
        }
    }
}


/*TODO:
 * 
 * WIP - redesign:
 * ? change back/foreground colors
 * ? more block colors
 * ? animations
 * ? remove borders (chess pattern?)
 * ? other color for head (opacity with dark background?)
 * ? change playerstats, move properties to snake struct? change properties to set only?
 * ? dynamic window size
 * ? dynamic field/size (field: change rect. size, fieldsize: change fieldsize)
 * ? window for levels (choose level, show preview
 * 
 * ideas:
 * powerups
 * proper online mp (lobby etc)
 * mp chat
 * pc enemy - automatic search for food[0], maybe nearest, 
 *      difficulty (easy: check next block/ direction, medium: check more blocks (better AI) etc)
 * button remapper + savefile
 * fieldsize chooser
 * up to 4 players, different starting points
 * level for multiplayer
 * save playernames, reset them
 * search lan for servers (number/players, maybe server name)
 * custom levels (+ starting point)
 * 
 * Legend:
 * ~: WIP
 * +: later
 * ?: maybe/idea
 * -: obsolete
*/
/* Changelog:
 * 
 * 2.0.4 (9.11.2016):
 * - Server side fixes
 * - Small ui changes in online
 * 2.0.0 (9.11.2016):
 * - Fixed block removal bug
 * - Changed displayed length to current length, not length+changed_length
 * - UI changes (countdown label now centered, removed green/red wall indicator)
 * 1.9.9 (6.11.2016):
 * - Fixed Online MP Sync via new tick tock method
 * 1.9.8.5 (5.11.2016):
 * - Send dir after tick, only go if dir changed
 * 1.9.8.4 (5.11.2016):
 * - Remove test send before tick
 * - Small ui changes in online
 * 1.9.8.3 (5.11.2016):
 * - Test: Remove nextDirection
 * 1.9.8.2 (5.11.2016):
 * - Test: send current client dir to client before tick
 * 1.9.8.1 (5.11.2016):
 * - Only send changedirection command if direction changed
 * 1.9.8 (5.11.2016):
 * - Changed countdown, now shows first blocks of snake
 * - Added flush to readasync
 * - Added DebugConsole for network traffic
 * - Changed BorderColor to black
 * - Changed starting positions
 * - Changed pause to not work online
 * 1.9.7 (4.11.2016):
 * - Fixed UI bugs in multiplayer/online
 * - Disabled color changing for now
 * 1.9.6 (4.11.2016):
 * - Changed port to 25566
 * - Small code refactorings
 * 1.9.5 (3.11.2016):
 * - Changed network protocol
 * - Changed UpdateStats
 * 1.9.4 (1.11.2016):
 * - Client/Server now properly connect/disconnect
 * 1.9.3 (30.10.2016):
 * - Code cleanup (Go function & others)
 * - Changed network protocol
 * 1.9.2 (29.10.2016):
 * - Changed speed selection to combobox
 * - Removed newlabel, no longer needed
 * 1.9.1 (29.10.2016):
 * - Added direction to playerstats
 * - Removed playername changing
 * 1.9.0 (29.10.2016):
 * - Changed all block colors
 * - Removed dependency of Blocks
 * 1.8.7 (28.10.2016):
 * - Changed WindowStyle to Windows WindowStyle (no mo' statusbar)
 * 1.8.6 (28.10.2016):
 * - Added new control box
 * 1.8.5 (28.10.2016):
 * - Changed position of start button
 * - Removed maximize from statusbar
 * 1.8.4 (28.10.2016):
 * - Changed mode selector
 * 1.8.3 (28.10.2016):
 * - New box designs (playerstats + others)
 * 1.8.2 (28.10.2016):
 * - Removed Info Popup, Information now on startscreen
 * 1.8.1 (28.10.2016):
 * - Simplified AddNewFoodBlock because enum
 * 1.8.0 (27.10.2016):
 * - New Colorpicker
 * - Removed usage of string in favor of enum
 * - Optimizations
 * 1.7.0 (27.10.2016):
 * - Unified Single/Multiplayer Grids
 * 1.6.0:
 * - Complete redesign, with multiple new grids, custom statusbar and more
 */
