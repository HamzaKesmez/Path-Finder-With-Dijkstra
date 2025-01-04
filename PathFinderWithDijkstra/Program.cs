using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Timer = System.Windows.Forms.Timer;

namespace PathFinderWithDijkstra
{
    public partial class MainForm : Form
    {
        private int[,] maze;
        private const int cellSize = 30;
        private const int initialEnergy = 50;
        private int currentEnergy;
        private Point currentPosition;
        private Point exitPoint;
        private List<Point> path;
        private Timer gameTimer;
        private HashSet<Point> visitedCells;
        private Stack<Point> pathStack;
        private Random random;
        private HashSet<string> triedPaths;
        private int attemptCount;

        public MainForm()
        {
            InitializeComponent();
            LoadMaze();
            random = new Random();
            triedPaths = new HashSet<string>();
            attemptCount = 0;
            FindExitPoint();
            InitializeGame();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form ayarları

            this.ClientSize = new Size(800, 900);
            this.Text = "Pikachu Labirent Oyunu";
            this.DoubleBuffered = true;
            this.Paint += MainForm_Paint;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            // Oyun zamanlayıcısı
            gameTimer = new Timer();
            gameTimer.Interval = 200; // 100ms hareket hızı
            gameTimer.Tick += GameTimer_Tick;

            this.ResumeLayout(false);
        }
        private List<Point> FindShortestPathWithDijkstra()
        {
            int rows = maze.GetLength(0);
            int cols = maze.GetLength(1);

            var distances = new Dictionary<Point, int>();
            var previousNodes = new Dictionary<Point, Point?>();
            var unvisitedNodes = new HashSet<Point>();

 
            Point start = currentPosition;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    Point point = new Point(j, i);

                    if (maze[i, j] != 0) 
                    {
                        distances[point] = int.MaxValue;
                        previousNodes[point] = null;
                        unvisitedNodes.Add(point);
                    }
                }
            }

            distances[start] = 0;

            while (unvisitedNodes.Count > 0)
            {

                Point current = unvisitedNodes.OrderBy(node => distances[node]).First();
                unvisitedNodes.Remove(current);

                if (current == exitPoint)
                    break;

            
                foreach (var neighbor in GetNeighbors(current))
                {
                    if (!unvisitedNodes.Contains(neighbor)) continue;

                    int newDist = distances[current] + 1; 

                    if (maze[neighbor.Y, neighbor.X] == 4)
                        newDist -= 5;

                    if (maze[neighbor.Y, neighbor.X] == 5)
                        newDist += 10;

                    if (newDist < distances[neighbor])
                    {
                        distances[neighbor] = newDist;
                        previousNodes[neighbor] = current;
                    }
                }
            }

            var path = new List<Point>();
            for (Point? at = exitPoint; at != null; at = previousNodes[at.Value])
            {
                path.Add(at.Value);
            }

            path.Reverse();
            return path;
        }

        private IEnumerable<Point> GetNeighbors(Point pos)
        {
            List<Point> directions = new List<Point>
    {
        new Point(0, -1), 
        new Point(1, 0),  
        new Point(0, 1),  
        new Point(-1, 0)  
    };

            foreach (var dir in directions)
            {
                Point neighbor = new Point(pos.X + dir.X, pos.Y + dir.Y);

                if (IsValidMove(neighbor))
                    yield return neighbor;
            }
        }

        private void LoadMaze()
        {
            try
            {
                string[] lines = File.ReadAllLines("Labirent1.txt");
                int rows = lines.Length;
                int cols = lines[0].Split(',').Length;
                maze = new int[rows, cols];

                for (int i = 0; i < rows; i++)
                {
                    string[] values = lines[i].Split(',');
                    for (int j = 0; j < cols; j++)
                    {
                        maze[i, j] = int.Parse(values[j]);
                        if (maze[i, j] == 2)
                        {
                            currentPosition = new Point(j, i);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Labirent dosyası yüklenirken hata oluştu: {ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private void FindExitPoint()
        {
            for (int i = 0; i < maze.GetLength(0); i++)
            {
                for (int j = 0; j < maze.GetLength(1); j++)
                {
                    if (maze[i, j] == 3)
                    {
                        exitPoint = new Point(j, i);
                        return;
                    }
                }
            }
        }

        private void InitializeGame()
        {
            currentEnergy = initialEnergy;
            path = new List<Point>();
            visitedCells = new HashSet<Point>();
            pathStack = new Stack<Point>();
            attemptCount++;

            path.Add(currentPosition);
            pathStack.Push(currentPosition);
            visitedCells.Add(currentPosition);

            gameTimer.Start();
        }


        private double GetDistanceToExit(Point pos)
        {
            return Math.Sqrt(Math.Pow(pos.X - exitPoint.X, 2) + Math.Pow(pos.Y - exitPoint.Y, 2));
        }


        private bool IsValidMove(Point pos)
        {
            if (pos.X < 0 || pos.Y < 0 ||
                pos.X >= maze.GetLength(1) ||
                pos.Y >= maze.GetLength(0))
                return false;

            return maze[pos.Y, pos.X] != 0; 
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            if (currentEnergy <= 0)
            {
                RestartGame();
                return;
            }

            if (currentPosition == exitPoint)
            {
                gameTimer.Stop();
                MessageBox.Show($"Tebrikler! Çıkışa ulaştınız!\nKalan enerji: {currentEnergy}\nDeneme sayısı: {attemptCount}");
                RestartGame();
                return;
            }

         
            List<Point> shortestPath = FindShortestPathWithDijkstra();
            if (shortestPath.Count > 1)
            {
                Point nextPosition = shortestPath[1];
                MoveToPosition(nextPosition);
            }
            else
            {
                RestartGame();
            }

            Invalidate();
        }

        private void MoveToPosition(Point newPos)
        {
            string hareketYon = "";

            if (newPos.X > currentPosition.X) hareketYon = "Sağ";
            else if (newPos.X < currentPosition.X) hareketYon = "Sol";
            else if (newPos.Y > currentPosition.Y) hareketYon = "Aşağı";
            else if (newPos.Y < currentPosition.Y) hareketYon = "Yukarı";

            currentEnergy--;

            switch (maze[newPos.Y, newPos.X])
            {
                case 4:
                    currentEnergy += 15;
                    break;
                case 5:
                    currentEnergy -= 10;
                    break;
            }

            currentPosition = newPos;
            path.Add(newPos);

            
            cikisYolu(hareketYon);
        }

        private void RestartGame()
        {
            string filePath = "Sonuc.txt";
            if (currentEnergy > 0)
            {
                using (StreamWriter sw = new StreamWriter(filePath, true))
                {
                    sw.WriteLine($"Tebrikler Kazandınız Oyun bitti. Kalan enerji: {currentEnergy}, Toplam deneme: {attemptCount}");
                    sw.WriteLine("=====================================");
                }
            }
            else
            {
                using (StreamWriter sw = new StreamWriter(filePath, true))
                {
                    sw.WriteLine($" Kaybettiniz Oyun bitti. Toplam deneme: {attemptCount}");
                    sw.WriteLine("=====================================");
                }
            }


            for (int i = 0; i < maze.GetLength(0); i++)
            {
                for (int j = 0; j < maze.GetLength(1); j++)
                {
                    if (maze[i, j] == 2)
                    {
                        currentPosition = new Point(j, i);
                        break;
                    }
                }
            }

            visitedCells.Clear();
            path.Clear();
            pathStack.Clear();
            InitializeGame();
        }
        private void cikisYolu(string hareketYon)
        {
            string filePath = "Sonuc.txt";



            using (StreamWriter sw = new StreamWriter(filePath, true))
            {
                sw.WriteLine($"Pozisyon: ({currentPosition.X},{currentPosition.Y}), Enerji: {currentEnergy}, Hareket: {hareketYon}");
            }
        }
        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            for (int i = 0; i < maze.GetLength(0); i++)
            {
                for (int j = 0; j < maze.GetLength(1); j++)
                {
                    Rectangle cell = new Rectangle(j * cellSize, i * cellSize, cellSize, cellSize);

                    switch (maze[i, j])
                    {
                        case 0:
                            g.FillRectangle(Brushes.Gray, cell);
                            break;
                        case 1:
                            g.FillRectangle(Brushes.White, cell);
                            break;
                        case 2:
                            g.FillRectangle(Brushes.Green, cell);
                            break;
                        case 3:
                            g.FillRectangle(Brushes.Red, cell);
                            break;
                        case 4:
                            g.FillRectangle(Brushes.Yellow, cell);
                            break;
                        case 5:
                            g.FillRectangle(Brushes.Purple, cell);
                            break;
                    }
                    g.DrawRectangle(Pens.Black, cell);
                }
            }

            if (path.Count > 1)
            {
                using (Pen pathPen = new Pen(Color.FromArgb(100, Color.Blue), 2))
                {
                    for (int i = 1; i < path.Count; i++)
                    {
                        Point p1 = new Point(
                            path[i - 1].X * cellSize + cellSize / 2,
                            path[i - 1].Y * cellSize + cellSize / 2
                        );
                        Point p2 = new Point(
                            path[i].X * cellSize + cellSize / 2,
                            path[i].Y * cellSize + cellSize / 2
                        );
                        g.DrawLine(pathPen, p1, p2);
                    }
                }
            }


            g.FillEllipse(Brushes.Orange,
                currentPosition.X * cellSize + 5,
                currentPosition.Y * cellSize + 5,
                cellSize - 10, cellSize - 10);


            Rectangle energyBar = new Rectangle(10, maze.GetLength(0) * cellSize + 10, 200, 20);
            g.DrawRectangle(Pens.Black, energyBar);
            g.FillRectangle(Brushes.Green,
                energyBar.X, energyBar.Y,
                (int)(energyBar.Width * (currentEnergy / (float)initialEnergy)),
                energyBar.Height);


            g.DrawString($"Enerji: {currentEnergy} | Deneme: {attemptCount}",
                SystemFonts.DefaultFont,
                Brushes.Black,
                220, maze.GetLength(0) * cellSize + 10);
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}