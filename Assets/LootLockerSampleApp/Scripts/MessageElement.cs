using LootLocker.Requests;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LootLockerDemoApp
{
    public class ElementSprites
    {
        public string spriteName;
        public Sprite sprite;
    }

    public class MessageElement : MonoBehaviour
    {
        public Image messageIconState;
        public Text messageSummaryText;
        public Sprite closedMessageSprite, openedMessageSprite;
        public ElementSprites[] elementSprites;
        public enum MessageState { Unread, Read };

        public void InitMessage(LootLockerGMMessage message)
        {

            Fill(message._new ? MessageState.Unread : MessageState.Read, message.summary);

        }

        void Fill(MessageState messageState, string content)
        {

            try
            {

                switch (messageState)
                {

                    case MessageState.Unread:

                        messageIconState.sprite = closedMessageSprite;

                        break;

                    case MessageState.Read:

                        messageIconState.sprite = openedMessageSprite;

                        break;

                }

                messageSummaryText.text = content;

            }

            catch (Exception ex)
            {

                Debug.LogWarning("Message init error: " + ex);

            }

        }

    }
}