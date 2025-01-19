using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using CSCore.CoreAudioAPI;
using Microsoft.Toolkit.Uwp.Notifications;
using IniParser;
using IniParser.Model;

namespace Spotify_Ad_Muter
{
    internal class Program
    {
        public static int check_frequency = 500;
        public static bool show_toasts = false;
        public static bool advanced_mode = false;
        public static string client_id = "abc";
        public static string client_secret = "abc";

        private static string lastTrack = null;

        static async Task Main(string[] args)
        {
            // load config if existing
            if (File.Exists("./config.ini"))
            {
                var IniParser = new FileIniDataParser();
                IniData data = IniParser.ReadFile("./config.ini");

                check_frequency = Convert.ToInt32(data["settings"]["check_frequency"]);
                show_toasts = Convert.ToBoolean(data["settings"]["show_toasts"]);
                advanced_mode = Convert.ToBoolean(data["settings"]["advanced_mode"]);
                client_id = data["settings"]["client_id"];
                client_secret = data["settings"]["client_secret"];
            } else
            {
                Console.WriteLine("config file not existing. Using default configuration");
            }

            while (true)
            {

                //reduce checkspeed to improve performance
                Thread.Sleep(check_frequency);

                //get info about current window
                string trackInfo = GetSpotifyProcessName();

                if (trackInfo == lastTrack) { continue; };
                lastTrack = trackInfo;

                //check if a song is being played or not
                if (trackInfo == null) { continue; }

                //do basic validation
                if (!trackInfo.Contains('-'))
                {
                    Console.WriteLine("Songname not valid (no '-'), muting Spotify...");
                    ChangeSpotifyMuteStatus(mute: true);
                    continue;
                }

                //simple mode finished
                if (!advanced_mode) 
                {
                    Console.WriteLine($"Now playing: {trackInfo}");
                    continue;
                }

                string track = trackInfo.Split(" - ", StringSplitOptions.None)[1];
                string artist = trackInfo.Split(" - ", StringSplitOptions.None)[0];

                Console.WriteLine($"Now playing: {artist} - {track}");

                //get access token
                string accessToken = await GetAccessToken();
                if (string.IsNullOrEmpty(accessToken)) { return; }


                if (await IsValidSong(track, artist, accessToken))
                {
                    Console.WriteLine("Song found, unmuting Spotify...");
                    ChangeSpotifyMuteStatus(mute: false);
                }
                else
                {
                    Console.WriteLine("Song not found, muting Spotify...");
                    ChangeSpotifyMuteStatus(mute: true);
                };
            }
        }

        static string GetSpotifyProcessName()
        {
            //get spotify process
            var proc = Process.GetProcessesByName("Spotify").FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.MainWindowTitle));

            if (proc == null) return null;

            if (string.Equals(proc.MainWindowTitle, "Spotify Free") || string.Equals(proc.MainWindowTitle, "Spotify")) return null;

            return proc.MainWindowTitle;
        }

        static void ChangeSpotifyMuteStatus(bool mute)
        {
            var device = MMDeviceEnumerator.DefaultAudioEndpoint(
                DataFlow.Render,
                Role.Multimedia);

            var sessionManager = AudioSessionManager2.FromMMDevice(device);
            var enumerator = sessionManager.GetSessionEnumerator();

            //skipping first as it seems to be some default system session
            foreach (var sessionControl in enumerator)
            {
                var sessionControl2 = sessionControl.QueryInterface<AudioSessionControl2>();
                var process = Process.GetProcessById(sessionControl2.ProcessID);

                if (process.ProcessName == "Spotify")
                {
                    //wait for a second to unmute
                    if (!mute) Thread.Sleep(1000);

                    var volume = sessionControl.QueryInterface<SimpleAudioVolume>();
                    volume.MasterVolume = mute ? 0 : 1;

                    if (mute && show_toasts)
                    {
                        new ToastContentBuilder()
                            .AddText("Spotify Ad Muter")
                            .AddText("Spotify is muted because an ad is playing")
                            .AddAudio(null, silent:true)
                            .Show();
                    }

                    return;
                }
            }
            Console.WriteLine("Spotify application not found :/");
        }

        static async Task<string> GetAccessToken()
        {
            HttpClient client = new HttpClient();
            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{client_id}:{client_secret}"));
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {authString}");

            var requestBody = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
            HttpResponseMessage response = await client.PostAsync("https://accounts.spotify.com/api/token", requestBody);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(jsonResponse);
                return jsonDoc.RootElement.GetProperty("access_token").GetString();
            }
            else
            {
                Console.WriteLine("Failed to get access token. Are the client_id & client_secret correct?");
                return null;
            }
        }

        static async Task<bool> IsValidSong(string songName, string artistName, string accessToken)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            string encodedQuery = Uri.EscapeDataString($"track:{songName} artist:{artistName}");
            string url = $"https://api.spotify.com/v1/search?q={encodedQuery}&type=track&limit=1";

            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(jsonResponse);
                return (Convert.ToInt32(json.RootElement.GetProperty("tracks").GetProperty("total").GetUInt16()) > 0) ? true : false;
            } else
            {
                Console.WriteLine("Something went wrong");
                return true;
            }
        }
    }
}