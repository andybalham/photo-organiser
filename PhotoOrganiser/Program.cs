using PhotoOrganiser.Forms;

namespace PhotoOrganiser;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
