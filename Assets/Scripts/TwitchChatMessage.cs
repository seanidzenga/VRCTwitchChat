
using UdonSharp;
using TMPro;

namespace TwitchChat
{
    public class TwitchChatMessage : UdonSharpBehaviour
    {
        public void SetMessage(string user, string message, string userColour = "#FFFFFF", bool highlight = false)
        {
            TextMeshProUGUI MessageComponent = GetComponent<TextMeshProUGUI>();
            MessageComponent.text = string.Format(MessageComponent.text, user, message, userColour);
        }
    }
}