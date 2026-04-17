using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class LayerRow
    {
        [SerializeField] CellData[] cells;

        public int AmountOfCells => cells.Length;

        public bool this[int i] => cells[i].IsFilled;

        /// <summary>
        /// Runtime-only constructor used by
        /// <see cref="LevelController.StripEffects"/> to build effect-free copies.
        /// Never called from inspector-deserialised paths.
        /// </summary>
        public LayerRow(CellData[] cells)
        {
            this.cells = cells;
        }

        public CellData GetCell(int i)
        {
            if (i < AmountOfCells && i >= 0) return cells[i];
            return null;
        }

        public int GetAmountOfFilledCells()
        {
            int counter = 0;
            for (int i = 0; i < AmountOfCells; i++)
            {
                if (cells[i].IsFilled) counter++;
            }
            return counter;
        }
    }
}