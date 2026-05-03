using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text.Json;

namespace cli_life
{
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool IsAliveNext;
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }

    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;

        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }

        public Board(int width, int height, int cellSize, double liveDensity = .1)
        {
            CellSize = cellSize;

            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            Randomize(liveDensity);
        }

        readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }

        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;

                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }

        public void SaveState(string filename)
        {
            using (StreamWriter writer = new StreamWriter(filename))
            {
                for (int y = 0; y < Rows; y++)
                {
                    for (int x = 0; x < Columns; x++)
                    {
                        writer.Write(Cells[x, y].IsAlive ? "1" : "0");
                    }
                    writer.WriteLine();
                }
            }
        }

        public void LoadState(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException($"Файл {filename} не найден");
            }

            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y].IsAlive = false;

            string[] lines = File.ReadAllLines(filename);
            for (int y = 0; y < Math.Min(Rows, lines.Length); y++)
            {
                for (int x = 0; x < Math.Min(Columns, lines[y].Length); x++)
                {
                    Cells[x, y].IsAlive = lines[y][x] == '1';
                }
            }
        }

        public int CountAlive()
        {
            int count = 0;
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    if (Cells[x, y].IsAlive) count++;
            return count;
        }

        public void Clear()
        {
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y].IsAlive = false;
        }


        public List<List<(int x, int y)>> FindClusters()
        {
            var visited = new bool[Columns, Rows];
            var clusters = new List<List<(int x, int y)>>();

            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    if (Cells[x, y].IsAlive && !visited[x, y])
                    {
                        var cluster = new List<(int x, int y)>();
                        var queue = new Queue<(int x, int y)>();
                        queue.Enqueue((x, y));
                        visited[x, y] = true;

                        while (queue.Count > 0)
                        {
                            var (cx, cy) = queue.Dequeue();
                            cluster.Add((cx, cy));

                            for (int dx = -1; dx <= 1; dx++)
                            {
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    if (dx == 0 && dy == 0) continue;
                                    int nx = cx + dx;
                                    int ny = cy + dy;
                                    if (nx >= 0 && nx < Columns && ny >= 0 && ny < Rows &&
                                        Cells[nx, ny].IsAlive && !visited[nx, ny])
                                    {
                                        visited[nx, ny] = true;
                                        queue.Enqueue((nx, ny));
                                    }
                                }
                            }
                        }
                        clusters.Add(cluster);
                    }
                }
            }
            return clusters;
        }
    }

    public class GameSettings
    {
        public int Width { get; set; } = 80;
        public int Height { get; set; } = 30;
        public int CellSize { get; set; } = 1;
        public double LiveDensity { get; set; } = 0.3;
        public int SleepMs { get; set; } = 200;
    }

    class Program
    {
        static Board board;
        static GameSettings settings;
        static int generation = 0;
        static bool autoMode = true;

        static void LoadSettings(string filename = "settings.json")
        {
            if (File.Exists(filename))
            {
                string json = File.ReadAllText(filename);
                settings = JsonSerializer.Deserialize<GameSettings>(json);
                Console.WriteLine("Настройки загружены из settings.json");
            }
            else
            {
                settings = new GameSettings();
                SaveSettings(filename);
                Console.WriteLine("Создан файл настроек settings.json");
            }
        }

        static void SaveSettings(string filename = "settings.json")
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filename, json);
        }

        static void Reset()
        {
            board = new Board(
                width: settings.Width,
                height: settings.Height,
                cellSize: settings.CellSize,
                liveDensity: settings.LiveDensity);
            generation = 0;
        }

        static void Render()
        {
            Console.Clear();
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                {
                    var cell = board.Cells[col, row];
                    Console.Write(cell.IsAlive ? '█' : ' ');
                }
                Console.WriteLine();
            }
            Console.WriteLine($"\nПоколение: {generation} | Живых клеток: {board.CountAlive()}");
            Console.WriteLine("\nКоманды: S-сохранить | L-загрузить | R-сброс | Пробел-пауза | C-классификация | T-стабилизация | E-эксперимент | ESC-выход");
        }


        static List<(int x, int y)> NormalizeCluster(List<(int x, int y)> cluster)
        {
            int minX = cluster.Min(p => p.x);
            int minY = cluster.Min(p => p.y);
            var normalized = new List<(int x, int y)>();
            foreach (var p in cluster)
            {
                normalized.Add((p.x - minX, p.y - minY));
            }
            return normalized.OrderBy(p => p.y).ThenBy(p => p.x).ToList();
        }


        static string ClassifyPattern(List<(int x, int y)> pattern)
        {
            if (pattern.Count == 4)
            {

                if (pattern.Contains((0, 0)) && pattern.Contains((1, 0)) &&
                    pattern.Contains((0, 1)) && pattern.Contains((1, 1)))
                    return "Block";
            }
            else if (pattern.Count == 6)
            {

                if (pattern.Contains((1, 0)) && pattern.Contains((2, 0)) &&
                    pattern.Contains((0, 1)) && pattern.Contains((3, 1)) &&
                    pattern.Contains((1, 2)) && pattern.Contains((2, 2)))
                    return "Beehive";


            }
            else if (pattern.Count == 5)
            {

                if (pattern.Contains((0, 0)) && pattern.Contains((2, 0)) &&
                    pattern.Contains((1, 1)) &&
                    pattern.Contains((0, 2)) && pattern.Contains((1, 2)))
                    return "Boat";


                if (pattern.Contains((1, 0)) && pattern.Contains((2, 1)) &&
                    pattern.Contains((0, 2)) && pattern.Contains((1, 2)) && pattern.Contains((2, 2)))
                    return "Glider";
            }
            else if (pattern.Count == 3)
            {

                if (pattern.Contains((0, 0)) && pattern.Contains((1, 0)) && pattern.Contains((2, 0)))
                    return "Blinker";
            }
            else if (pattern.Count == 7)
            {

                if (pattern.Contains((1, 0)) && pattern.Contains((2, 0)) &&
                    pattern.Contains((0, 1)) && pattern.Contains((3, 1)) &&
                    pattern.Contains((1, 2)) && pattern.Contains((3, 2)) &&
                    pattern.Contains((2, 3)))
                    return "Loaf";
            }

            return "Unknown";
        }


        static void ClassifyAndReport()
        {
            var clusters = board.FindClusters();
            Console.Clear();
            Console.WriteLine("=== АНАЛИЗ КОЛОНИИ ===");
            Console.WriteLine($"Всего живых клеток: {board.CountAlive()}");
            Console.WriteLine($"Всего кластеров (комбинаций): {clusters.Count}");

            var stats = new Dictionary<string, int>();
            stats["Block"] = 0;
            stats["Beehive"] = 0;
            stats["Boat"] = 0;
            stats["Loaf"] = 0;
            stats["Blinker"] = 0;
            stats["Glider"] = 0;
            stats["Unknown"] = 0;

            foreach (var cluster in clusters)
            {
                var normalized = NormalizeCluster(cluster);
                string type = ClassifyPattern(normalized);
                stats[type]++;
            }

            Console.WriteLine("\n=== КЛАССИФИКАЦИЯ ФИГУР ===");
            Console.WriteLine($"Устойчивые фигуры:");
            Console.WriteLine($"  Block (квадрат 2×2): {stats["Block"]}");
            Console.WriteLine($"  Beehive (улей): {stats["Beehive"]}");
            Console.WriteLine($"  Boat (лодка): {stats["Boat"]}");
            Console.WriteLine($"  Loaf (буханка): {stats["Loaf"]}");

            Console.WriteLine($"\nПериодические фигуры:");
            Console.WriteLine($"  Blinker (мигалка): {stats["Blinker"]}");

            Console.WriteLine($"\nДвигающиеся фигуры:");
            Console.WriteLine($"  Glider (планер): {stats["Glider"]}");

            Console.WriteLine($"\nНеизвестные фигуры: {stats["Unknown"]}");

            Console.WriteLine("\nНажмите любую клавишу для продолжения...");
            Console.ReadKey(true);
        }


        static int FindStabilizationTime(int maxGenerations = 2000, int stableThreshold = 10)
        {
            var aliveHistory = new List<int>();
            int stableGenerations = 0;
            int lastAliveCount = -1;

            for (int gen = 0; gen < maxGenerations; gen++)
            {
                int aliveCount = board.CountAlive();
                aliveHistory.Add(aliveCount);

                if (aliveCount == lastAliveCount)
                {
                    stableGenerations++;
                    if (stableGenerations >= stableThreshold)
                    {
                        return gen - stableThreshold + 1;
                    }
                }
                else
                {
                    stableGenerations = 0;
                }

                lastAliveCount = aliveCount;
                board.Advance();
            }

            return -1;
        }


        static void RunExperiment()
        {
            Console.Clear();
            Console.WriteLine("=== ЭКСПЕРИМЕНТ ПО СТАБИЛИЗАЦИИ ===");
            Console.WriteLine("Исследуется зависимость времени стабилизации от плотности заполнения\n");

            double[] densities = { 0.05, 0.1, 0.15, 0.2, 0.25, 0.3, 0.35, 0.4, 0.45, 0.5 };
            int attemptsPerDensity = 5;


            string dataDir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
            Console.WriteLine($"ПАПКА ДЛЯ ДАННЫХ: {Path.GetFullPath(dataDir)}");
            Console.ReadKey();
            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);

            using (StreamWriter sw = new StreamWriter(Path.Combine(dataDir, "data.txt")))
            {
                sw.WriteLine("Density\tAttempt\tStabilizationTime");

                foreach (double density in densities)
                {
                    Console.WriteLine($"\nТестирование плотности {density * 100}%...");
                    List<int> times = new List<int>();

                    for (int attempt = 1; attempt <= attemptsPerDensity; attempt++)
                    {
                        settings.LiveDensity = density;
                        Reset();

                        Console.Write($"  Попытка {attempt}/{attemptsPerDensity}...");
                        int stabTime = FindStabilizationTime();
                        times.Add(stabTime);
                        sw.WriteLine($"{density:F3}\t{attempt}\t{stabTime}");
                        Console.WriteLine($" стабилизация на {stabTime} поколении");
                    }

                    double avgTime = times.Where(t => t > 0).Average();
                    Console.WriteLine($"  СРЕДНЕЕ: {avgTime:F1} поколений");
                }
            }

            using (StreamWriter sw = new StreamWriter(Path.Combine(dataDir, "data_avg.txt")))
            {
                sw.WriteLine("Density\tAvgStabilizationTime");

                for (int i = 0; i < densities.Length; i++)
                {
                    double density = densities[i];
                    settings.LiveDensity = density;

                    List<int> times = new List<int>();
                    for (int attempt = 1; attempt <= attemptsPerDensity; attempt++)
                    {
                        Reset();
                        int stabTime = FindStabilizationTime();
                        if (stabTime > 0) times.Add(stabTime);
                    }

                    double avgTime = times.Count > 0 ? times.Average() : -1;
                    sw.WriteLine($"{density:F3}\t{avgTime:F1}");
                }
            }

            Console.WriteLine("\n\nЭксперимент завершён!");
            Console.WriteLine($"Данные сохранены в папку: {Path.GetFullPath(dataDir)}");
            Console.WriteLine("\nФайлы:");
            Console.WriteLine("  - data.txt (все попытки)");
            Console.WriteLine("  - data_avg.txt (средние значения для графика)");
            Console.WriteLine("\nНажмите любую клавишу для продолжения...");
            Console.ReadKey(true);
        }

        static void ShowLoadMenu()
        {
            Console.Clear();
            Console.WriteLine("=== ЗАГРУЗКА ФИГУРЫ ===");
            Console.WriteLine("1 - Glider (планер) - двигающаяся фигура");
            Console.WriteLine("2 - Blinker (мигалка) - периодическая фигура");
            Console.WriteLine("3 - Block (блок) - устойчивая фигура");
            Console.WriteLine("4 - Beehive (улей) - устойчивая фигура");
            Console.WriteLine("5 - Ввести имя файла вручную");
            Console.WriteLine("0 - Отмена");
            Console.Write("\nВыберите фигуру (0-5): ");
        }

        static void CreateFiguresFolder()
        {
            string figuresPath = "Figures";
            if (!Directory.Exists(figuresPath))
            {
                Directory.CreateDirectory(figuresPath);

                File.WriteAllText(Path.Combine(figuresPath, "glider.txt"),
                    "010\n001\n111");

                File.WriteAllText(Path.Combine(figuresPath, "blinker.txt"),
                    "111");

                File.WriteAllText(Path.Combine(figuresPath, "block.txt"),
                    "11\n11");

                File.WriteAllText(Path.Combine(figuresPath, "beehive.txt"),
                    "0110\n1001\n0110");

                File.WriteAllText(Path.Combine(figuresPath, "boat.txt"),
                    "010\n101\n010");

                Console.WriteLine("Создана папка Figures с файлами фигур");
            }
        }

        static void Main(string[] args)
        {
            LoadSettings();
            Reset();
            CreateFiguresFolder();

            Console.CursorVisible = false;

            while (true)
            {
                Render();

                if (autoMode)
                {
                    board.Advance();
                    generation++;
                    Thread.Sleep(settings.SleepMs);
                }

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.S:
                            autoMode = false;
                            string savesDir = "Saves";
                            if (!Directory.Exists(savesDir))
                                Directory.CreateDirectory(savesDir);

                            Console.WriteLine("\nВведите имя файла для сохранения:");
                            string saveFile = Console.ReadLine();
                            if (!string.IsNullOrEmpty(saveFile))
                            {
                                string fullPath = Path.Combine(savesDir, saveFile + ".txt");
                                board.SaveState(fullPath);
                                Console.WriteLine($"Сохранено в: {fullPath}");
                            }
                            Thread.Sleep(1000);
                            Console.Clear();
                            Render();
                            autoMode = true;
                            break;

                        case ConsoleKey.L:
                            autoMode = false;
                            ShowLoadMenu();

                            var choice = Console.ReadKey(true).KeyChar;
                            string fileName = "";

                            switch (choice)
                            {
                                case '1': fileName = "glider.txt"; break;
                                case '2': fileName = "blinker.txt"; break;
                                case '3': fileName = "block.txt"; break;
                                case '4': fileName = "beehive.txt"; break;
                                case '5':
                                    Console.Clear();
                                    Console.Write("Введите имя файла: ");
                                    fileName = Console.ReadLine();
                                    break;
                                case '0':
                                    Console.Clear();
                                    Render();
                                    autoMode = true;
                                    continue;
                                default:
                                    Console.WriteLine("\nНеверный выбор!");
                                    Thread.Sleep(1000);
                                    Console.Clear();
                                    Render();
                                    autoMode = true;
                                    continue;
                            }

                            if (!string.IsNullOrEmpty(fileName))
                            {
                                string fullPath = Path.Combine("Figures", fileName);

                                if (File.Exists(fullPath))
                                {
                                    try
                                    {
                                        board.LoadState(fullPath);
                                        generation = 0;
                                        Console.Clear();
                                        Render();
                                        Console.WriteLine($"\nЗагружено: {fileName}");
                                        Console.WriteLine("Нажмите любую клавишу для продолжения...");
                                        Console.ReadKey(true);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.Clear();
                                        Console.WriteLine($"Ошибка загрузки: {ex.Message}");
                                        Console.WriteLine("Нажмите любую клавишу...");
                                        Console.ReadKey(true);
                                    }
                                }
                                else
                                {
                                    Console.Clear();
                                    Console.WriteLine($"Файл {fullPath} не найден!");
                                    Console.WriteLine("Нажмите любую клавишу...");
                                    Console.ReadKey(true);
                                }
                            }

                            Console.Clear();
                            Render();
                            autoMode = true;
                            break;

                        case ConsoleKey.R:
                            Reset();
                            Console.WriteLine("Поле сброшено (случайное заполнение)");
                            Thread.Sleep(500);
                            break;

                        case ConsoleKey.C:
                            autoMode = false;
                            ClassifyAndReport();
                            Console.Clear();
                            Render();
                            autoMode = true;
                            break;

                        case ConsoleKey.T:
                            autoMode = false;
                            Console.Clear();
                            Console.WriteLine("Поиск времени стабилизации...");
                            Console.WriteLine("(это может занять некоторое время)");
                            int stabTime = FindStabilizationTime();
                            Console.Clear();
                            Console.WriteLine($"Время стабилизации: {stabTime} поколений");
                            if (stabTime == -1)
                                Console.WriteLine("Поле не стабилизировалось за отведённое время");
                            Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                            Console.ReadKey(true);
                            Console.Clear();
                            Render();
                            autoMode = true;
                            break;

                        case ConsoleKey.E:
                            autoMode = false;
                            RunExperiment();
                            Console.Clear();
                            Render();
                            autoMode = true;
                            break;

                        case ConsoleKey.Spacebar:
                            autoMode = !autoMode;
                            Console.WriteLine(autoMode ? "Автоматический режим" : "Пауза");
                            Thread.Sleep(500);
                            break;

                        case ConsoleKey.Escape:
                            return;
                    }
                }
            }
        }
    }
}