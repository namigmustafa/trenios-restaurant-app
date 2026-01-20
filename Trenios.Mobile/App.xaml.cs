namespace Trenios.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Global exception handlers for debugging
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                System.Diagnostics.Debug.WriteLine($"UNHANDLED: {ex?.GetType().Name}: {ex?.Message}\n{ex?.StackTrace}");
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"UNOBSERVED TASK: {e.Exception?.GetType().Name}: {e.Exception?.Message}\n{e.Exception?.StackTrace}");
                e.SetObserved();
            };
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}