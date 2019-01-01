using System;

namespace XdTwitchBot
{
	/// <summary>
	/// The class responsible for handling the console application - nothing big, just create the bot and let it connect/disconnect through the console.
	/// </summary>
	internal class Program
	{
		private static void Main(string[] args)
		{
			var bot = new TwitchChatBot();
			bot.Connect();

			Console.ReadLine();

			bot.Disconnect();
		}
	}
}