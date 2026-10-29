using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

namespace LanControl.SetupWizard
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string joinedArgs = (args != null && args.Length > 0) ? string.Join(" ", args) : "";

            if (!IsAdministrator())
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = Application.ExecutablePath,
                        Arguments = joinedArgs,
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    Process.Start(psi);
                    return;
                }
                catch
                {
                    // User declined UAC elevation prompt, continue running with standard rights
                }
            }

            InstallEdition initialEdition = InstallEdition.FullSuite;
            if (args != null)
            {
                foreach (string arg in args)
                {
                    string a = (arg ?? "").Trim().ToLowerInvariant();
                    if (a == "agent" || a == "--agent" || a == "-a")
                    {
                        initialEdition = InstallEdition.AgentClient;
                    }
                    else if (a == "server" || a == "--server" || a == "-s")
                    {
                        initialEdition = InstallEdition.ServerMaster;
                    }
                    else if (a == "full" || a == "--full" || a == "both" || a == "--both" || a == "-f")
                    {
                        initialEdition = InstallEdition.FullSuite;
                    }
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupWizardForm(initialEdition));
        }

        private static bool IsAdministrator()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
