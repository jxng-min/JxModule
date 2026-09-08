using UnityEngine;

namespace JxModule.Terminal
{
    public abstract class JxTerminalCommandInstallerBehaviour : MonoBehaviour, IJxTerminalCommandInstaller
    {
        public abstract void Install(JxTerminal terminal);
    }
}
