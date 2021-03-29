using System;
using System.Threading;
using System.Threading.Tasks;
using Utils.Torch.Patches;

namespace Utils.Torch
{
    public static class GameLoopObserver
    {
        public static Task MoveToGameLoop(CancellationToken canceller = default)
        {
            return MySession_Update.MoveToGameLoop(canceller);
        }
    }
}