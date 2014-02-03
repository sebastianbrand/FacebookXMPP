using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using agsXMPP;
using agsXMPP.Collections;
using agsXMPP.protocol.client;
using Newtonsoft.Json;

namespace facebookXMPP
{
    public class FacebookChatClient
    {
        private readonly WebClient _webClient;
        private readonly Dictionary<string, FacebookUser> _contacts;
        private XmppClientConnection _xmppClient;

        public Dictionary<string, FacebookUser> Contacts
        {
            get { return _contacts; }
        }

        public bool LoggedIn { get; private set; }

        public Action<bool> OnLoginResult;
        public Action<Message, FacebookUser> OnMessageReceived;
        public Action<string> OnConnectionStateChanged;
        public Action<FacebookUser> OnUserIsTyping;
        public Action<FacebookUser> OnContactAdded;
        public Action<FacebookUser> OnContactRemoved;
        public Action<string> OnAuthError;
        public Action OnLogout;

        public FacebookChatClient()
        {
            _webClient = new WebClient();
            _contacts = new Dictionary<string, FacebookUser>();
            LoggedIn = false;
        }

        public void Login(string username, string password)
        {
            if (!LoggedIn)
            {
                try
                {
                    _xmppClient = new XmppClientConnection("chat.facebook.com", 5222);
                    _xmppClient.OnXmppConnectionStateChanged += (sender, state) =>
                    {
                        if (OnConnectionStateChanged != null) OnConnectionStateChanged(state.ToString());
                    };
                    _xmppClient.UseStartTLS = true;
                    _xmppClient.OnPresence += UpdateUserList;
                    _xmppClient.OnLogin += OnLogin;
                    _xmppClient.OnAuthError += (sender, element) =>
                    {
                        if (OnAuthError != null) OnAuthError(element.ToString());
                    };

                   _xmppClient.Open(username, password);
                }
                catch
                {
                    if (OnLoginResult != null)
                        OnLoginResult(false);
                }
            }
        }

        public void SendMessage(string msg, string receiverName)
        {
            _xmppClient.Send(new Message(
                new Jid(_contacts.First(x => x.Value.Name == receiverName).Key), MessageType.chat, msg));
        }
        
        public void Logout() { 
            _xmppClient.Close(); 
            LoggedIn = false;
            OnLogout();
        }

        private void UpdateUserList(object sender, Presence pres)
        {
            var user = GetUser(pres.From.User);
            user.Jid = pres.From.Bare;

            if (pres.Type == PresenceType.available && !_contacts.ContainsKey(pres.From.Bare))
            {
                _contacts.Add(pres.From.Bare, user);
                _xmppClient.MessageGrabber.Add(new Jid(pres.From.Bare), new BareJidComparer(), MessageReceived, null);
                if (OnContactAdded != null) OnContactAdded(user);
            }
            else if (pres.Type == PresenceType.unavailable && _contacts.ContainsKey(pres.From.Bare))
            {
                _xmppClient.MessageGrabber.Remove(new Jid(pres.From.Bare));
                _contacts.Remove(pres.From.Bare);

                if (OnContactRemoved != null) OnContactRemoved(GetUser(pres.From.User));
            }
        }

        private FacebookUser GetUser(string userId)
        {
            userId = userId.Replace("-", string.Empty);
            var response = _webClient.DownloadString("https://graph.facebook.com/" + userId);
            var user = JsonConvert.DeserializeObject<FacebookUser>(response);

            return user;
        }

        private void MessageReceived(object sender, Message msg, object data)
        {
            if(String.IsNullOrEmpty(msg.Body) && OnUserIsTyping != null)
            {
                _contacts[msg.From.Bare].IsTyping = !_contacts[msg.From.Bare].IsTyping;
                OnUserIsTyping(_contacts[msg.From.Bare]);
            }
            else if(OnMessageReceived != null && !String.IsNullOrEmpty(msg.Body))
            {
                var from = _contacts.First(x => x.Key == msg.From.Bare).Value;
                from.IsTyping = false;
                OnMessageReceived(msg, from);
            }
        }

        private void OnLogin(object sender)
        {
            LoggedIn = true;
            if (OnLoginResult != null)
                OnLoginResult(true);

            _xmppClient.OnPresence += UpdateUserList;

            // Changing presence to online
            var presence = new Presence(ShowType.chat, "Online") {Type = PresenceType.available};
            _xmppClient.Send(presence);
        }
    }
}
