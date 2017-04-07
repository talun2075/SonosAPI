using System.Windows;

namespace PlaylistWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            foreach (string arg in e.Args)
            {
                if (arg.ToLower() == "autorun")
                {
                    Functions.AutorunArgs = true;

                }
            }
            base.OnStartup(e);
        }

    }
}
