using System.Threading;
using System.Windows;

namespace Utils.General
{
    public static class WindowsUtils
    {
        public static void CopyToClipboard(string text)
        {
            var thread = new Thread(() => Clipboard.SetText(text));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
    }
}