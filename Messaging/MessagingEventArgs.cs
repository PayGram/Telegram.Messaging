using System;

namespace Telegram.Messaging.Messaging
{
	public class MessagingEventArgs : EventArgs
	{
		public MessageManager Manager { get; set; }
	}
}
