namespace InboxWrap.Module.Errors;

public enum AuthErrorCode
{
    MissingEmailOrPassword,
    EmailInUse,
    UserNotFound,
    InvalidCredentials,
    SaveFailed,
}

public static class AuthErrorCodeExtensions
{
    public static string GetMessage(this AuthErrorCode errorCode) => errorCode switch
    {
        AuthErrorCode.MissingEmailOrPassword => "Email and password are required.",
        AuthErrorCode.EmailInUse => "An account with this email already exists.",
        AuthErrorCode.UserNotFound => "No user was found with that email.",
        AuthErrorCode.InvalidCredentials => "Incorrect email or password.",
        AuthErrorCode.SaveFailed => "Something went wrong while creating your account. Please try again.",
        _ => "An unknown error occurred."
    };
}
