namespace OneDevice.Framework.Libs.Validator;

[Obsolete("String-typed and closed for extension. Parse init strings with OneDevice.Framework.Connection.ConnectionSettings.TryParse, or create the validator directly. Will be removed in 3.0.")]
public static class ValidatorFactory
{
    public static IValidator CreateValidator(string validatorType)
    {
        return validatorType switch
        {
            "Comport" => new ComportValidator(),
            "IPAddress" => new IpAddressValidator(),
            _ => throw new ArgumentException("Invalid validator type")
        };
    }
}