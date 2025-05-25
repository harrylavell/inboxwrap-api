namespace InboxWrap.Module.Errors;

public enum AccountErrorCode
{
    MissingUserId,
    MissingUserPreferences,
    MissingTimezone,
    MissingDeliveryTimes,
    InvalidTimezone,
    InvalidDeliveryTimes,
    UserNotFound,
    SaveFailed
}

public static class AccountErrorCodeExtensions
{
    public static string GetMessage(this AccountErrorCode errorCode) => errorCode switch
    {
        AccountErrorCode.MissingUserId => "Unable to retrieve your user information.",
        AccountErrorCode.MissingUserPreferences => "Unable to retrieve your requested user preferences.",
        AccountErrorCode.MissingTimezone => "Unable to retrieve your requested timezone.",
        AccountErrorCode.MissingDeliveryTimes => "Unable to retrieve your requested delivery times.",
        AccountErrorCode.InvalidTimezone => "Your request timezone is invalid.",
        AccountErrorCode.InvalidDeliveryTimes => "One or more of your requested delivery times are invalid.",
        AccountErrorCode.UserNotFound => "Associated user not found.",
        AccountErrorCode.SaveFailed => "Something went wrong while updating your preferences. Please try again.",
        _ => "An unknown error occurred."
    };
}
