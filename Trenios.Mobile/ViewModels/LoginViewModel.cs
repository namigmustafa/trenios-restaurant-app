using System.Windows.Input;
using Trenios.Mobile.Services;

namespace Trenios.Mobile.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly AuthService _authService;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasError;

    public string Username
    {
        get => _username;
        set
        {
            SetProperty(ref _username, value);
            HasError = false;
            ((Command)LoginCommand).ChangeCanExecute();
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            SetProperty(ref _password, value);
            HasError = false;
            ((Command)LoginCommand).ChangeCanExecute();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

    public ICommand LoginCommand { get; }

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
        LoginCommand = new Command(async () => await LoginAsync(), CanLogin);
    }

    private bool CanLogin()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password);
    }

    private async Task LoginAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            HasError = false;

            var (success, errorMessage) = await _authService.LoginAsync(Username, Password);

            if (success)
            {
                // Clear fields
                Password = string.Empty;

                // Navigate based on user role
                var route = _authService.GetPostLoginRoute();
                await Shell.Current.GoToAsync(route);
            }
            else
            {
                ErrorMessage = LocalizationService.Instance.Get("InvalidCredentials");
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Connection error. Please check your network.";
            HasError = true;
            System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
