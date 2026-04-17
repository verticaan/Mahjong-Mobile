using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class Layer
    {
        [SerializeField] LayerRow[] rows;

        public int AmountOfRows => rows.Length;

        public LayerRow this[int i] => rows[i];

        /// <summary>
        /// Runtime-only constructor used by
        /// <see cref="LevelController.StripEffects"/> to build effect-free copies.
        /// Never called from inspector-deserialised paths.
        /// </summary>
        public Layer(LayerRow[] rows)
        {
            this.rows = rows;
        }

        public LayerRow GetRow(int i)
        {
            if (i < AmountOfRows && i >= 0) return rows[i];
            return null;
        }

        public int GetAmountOfFilledCells()
        {
            int counter = 0;
            for (int i = 0; i < AmountOfRows; i++)
            {
                counter += rows[i].GetAmountOfFilledCells();
            }
            return counter;
        }
    }
}