using Microsoft.VisualStudio.TestTools.UnitTesting;
using cli_life; // Пространство имен твоей основной программы
using System.IO;

namespace Life.Tests
{
    [TestClass]
    public class GameOfLifeTests
    {
        // === ТЕСТЫ ПРАВИЛ ДЛЯ КЛЕТОК (CELL) ===

        [TestMethod]
        public void Cell_DeadWith3Neighbors_BecomesAlive()
        {
            var cell = new Cell { IsAlive = false };
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.neighbors.Add(new Cell { IsAlive = true });

            cell.DetermineNextLiveState();
            cell.Advance();

            Assert.IsTrue(cell.IsAlive, "Мертвая клетка с 3 соседями должна ожить.");
        }

        [TestMethod]
        public void Cell_DeadWith2Neighbors_StaysDead()
        {
            var cell = new Cell { IsAlive = false };
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.neighbors.Add(new Cell { IsAlive = true });

            cell.DetermineNextLiveState();
            cell.Advance();

            Assert.IsFalse(cell.IsAlive, "Мертвая клетка с 2 соседями должна оставаться мертвой.");
        }

        [TestMethod]
        public void Cell_AliveWith2Neighbors_Survives()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.neighbors.Add(new Cell { IsAlive = true });

            cell.DetermineNextLiveState();
            cell.Advance();

            Assert.IsTrue(cell.IsAlive, "Живая клетка с 2 соседями должна выжить.");
        }

        [TestMethod]
        public void Cell_AliveWith3Neighbors_Survives()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.neighbors.Add(new Cell { IsAlive = true });

            cell.DetermineNextLiveState();
            cell.Advance();

            Assert.IsTrue(cell.IsAlive, "Живая клетка с 3 соседями должна выжить.");
        }

        [TestMethod]
        public void Cell_AliveWith1Neighbor_Dies()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.Add(new Cell { IsAlive = true }); // Только 1 живой сосед

            cell.DetermineNextLiveState();
            cell.Advance();

            Assert.IsFalse(cell.IsAlive, "Живая клетка с < 2 соседями должна умереть от одиночества.");
        }

        [TestMethod]
        public void Cell_AliveWith4Neighbors_Dies()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 4; i++) cell.neighbors.Add(new Cell { IsAlive = true });

            cell.DetermineNextLiveState();
            cell.Advance();

            Assert.IsFalse(cell.IsAlive, "Живая клетка с > 3 соседями должна умереть от перенаселенности.");
        }

        // === ТЕСТЫ ДОСКИ И ЛОГИКИ (BOARD) ===

        [TestMethod]
        public void Board_Initialization_SetsCorrectDimensions()
        {
            var board = new Board(100, 50, 1, 0); // width 100, height 50, cellsize 1
            Assert.AreEqual(100, board.Columns);
            Assert.AreEqual(50, board.Rows);
        }

        [TestMethod]
        public void Board_Clear_KillsAllCells()
        {
            var board = new Board(10, 10, 1, 1.0); // 100% заполнение
            board.Clear();
            Assert.AreEqual(0, board.CountAlive(), "После Clear на доске не должно быть живых клеток.");
        }

        [TestMethod]
        public void Board_CountAlive_ReturnsCorrectAmount()
        {
            var board = new Board(5, 5, 1, 0); // пустая доска
            board.Cells[0, 0].IsAlive = true;
            board.Cells[1, 1].IsAlive = true;
            board.Cells[4, 4].IsAlive = true;

            Assert.AreEqual(3, board.CountAlive());
        }

        [TestMethod]
        public void Board_Randomize_ChangesAliveCount()
        {
            var board = new Board(10, 10, 1, 0); // пустая доска
            board.Randomize(0.5); // 50% плотность
            Assert.IsTrue(board.CountAlive() > 0, "После Randomize должны появиться живые клетки.");
        }

        // === ТЕСТЫ ФИГУР (ПОВЕДЕНИЕ СИСТЕМЫ) ===

        [TestMethod]
        public void Board_BlockPattern_IsStable()
        {
            var board = new Board(4, 4, 1, 0);
            // Создаем блок 2x2
            board.Cells[1, 1].IsAlive = true;
            board.Cells[1, 2].IsAlive = true;
            board.Cells[2, 1].IsAlive = true;
            board.Cells[2, 2].IsAlive = true;

            board.Advance();

            Assert.AreEqual(4, board.CountAlive());
            Assert.IsTrue(board.Cells[1, 1].IsAlive && board.Cells[2, 2].IsAlive);
        }

        [TestMethod]
        public void Board_BlinkerPattern_Oscillates()
        {
            var board = new Board(5, 5, 1, 0);
            // Горизонтальная мигалка 1x3
            board.Cells[1, 2].IsAlive = true;
            board.Cells[2, 2].IsAlive = true;
            board.Cells[3, 2].IsAlive = true;

            board.Advance();

            // Должна стать вертикальной
            Assert.AreEqual(3, board.CountAlive());
            Assert.IsTrue(board.Cells[2, 1].IsAlive);
            Assert.IsTrue(board.Cells[2, 2].IsAlive);
            Assert.IsTrue(board.Cells[2, 3].IsAlive);
        }

        // === ТЕСТЫ ФАЙЛОВОЙ СИСТЕМЫ ===

        [TestMethod]
        public void Board_SaveState_CreatesFile()
        {
            var board = new Board(5, 5, 1, 0);
            string testFile = "test_save.txt";

            board.SaveState(testFile);

            Assert.IsTrue(File.Exists(testFile));
            File.Delete(testFile); // Убираем за собой
        }

        [TestMethod]
        public void Board_LoadState_RestoresBoardCorrectly()
        {
            var board = new Board(3, 3, 1, 0);
            string testFile = "test_load.txt";
            File.WriteAllText(testFile, "100\n010\n001");

            board.LoadState(testFile);

            Assert.IsTrue(board.Cells[0, 0].IsAlive);
            Assert.IsTrue(board.Cells[1, 1].IsAlive);
            Assert.IsTrue(board.Cells[2, 2].IsAlive);
            Assert.AreEqual(3, board.CountAlive());

            File.Delete(testFile);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void Board_LoadState_ThrowsExceptionIfFileNotFound()
        {
            var board = new Board(5, 5, 1, 0);
            board.LoadState("non_existent_file.txt");
        }

        // === ТЕСТ КЛАСТЕРИЗАЦИИ (ЗАДАЧА 2) ===

        [TestMethod]
        public void Board_FindClusters_ReturnsCorrectNumberOfClusters()
        {
            var board = new Board(10, 10, 1, 0);
            // Кластер 1 (блок)
            board.Cells[0, 0].IsAlive = true; board.Cells[0, 1].IsAlive = true;
            board.Cells[1, 0].IsAlive = true; board.Cells[1, 1].IsAlive = true;

            // Кластер 2 (одиночная клетка в другом углу)
            board.Cells[8, 8].IsAlive = true;

            var clusters = board.FindClusters();

            Assert.AreEqual(2, clusters.Count, "Должно быть найдено ровно 2 изолированных кластера.");
        }
    }
}