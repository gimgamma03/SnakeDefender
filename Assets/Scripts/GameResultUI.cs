using UnityEngine;
using UnityEngine.UI;

namespace SnakeDefender
{
    public class GameResultUI : MonoBehaviour
    {
        [SerializeField] private Text resultText;

        public void ShowVictory()
        {
            SetText("Victory");
        }

        public void ShowDefeat()
        {
            SetText("Defeat");
        }

        private void SetText(string message)
        {
            if (resultText == null)
            {
                return;
            }

            resultText.gameObject.SetActive(true);
            resultText.text = message;
        }
    }
}
