
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;


namespace TwitchChat
{
    public class TwitchChat : UdonSharpBehaviour
    {
        [SerializeField]
        private float interval = 5f;
        [SerializeField]
        private TwitchChatUIController UIController;

        // TODO - would be nice if we could have a serialized field for our endpoint
        private readonly VRCUrl AllMessagesURL = new VRCUrl("http://localhost:3000/messages");
        private readonly VRCUrl subfrequenciesURL = new VRCUrl("http://localhost:3000/messages/subfrequencies");
        private float time = 0.0f;
        private int lastHash;

        private DataToken channelsToken = new DataToken("channels");
        private DataToken messagesToken = new DataToken("messages");

        private DataToken usersToken = new DataToken("users");

        void Start()
        {
            // I think we could get extra clever by making the DataContainer networked
            // then we could ask each player to fetch the data round-robin style and
            // at least until VRC decides to shorten the time in-between string loads
            FetchMessages();
        }

        void Update()
        {
            time += Time.deltaTime;

            if (time >= interval)
            {
                time -= interval;
                FetchMessages();          
            }
        }

        private void FetchMessages()
        {
            //Debug.Log("fetching ALL messages...");
            //VRCStringDownloader.LoadUrl(AllMessagesURL, (IUdonEventReceiver)this);

            Debug.Log("fetching messages from subfrequencies...");
            VRCStringDownloader.LoadUrl(subfrequenciesURL, (IUdonEventReceiver)this);
        }

        public override void OnStringLoadSuccess(IVRCStringDownload download)
        {
            string json = download.Result;

            if (lastHash == download.Result.GetHashCode()) return;

            if(VRCJson.TryDeserializeFromJson(json, out DataToken result))
            {
                if (result.TokenType == TokenType.DataDictionary)
                {
                    if (result.DataDictionary.TryGetValue(usersToken, out DataToken users))
                    {
                        UIController.UpdateUsers(users.DataDictionary);
                    }

                    // now we have to determine which form has been loaded, if this is all messages
                    // we should have a property called "channels" which contains each channel we're
                    // watching and the subsequent messages within each - if we're pulling from a 
                    // specific channel however we'll have a "messages" property at this level
                    if (result.DataDictionary.TryGetValue(channelsToken, out DataToken channels))
                    {
                        UIController.UpdateMessages(channels.DataDictionary);
                    }
                    else if (result.DataDictionary.TryGetValue(messagesToken, out DataToken messages))
                    {
                        UIController.UpdateMessages(messages.DataList);
                    }
                }
            } 
            else
            {
                Debug.Log($"Failed to Deserialize json {json} - {result}");
            }

            lastHash = download.Result.GetHashCode();
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            Debug.Log("Error when loading a string!");
            Debug.Log(result);

            // should do something nicer for the UI
        }
    }
}
