using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Cards Database", menuName = "Content/Cards/Database")]
    public class CardDatabaseSO : ScriptableObject
    {
        [SerializeField] CardSettingBase[] cardSettings;
        public CardSettingBase[] CardSettings => cardSettings;
        
        [SerializeField] CardDataSO[] cardData;
        public CardDataSO[] CardData => cardData;
    }
}
