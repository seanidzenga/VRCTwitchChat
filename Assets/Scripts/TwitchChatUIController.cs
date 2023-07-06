
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;

namespace TwitchChat
{
    public class TwitchChatUIController : UdonSharpBehaviour
    {
        [SerializeField]
        private GameObject ChatMessagePrefab;
        [SerializeField]
        private GameObject ChatMessagePanel;

        private DataToken MessagesToken = new DataToken("messages");
        private DataToken ChannelsToken = new DataToken("channels");

        private DataToken UserToken = new DataToken("user");
        private DataToken MessageToken = new DataToken("message");
        private DataToken EmotesToken = new DataToken("emotes");

        private DataDictionary Users = new DataDictionary();
        private DataToken DisplayNameToken = new DataToken("display-name");
        private DataToken ColorToken = new DataToken("color");

        public void UpdateMessages(DataList data)
        {
            // DataList comes as a list of dictionaries
            // each one an object { user: "foo", message: "bar" }
            // there may be other attributes added later
            // also need to consider what we're doing with the users dictionary

            // yeah I'm not super keen on how this works either...
            // maybe we can be more clever, or clever enough to retain history
            foreach(Transform child in ChatMessagePanel.transform)
            {
                Destroy(child.gameObject);
            }

            for (int i = data.Count; i > -1; i--)
            {
                if (data.TryGetValue(i, out DataToken messageObj))
                {
                    PrintMessage(messageObj.DataDictionary);
                    AddMessage(messageObj.DataDictionary);
                }
            }

            // force a rebuild because layout manager components just be like that I guess
            LayoutRebuilder.ForceRebuildLayoutImmediate(ChatMessagePanel.GetComponent<RectTransform>());
        }

        public void UpdateMessages(DataDictionary data)
        {
            // in the case of getting ALL messages (a 'channels' object provided with messages per channel)

        }

        public void UpdateUsers(DataDictionary data)
        {
            Users = data;
        }

        private void PrintMessage(DataDictionary messageObj)
        {
            if (messageObj.TryGetValue(UserToken, out DataToken user))
            {
                messageObj.TryGetValue(MessageToken, out DataToken message);
                Debug.Log($"{user.String}: {message.String}");
            }
        }

        private void AddMessage(DataDictionary messageObj)
        {
            GameObject msg = Instantiate(ChatMessagePrefab, ChatMessagePanel.transform);
            TwitchChatMessage tcm = msg.GetComponent<TwitchChatMessage>();
            if(tcm != null)
            {
                if(messageObj.TryGetValue(UserToken, out DataToken u))
                {
                    string color = "#FFFFFF";
                    string name = u.String;
                    // get the display name from users dictionary
                    if(Users.TryGetValue(new DataToken(u.String), out DataToken userData))
                    {
                        if(userData.DataDictionary.TryGetValue(ColorToken, out DataToken userColour))
                        {
                            color = userColour.String;
                        }

                        if(userData.DataDictionary.TryGetValue(DisplayNameToken, out DataToken displayName))
                        {
                            name = displayName.String;
                        }
                    }

                    messageObj.TryGetValue(MessageToken, out DataToken m);

                    tcm.SetMessage(name, m.String, color);

                    // TODO - processing for emotes
                }
            }
        }
    }
}
