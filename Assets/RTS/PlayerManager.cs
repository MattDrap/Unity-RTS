using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using Newtonsoft.Json;

namespace RTS {
	public static class PlayerManager {
		[Serializable]
		private struct PlayerDetails {
			private string name;
			public PlayerDetails(string name) {
				this.name = name;
			}
			public string Name { get { return name; } set{ name = value;} }
		}
		private static List< PlayerDetails > players = new List< PlayerDetails >();
		private static PlayerDetails currentPlayer;

		public static void SelectPlayer(string name) {
			//check player doesnt already exist
			bool playerExists = false;
			foreach(PlayerDetails player in players) {
				if(player.Name == name) {
					currentPlayer = player;
					playerExists = true;
				}
			}
			if(!playerExists) {
				Directory.CreateDirectory("SavedGames" + Path.DirectorySeparatorChar + name);
				PlayerDetails newPlayer = new PlayerDetails(name);
				players.Add(newPlayer);
				currentPlayer = newPlayer;
			}
			Save();
		}

		public static string GetPlayerName() {
			return currentPlayer.Name == "" ? "Unknown" : currentPlayer.Name;
		}
		public static void Save() {
			JsonSerializer serializer = new JsonSerializer();
			serializer.NullValueHandling = NullValueHandling.Ignore;
			using(StreamWriter sw = new StreamWriter("SavedGames" + Path.DirectorySeparatorChar + "Players.json")){
				using (JsonWriter jw = new JsonTextWriter (sw)) {
					jw.WriteStartObject ();
					jw.WritePropertyName ("Current");
					jw.WriteValue (currentPlayer.Name);
					jw.WritePropertyName ("Players");
					serializer.Serialize (jw, players);
					jw.WriteEndObject ();
					//BinaryFormatter formatter = new BinaryFormatter ();
					//formatter.Serialize (sw, players);
				}
			}
		}
		public static void Load() {
			players.Clear();

			string filename = "SavedGames" + Path.DirectorySeparatorChar + "Players.json";
			if(File.Exists(filename)) {
				//read contents of file
				using(StreamReader tr = new StreamReader(filename)) {
					//BinaryFormatter formatter = new BinaryFormatter ();
					//object ob = formatter.Deserialize (sr);
					//if (ob is List<PlayerDetails>) {
					//	players = ob as List<PlayerDetails>;
					//	Debug.Log (players);
					//	foreach (PlayerDetails pd in players) {
					//		Debug.Log (pd.Name);
					//	}
					//}
					string currentPlayerName = null;
					using(JsonReader jr = new JsonTextReader(tr)){
						JsonSerializer serializer = new JsonSerializer();
						while (jr.Read ()) {
							if (jr.Value != null) {
								if (jr.TokenType == JsonToken.PropertyName) {
									string property = (string)jr.Value;
									if (property == "Current") {
										jr.Read ();
										currentPlayerName = (string)jr.Value;
									}
									if (property == "Players") {
										jr.Read ();
										players = serializer.Deserialize< List< PlayerDetails > > (jr);	
									}
								}
							}
						}
						if (players != null && currentPlayerName != null) {
							currentPlayer = players.Find (a => a.Name == currentPlayerName);
						}
					}
				}
			}
		}
		public static string[] GetPlayerNames() {
			string[] playerNames = new string[players.Count];
			for(int i = 0; i < playerNames.Length; i++) playerNames[i] = players[i].Name;
			return playerNames;
		}

		public static string[] GetSavedGames() {
			DirectoryInfo directory = new DirectoryInfo("SavedGames" + Path.DirectorySeparatorChar + currentPlayer.Name);
			FileInfo[] files = directory.GetFiles();
			string[] savedGames = new string[files.Length];
			for(int i=0; i < files.Length; i++) {
				string filename = files[i].Name;
				savedGames[i] = filename.Substring(0, filename.IndexOf("."));
			}
			return savedGames;
		}
	}
}