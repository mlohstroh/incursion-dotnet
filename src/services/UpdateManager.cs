using System;
using Octokit;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Jabber
{
    public class UpdateManager
    {
        private const int UpdateExitCode = 9567;
        private const string ReleaseZipName = "release.zip";

        private GitHubClient m_client;
        private string m_githubToken;

        public UpdateManager()
        {
            CreateGithubClient();
        }

        private void CreateGithubClient()
        {
            if(m_client != null)
                return;

            // Github Client.
            var client = new GitHubClient(new Octokit.ProductHeaderValue("samuel-grant"));

            // Authorised Repo access.
            Config.GetString("GITHUB_TOKEN", out m_githubToken);
            client.Credentials = new Credentials(m_githubToken);

            m_client = client;
        }

        /// <summary>
        /// Returns the application version as stored in the assembly file
        /// </summary>
        /// <returns>string version</returns>
        private string GetApplicationVersion()
        {
            try
            {
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                FileVersionInfo version = FileVersionInfo.GetVersionInfo(assembly.Location);

                return version.FileVersion;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Format("Error getting assembly version {0}", e.Message));
                Console.ForegroundColor = ConsoleColor.White;
                return null;
            }

        }

        /// <summary>
        /// Returns the latest github release version
        /// </summary>
        /// <returns>string version</returns>
        private async Task<Release> GetGithubReleaseVersionAsync()
        {
            var releases = await m_client.Repository.Release.GetAll("mlohstroh", "incursion-dotnet");
            var latest = releases[0];

            System.Diagnostics.Debug.WriteLine(latest.Name);
            Console.Beep();


            if (latest != null)
                return latest;

            return null;
        }


        /// <summary>
        /// Checks to see if an update is required.
        /// </summary>
        public void HandleUpdate()
        {
            var currentVersion = GetApplicationVersion();
            Release applicationVersion = GetGithubReleaseVersionAsync().Result;

            // Error and abort if we cannot get a github or application version
            if(currentVersion ==  null || applicationVersion == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Format("Error checking for updates"));
                Console.ForegroundColor = ConsoleColor.White;

                return;
            }

            // If the current version and github versions do not match - Flag for updates.
            if(currentVersion != applicationVersion.TagName)
            {
                DoUpdate(applicationVersion).Wait();
            }
        }

        private async Task DoUpdate(Release latestRelease)
        {
            string tempPath = GetTempDownloadFilePath();

            if(File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            var handler = new HttpClientHandler() 
            {
                AllowAutoRedirect = true
            };

            HttpClient client = new HttpClient(handler);

            client.DefaultRequestHeaders.Add("User-Agent", "incursion-dotnet/1.0");
            client.DefaultRequestHeaders.Add("Accept", "application/octet-stream");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", m_githubToken);
            var assets = latestRelease.Assets;
            string chosenUrl = "";


            foreach(var asset in assets)
            {
                if(asset.Name == ReleaseZipName)
                {
                    chosenUrl = asset.BrowserDownloadUrl;
                }
            }
            
            byte[] bytes = await client.GetByteArrayAsync(chosenUrl);

            File.WriteAllBytes(tempPath, bytes);

            string extractDir = Path.Combine(Path.GetTempPath(), "temp-update");
            // unzip to temp directory
            ZipFile.ExtractToDirectory(tempPath, extractDir);

            string currentDirectory = Environment.CurrentDirectory;

            // Check our config to make sure 
            if(!Config.GetString("IS_DEPLOYMENT", out string testString))
            {
                // do nothing
                return;
            }

            // Otherwise, lets do extremely destructive actions. yay...

            string[] filePaths = Directory.GetFiles(extractDir);
            for(int i = 0; i < filePaths.Length; i++)
            {
                // what can possibly go wrong
                File.Copy(filePaths[i], currentDirectory, true);
            }

            // Die
            Environment.Exit(UpdateExitCode);
        }

        private string GetTempDownloadFilePath()
        {
            return Path.Combine(Path.GetTempPath(), "temp-update.zip");
        }
    }
}