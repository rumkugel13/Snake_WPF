using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static SnakeWPF.MainWindow;

namespace SnakeWPF
{
    /// <summary>
    /// Interaktionslogik für PlayerStats.xaml
    /// </summary>
    public partial class PlayerStats : UserControl
    {
        public PlayerStats()
        {
            InitializeComponent();
        }

        public bool ChangeAllowed { get; set; }

        public string Header
        {
            get { return (string)lb_header.Content; }
            set { lb_header.Content = value; }
        }

        public Brush BlockColor
        {
            get { return lb_header.Background; }
            set {
                //Color myColor = ((SolidColorBrush)value).Color;
                //myColor.A = (byte)(255 * 0.9); // 255 * 0.9 is approx. 230
                //rec1.Fill = new SolidColorBrush(myColor);
                //rec1.Stroke = value;
                lb_header.Background = value;
            }
        }

        public int X
        {
            get { return Convert.ToInt32(lb_x.Content); }
            set { lb_x.Content = value; }
        }

        public int Y
        {
            get { return Convert.ToInt32(lb_y.Content); }
            set { lb_y.Content = value; }
        }

        public int Points
        {
            get { return Convert.ToInt32(lb_points.Content); }
            set { lb_points.Content = value; }
        }

        public int Food
        {
            get { return Convert.ToInt32(lb_food.Content); }
            set { lb_food.Content = value; }
        }
        
        public int Length
        {
            get { return Convert.ToInt32(lb_length.Content); }
            set { lb_length.Content = value; }
        }

        public Directions Direction
        {
            get
            {
                switch(lb_direction.Content.ToString())
                {
                    case "Links": return Directions.Left;
                    case "Rechts": return Directions.Right;
                    case "Oben": return Directions.Up;
                    case "Unten": return Directions.Down;
                    default: Direction = Directions.Left; return Directions.Left;
                }
            }
            set
            {
                switch(value)
                {
                    case Directions.Left: lb_direction.Content = "Links"; break;
                    case Directions.Right: lb_direction.Content = "Rechts"; break;
                    case Directions.Up: lb_direction.Content = "Oben"; break;
                    case Directions.Down: lb_direction.Content = "Unten"; break;
                }
            }
        }

        private void ColorBlocks_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            lb_header.Background = ((Rectangle)sender).Stroke;
            grid_color.Visibility = Visibility.Hidden;
        }

        private void lb_header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ChangeAllowed)
            {
                grid_color.Visibility = Visibility.Visible;
            }
        }
    }
}
