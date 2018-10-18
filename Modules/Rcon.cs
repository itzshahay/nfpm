﻿using System;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using CommandLine;
using JetBrains.Annotations;
using System.Threading.Tasks;
using Console = Colorful.Console;
using NFive.PluginManager.Extensions;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Connect to a running FiveM server over RCON.
	/// </summary>
	[UsedImplicitly]
	[Verb("rcon", HelpText = "Connect to a running FiveM server over RCON.")]
	internal class Rcon
	{
		private PluginManager.Rcon rcon;

		[Option('h', "host", Default = "localhost", Required = false, HelpText = "Remote server host.")]
		public string Host { get; set; }

		[Option('p', "port", Default = 30120, Required = false, HelpText = "Remote server port.")]
		public int Port { get; set; }

		[Option("password", Required = false, HelpText = "Remote server password, if unset will be prompted.")]
		public string Password { get; set; }

		[Value(0, Required = false, HelpText = "Command to run on the remote server, if unset will use stdin.")]
		public string Command { get; set; }

		internal async Task<int> Main()
		{
			if (this.Password == null)
			{
				this.Password = Input.String("Password");
				Console.CursorTop--;
				Console.Write(new string(' ', Console.WindowWidth - 1));
				Console.CursorLeft = 0;
			}

			Console.WriteLine($"Connecting to {this.Host}:{this.Port}...", Color.Green);

			this.rcon = new PluginManager.Rcon(Dns.GetHostEntry(this.Host).AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork), this.Port, this.Password);

			if (this.Command == null)
			{
				while (true)
				{
					if (await this.RunCommand(Input.String("#")))
					{
						return 1;
					}
				}
			}

			await this.RunCommand(this.Command);

			return 0;
		}

		private async Task<bool> RunCommand(string command)
		{
			var response = await this.rcon.Command(command);

			var lines = response
				.Split('\n')
				.Select(l => l.TrimEnd("^7")) // Why FiveM? Why?
				.ToList();

			var output = string.Join(Environment.NewLine, lines); // Correct line endings

			if (lines.Any(l =>
				l.Equals("Invalid password.", StringComparison.InvariantCultureIgnoreCase) ||
				l.Equals("The server must set rcon_password to be able to use this command.", StringComparison.InvariantCultureIgnoreCase) ||
				l.StartsWith("No such command ", StringComparison.InvariantCultureIgnoreCase)
			))
			{
				Console.Write(output, Color.Red);

				return true;
			}

			Console.Write(output);

			return false;
		}
	}
}