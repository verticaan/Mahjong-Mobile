using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Cards Database", menuName = "Content/Cards/Database")]
    public class CardDeckSO : ScriptableObject
    {
        [SerializeField] CardDataSO[] cardData;
        public CardDataSO[] CardData => cardData;
    }
}
