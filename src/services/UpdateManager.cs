using System;
using Octokit;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Jabber
{
    internal class UpdateManager
    {

        /// <summary>
        /// Returns the application version as stored in the assembly file
        /// </summary>
        /// <returns>string version</returns>
        internal static string GetApplicationVersion()
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
        internal static async Task<string> GetGithubReleaseVersionAsync()
        {
            // Github Client.
            var client = new GitHubClient(new ProductHeaderValue("samuel-grant"));

            // Authorised Repo access.
            Config.GetString("GITHUB_TOKEN", out string token);
            client.Credentials = new Credentials(token);

            var releases = await client.Repository.Release.GetAll("mlohstroh", "incursion-dotnet");
            var latest = releases[0];

            System.Diagnostics.Debug.WriteLine(latest.Name);
            Console.Beep();


            if (latest != null)
                return latest.TagName;

            return null;
        }


        /// <summary>
        /// Checks to see if an update is required.
        /// </summary>
        /// <returns>bool UpdateRequired</returns>
        internal static bool UpdatePending()
        {
            var latestGithubVersion = UpdateManager.GetApplicationVersion();
            string ApplicationVersion = UpdateManager.GetGithubReleaseVersionAsync().Result;

            // Error and abort if we cannot get a github or application version
            if(latestGithubVersion ==  null || ApplicationVersion == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Format("Error checking for updates"));
                Console.ForegroundColor = ConsoleColor.White;

                return false;
            }

            // If the current version and github versions do not match - Flag for updates.
            if(latestGithubVersion != ApplicationVersion)
                return true;

            // No Update required.
            return false;
        }

        static void DoUpdate(string newVersion)
        {
            // download latest release zip
            // unzip to temp directory
            // replace all files in temp directory to install directory
            // die
        }
    }
}