
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;

namespace TwitchChat
{
    public class TwitchChatUIController : UdonSharpBehaviour
    {
        [SerializeField]
        private string Channel;
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

        private DataToken nameToken = new DataToken("name");
        private DataToken startToken = new DataToken("start");
        private DataToken endToken = new DataToken("end");

        public void SetChannel(string channelName)
        {
            Clear();
            Channel = channelName;
        }

        public void UpdateMessages(DataList data)
        {
            // DataList comes as a list of dictionaries
            // each one an object { user: "foo", message: "bar" }
            // there may be other attributes added later
            // also need to consider what we're doing with the users dictionary

            // yeah I'm not super keen on how this works either...
            // maybe we can be more clever, or clever enough to retain history
            Clear();

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
            // need to set the channel here derp...
            if(data.TryGetValue(new DataToken(Channel), out DataToken channelObj))
            {
                if(channelObj.DataDictionary.TryGetValue(MessagesToken, out DataToken messages))
                {
                    UpdateMessages(messages.DataList);
                }
            }
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
            }
        }

        private void Clear()
        {
            foreach (Transform child in ChatMessagePanel.transform)
            {
                Destroy(child.gameObject);
            }
        }

        private void AddMessage(DataDictionary messageObj)
        {
            GameObject msg = Instantiate(ChatMessagePrefab, ChatMessagePanel.transform);
            TwitchChatMessage tcm = msg.GetComponent<TwitchChatMessage>();
            if (tcm != null)
            {
                if (messageObj.TryGetValue(UserToken, out DataToken u))
                {
                    string color = "#FFFFFF";
                    string name = u.String;
                    // get the display name from users dictionary
                    if (Users.TryGetValue(new DataToken(u.String), out DataToken userData))
                    {
                        if (userData.DataDictionary.TryGetValue(ColorToken, out DataToken userColour) 
                            && userColour.TokenType == TokenType.String)
                        {
                            color = userColour.String;
                        }

                        if (userData.DataDictionary.TryGetValue(DisplayNameToken, out DataToken displayName) 
                            && displayName.TokenType == TokenType.String)
                        {
                            name = displayName.String;
                        }
                    }

                    if (messageObj.TryGetValue(MessageToken, out DataToken m)
                        && m.TokenType == TokenType.String
                        && messageObj.TryGetValue(EmotesToken, out DataToken e)
                        && e.TokenType == TokenType.DataList)
                    {
                        string emotified = ProcessEmotes(m.String, e.DataList);
                        tcm.SetMessage(name, emotified, color);
                    }
                    else
                    {
                        tcm.SetMessage(name, m.String, color);
                    }
                }
            }
        }

        private string ProcessEmotes(string message, DataList emotes)
        {
            string result = message;

            // There is something silly going on here, although my API is returning an integer
            // udon refuses to accept that the values stored in start and end are anything but
            // a Double, so I'll do a cast to int and truncate any potential garbage after the
            // decimal
            foreach (DataToken emote in emotes.ToArray())
            {
                if (emote.DataDictionary.TryGetValue(nameToken, out DataToken name))
                {
                    // TODO - I'm sure there's something more clever we can do here
                    emote.DataDictionary.TryGetValue(startToken, out DataToken s);
                    emote.DataDictionary.TryGetValue(endToken, out DataToken e);
                    string wip = result;
                    int start = (int)s.Double;
                    int end = (int)e.Double;
                    int length = (end - start) + 1;
                    wip = wip.Remove(start, length);
                    wip = wip.Insert(start, $"<sprite name=\"{name.String}\">");
                    result = wip;
                }
            }

            return result;
        }
    }
}
