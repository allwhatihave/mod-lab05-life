// Copyright 2026 UNN
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Life
{
    public class Cell
    {
        public bool IsAlive { get; set; }
        public List<Cell> Neighbors { get; set; } = new List<Cell>();
    }

    public class Board
    {
        public int Width { get; }
        public int Height { get; }
        public Cell[,] Cells { get; }
        public double LiveDensity { get; }

        public Board(int width, int height, int cellSize, double density)
        {
            Width = width;
            Height = height;
            LiveDensity = density;
            Cells = new Cell[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Cells[x, y] = new Cell();
                }
            }

            ConnectNeighbors();
            Randomize(density);
        }

        private void ConnectNeighbors()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int xl = -1; xl <= 1; xl++)
                    {
                        for (int yl = -1; yl <= 1; yl++)
                        {
                            if (xl == 0 && yl == 0) continue;

                            int xn = (x + xl + Width) % Width;
                            int yn = (y + yl + Height) % Height;

                            Cells[x, y].Neighbors.Add(Cells[xn, yn]);
                        }
                    }
                }
            }
        }

        public void Randomize(double density)
        {
            Random rand = new Random();
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Cells[x, y].IsAlive = rand.NextDouble() < density;
                }
            }
        }

        public void Advance()
        {
            bool[,] nextStates = new bool[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int liveNeighbors = Cells[x, y].Neighbors.Count(n => n.IsAlive);
                    if (Cells[x, y].IsAlive)
                    {
                        nextStates[x, y] = liveNeighbors == 2 || liveNeighbors == 3;
                    } else {
                        nextStates[x, y] = liveNeighbors == 3;
                    }
                }
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Cells[x, y].IsAlive = nextStates[x, y];
                }
            }
        }

        public int CountLiveCells()
        {
            int count = 0;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Cells[x, y].IsAlive) count++;
                }
            }
            return count;
        }

        public void SaveToFile(string path)
        {
            List<string> lines = new List<string>();
            for (int y = 0; y < Height; y++)
            {
                string line = "";
                for (int x = 0; x < Width; x++)
                {
                    line += Cells[x, y].IsAlive ? '*' : '.';
                }
                lines.Add(line);
            }
            File.WriteAllLines(path, lines);
        }

        public void LoadFromFile(string path)
        {
            string[] lines = File.ReadAllLines(path);
            for (int y = 0; y < Math.Min(Height, lines.Length); y++)
            {
                for (int x = 0; x < Math.Min(Width, lines[y].Length); x++)
                {
                    Cells[x, y].IsAlive = lines[y][x] == '*';
                }
            }
        }
    }

    public class GameSettings
    {
        public int Width { get; set; } = 50;
        public int Height { get; set; } = 20;
        public double Density { get; set; } = 0.3;
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            GameSettings settings = new GameSettings();
            string configPath = "config.json";

            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    settings = JsonSerializer.Deserialize<GameSettings>(json) ?? new GameSettings();
                }
                catch
                {
                    settings = new GameSettings();
                }
            } else {
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
            }

            Board board = new Board(settings.Width, settings.Height, 1, settings.Density);

            if (args.Length > 0 && File.Exists(args[0]))
            {
                board.LoadFromFile(args[0]);
            }

            for (int i = 0; i < 10; i++)
            {
                board.Advance();
            }

            Directory.CreateDirectory("Data");
            board.SaveToFile("Data/state.txt");
        }
    }
}
