namespace OneDriver.Framework.Libs.Validator;

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