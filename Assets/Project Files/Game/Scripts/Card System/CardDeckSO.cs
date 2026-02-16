using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Cards Deck", menuName = "Content/Cards/Deck")]
    public class CardDeckSO : ScriptableObject
    {
        [SerializeField] CardDataSO[] cardData;
        public CardDataSO[] CardData => cardData;
    }
}
