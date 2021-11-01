using System;
using System.IO;

namespace SpeckleConnectionManagerUI.Services
{
    public static class Constants
    {
        public static string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static string AppFolderFullName = Path.Combine(AppDataFolder, "Speckle");
        public static string ServerLocationStore = Path.Combine(AppFolderFullName, "server.txt");
        public static string ChallengeCodeStore = Path.Combine(AppFolderFullName, "challenge.txt");
        public static string DatabasePath = Path.Combine(AppFolderFullName, "Accounts.db");
    }
}