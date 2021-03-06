using TwitchLib.Communication.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Client.Events;
using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using TwitchLib.Communication.Events;

namespace XdTwitchBot
{
	/// <summary>
	/// This class is responsible for managing the bot - connecting, disconnecting and sending whispers to the nasty users that type Xd.
	/// </summary>
	internal class TwitchChatBot
	{
		private readonly ConnectionCredentials credentials = new ConnectionCredentials(TwitchInfo.BotUsername, TwitchInfo.BotToken);
		private TwitchClient client;

		private readonly string Message1 = @"Serio, mało rzeczy mnie triggeruje tak jak to chore „Xd”. Kombinacji x i d można używać na wiele wspaniałych sposobów.
	Coś cię śmieszy? Stawiasz „xD”. Coś się bardzo śmieszy? Śmiało: „XD”! Coś doprowadza Cię do płaczu ze śmiechu? „XDDD” i załatwione. Uśmiechniesz się pod nosem?
	„xd”. Po kłopocie. A co ma do tego ten bękart klawiaturowej ewolucji, potwór i zakała ludzkiej estetyki - „Xd”? Co to w ogóle ma wyrażać? Martwego człowieka z wywalonym
	jęzorem?";

		private readonly string Message2 = @"Powiem Ci, co to znaczy. To znaczy, że masz w telefonie włączone zaczynanie zdań dużą literą, ale szkoda Ci klikać capsa na jedno „d” później.Korona z głowy spadnie?
	Nie sondze. „Xd” to symptom tego, że masz mnie, jako rozmówcę, gdzieś, bo Ci się nawet kliknąć nie chce, żeby mi wysłać poprawny emotikon.Szanujesz mnie? Używaj „xd”, „xD”,
	„XD”, do wyboru. Nie szanujesz mnie? Okaż to. Wystarczy, że wstawisz to zjebane „Xd” w choć jednej wiadomości. Nie pozdrawiam.";

		//In case the person whispers back with a message that contains Xd
		private readonly string ResponseMessage = @"...masz ty w ogóle rozum i godność człowieka? :rage:";

		//Manual whisper throttler since WhisperThrottlingPeriod doesn't seem to be working
		private int MessageCount = 0;

		//Keeping track of the total whispers sent (to check if the bot isn't malfunctioning/spamming too much)
		private int GlobalMessageCount = 0;

		//Making the whisper throttle more smooth by measuring how long it took since last Xd
		private TimeSpan TimeElapsed;
		private DateTime CurrentDate;

		/// <summary>
		/// The dictionary is responsible for keeping track of users' usage of Xd - every time a user gets sent a whisper, one is added to value. The bot will not send
		/// a whisper to the same user more than once during a given session. Upon disconnection, the dictionary gets logged to a file, just so that we can see who
		/// was naughty and used the forbidden Xd a lot.
		/// </summary>
		private Dictionary<string, int> AlreadySent = new Dictionary<string, int>();

		internal void Connect()
		{
			Console.WriteLine("Connecting");

			var options = new ClientOptions//doesn't seem to work
			{
				WhisperThrottlingPeriod = TimeSpan.FromSeconds(45),
				WhispersAllowedInPeriod = 10
			};

			var customClient = new WebSocketClient(options);
			//customClient.WhisperThrottlingPeriod

			client = new TwitchClient(customClient);
			client.Initialize(credentials, TwitchInfo.ChannelName);

			client.OnConnected += Client_OnConnected;
			client.OnLog += Client_OnLog;
			client.OnConnectionError += Client_OnConnectionError;
			client.OnMessageReceived += Client_OnMessageReceived;
			client.OnWhisperReceived += Client_OnWhisperReceived;

			client.Connect();
			Console.WriteLine("Connected!");
			CurrentDate = DateTime.Now;
		}

		private void Client_OnConnected(object sender, OnConnectedArgs e)
		{
			client.SendMessage(TwitchInfo.ChannelName, "HeyGuys");

			//foreach (var channel in client.JoinedChannels)//reserved in case joining many channels is implemented
			//{
			//	client.SendMessage(channel, "HeyGuys");
			//}
		}

		private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
		{
			Console.WriteLine($"Received a whisper from {e.WhisperMessage.DisplayName}! Message: {e.WhisperMessage.Message}");

			if (e.WhisperMessage.Message.Contains("Xd"))
			{
				client.SendWhisper(e.WhisperMessage.DisplayName, ResponseMessage);
			}
		}

		private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
		{
			if (e.ChatMessage.Message.Contains("Xd"))
			{
				var username = e.ChatMessage.DisplayName;
				Console.WriteLine($"Name of the user who typed Xd: {username}");

				if (AlreadySent.ContainsKey(username))//the user has already said Xd during the session
				{
					AlreadySent[username]++;//increment the value if the key(username) exists
				}
				else//the user has not said Xd (yet)
				{
					this.Whisper(username);
				}
			}
		}

		private void Client_OnLog(object sender, OnLogArgs e)
		{
			//Console.WriteLine(e.Data);
		}

		private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
		{
			Console.WriteLine($"Error! {e.Error}");
		}

		private void Whisper(string username)
		{
			client.SendWhisper(username, Message1);
			client.SendWhisper(username, Message2);

			AlreadySent.Add(username, 1);//add a new user with a base count of one "Xd"

			GlobalMessageCount += 2;
			TimeElapsed = DateTime.Now - CurrentDate;

			//add to the counter only if less than 10 seconds have passed since the last whisper
			if (TimeElapsed < TimeSpan.FromMilliseconds(10000))
			{
				MessageCount++;
			}

			if (MessageCount > 5)//wait after sending too many messages - to be improved (maybe?)
			{
				Thread.Sleep(30000);
				MessageCount = 0;
			}
			CurrentDate = DateTime.Now;
		}

		internal void Disconnect()
		{
			Console.WriteLine("Disconnecting");
			client.Disconnect();
			Console.WriteLine("Disconnected!");

			if (AlreadySent.Count > 0)
			{
				//output the log
				var path = String.Format("XdLogs{0}.txt", DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss"));

				using (var writer = new StreamWriter(path))
				{
					writer.WriteLine($"Total whispers sent: {GlobalMessageCount}");
					writer.WriteLine("Username: Amount of Xd's during the session");
					foreach (var pair in AlreadySent)
					{
						writer.WriteLine($"{pair.Key}: {pair.Value}");
					}
				}
			}
		}
	}
}
