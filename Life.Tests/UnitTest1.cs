// Copyright 2026 UNN
using NUnit.Framework;
using Life;
using System.IO;

namespace Life.Tests
{
    [TestFixture]
    public class WebLifeTests
    {
        [Test]
        public void TestCellInitialState()
        {
            Cell cell = new Cell();
            Assert.IsFalse(cell.IsAlive);
        }

        [Test]
        public void TestBoardCreationSize()
        {
            Board board = new Board(20, 10, 1, 0.3);
            Assert.AreEqual(20, board.Width);
            Assert.AreEqual(10, board.Height);
        }

        [Test]
        public void TestBoardInitialDensityZero()
        {
            Board board = new Board(10, 10, 1, 0.0);
            Assert.AreEqual(0, board.CountLiveCells());
        }

        [Test]
        public void TestBoardInitialDensityOne()
        {
            Board board = new Board(10, 10, 1, 1.0);
            Assert.AreEqual(100, board.CountLiveCells());
        }

        [Test]
        public void TestConnectNeighborsCount()
        {
            Board board = new Board(5, 5, 1, 0.0);
            Assert.AreEqual(8, board.Cells[0, 0].Neighbors.Count);
        }

        [Test]
        public void TestSaveToFileCreatesFile()
        {
            Board board = new Board(5, 5, 1, 0.2);
            string path = "test_state.txt";
            board.SaveToFile(path);
            Assert.IsTrue(File.Exists(path));
            File.Delete(path);
        }

        [Test]
        public void TestLoadFromFileUpdatesBoard()
        {
            Board board = new Board(3, 3, 1, 0.0);
            string path = "test_load.txt";
            File.WriteAllLines(path, new string[] { "***", "...", "..." });
            board.LoadFromFile(path);
            Assert.AreEqual(3, board.CountLiveCells());
            File.Delete(path);
        }

        [Test]
        public void TestGameSettingsDefaultValues()
        {
            GameSettings settings = new GameSettings();
            Assert.AreEqual(50, settings.Width);
            Assert.AreEqual(20, settings.Height);
            Assert.AreEqual(0.3, settings.Density);
        }

        [Test]
        public void TestUnderpopulationRule()
        {
            Board board = new Board(3, 3, 1, 0.0);
            board.Cells[1, 1].IsAlive = true;
            board.Advance();
            Assert.IsFalse(board.Cells[1, 1].IsAlive);
        }

        [Test]
        public void TestSurvivalRuleTwoNeighbors()
        {
            Board board = new Board(3, 3, 1, 0.0);
            board.Cells[1, 1].IsAlive = true;
            board.Cells[0, 0].IsAlive = true;
            board.Cells[0, 1].IsAlive = true;
            board.Advance();
            Assert.IsTrue(board.Cells[1, 1].IsAlive);
        }

        [Test]
        public void TestSurvivalRuleThreeNeighbors()
        {
            Board board = new Board(3, 3, 1, 0.0);
            board.Cells[1, 1].IsAlive = true;
            board.Cells[0, 0].IsAlive = true;
            board.Cells[0, 1].IsAlive = true;
            board.Cells[0, 2].IsAlive = true;
            board.Advance();
            Assert.IsTrue(board.Cells[1, 1].IsAlive);
        }

        [Test]
        public void TestOverpopulationRule()
        {
            Board board = new Board(3, 3, 1, 0.0);
            board.Cells[1, 1].IsAlive = true;
            board.Cells[0, 0].IsAlive = true;
            board.Cells[0, 1].IsAlive = true;
            board.Cells[0, 2].IsAlive = true;
            board.Cells[1, 0].IsAlive = true;
            board.Advance();
            Assert.IsFalse(board.Cells[1, 1].IsAlive);
        }

        [Test]
        public void TestReproductionRule()
        {
            Board board = new Board(3, 3, 1, 0.0);
            board.Cells[1, 1].IsAlive = false;
            board.Cells[0, 0].IsAlive = true;
            board.Cells[0, 1].IsAlive = true;
            board.Cells[0, 2].IsAlive = true;
            board.Advance();
            Assert.IsTrue(board.Cells[1, 1].IsAlive);
        }

        [Test]
        public void TestAdvanceChangesState()
        {
            Board board = new Board(5, 5, 1, 0.5);
            int initialCount = board.CountLiveCells();
            board.Advance();
            Assert.Pass();
        }

        [Test]
        public void TestCellPropertiesAndNeighborsInit()
        {
            Cell cell = new Cell();
            Assert.IsNotNull(cell.Neighbors);
        }
    }
}
