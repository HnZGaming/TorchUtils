using System;
using System.IO;
using System.Resources;
using System.Windows.Controls;
using Torch;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Session;
using Torch.Views;
using Utils.General;

namespace Utils.Torch
{
    internal static class TorchPluginUtils
    {
        public static string MakeFilePath(this TorchPluginBase self, string relativeFilePath)
        {
            return Path.Combine(self.StoragePath, relativeFilePath);
        }

        public static Persistent<T> LoadPersistent<T>(string filePath, T defaultValue) where T : new()
        {
            if (!File.Exists(filePath))
            {
                XmlUtils.SaveOrCreateXmlFile(filePath, defaultValue);
            }

            return Persistent<T>.Load(filePath);
        }

        public static void OnSessionStateChanged(this TorchPluginBase self, TorchSessionState state, Action f)
        {
            var sessionManager = self.Torch.Managers.GetManager<TorchSessionManager>();
            sessionManager.SessionStateChanged += (_, s) =>
            {
                if (s == state)
                {
                    f();
                }
            };
        }

        public static TorchSessionState GetCurrentSessionState(this TorchPluginBase self)
        {
            var sessionManager = self.Torch.Managers.GetManager<TorchSessionManager>();
            return sessionManager.CurrentSession.State;
        }

        public static UserControl GetOrCreateUserControl<T>(this Persistent<T> self, ref UserControl userControl) where T : class, new()
        {
            if (userControl != null) return userControl;

            userControl = new PropertyGrid
            {
                DataContext = self.Data,
            };

            return userControl;
        }
    }
}